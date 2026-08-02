using Inkboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inkboard.Infra.Db;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Party> Parties { get; set; }
    public DbSet<PartyMember> PartyMembers { get; set; }
    public DbSet<PartyInvite> PartyInvites { get; set; }
    public DbSet<BlockList> BlockLists { get; set; }
    public DbSet<Canvas> Canvas { get; set; }
    public DbSet<CanvasOperation> CanvasOperations { get; set; }
    public DbSet<FriendRequest> FriendRequests { get; set; }
    public DbSet<Friendship> Friendships { get; set; }

    // area for db constraints/restrictions, etc..
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Registration only checks this in AuthService, which two concurrent
        // requests can both pass, so the DB has to be the real guard
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder
            .Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Refresh and logout both look a token up by its hash
        modelBuilder.Entity<RefreshToken>().HasIndex(rt => rt.TokenHash).IsUnique();

        // PartyMember composite key
        modelBuilder.Entity<PartyMember>().HasKey(pm => new { pm.PartyId, pm.UserId });

        // PartyMember.PartyId refs Party.Id (N to 1)
        modelBuilder
            .Entity<PartyMember>()
            .HasOne(pm => pm.Party)
            .WithMany()
            .HasForeignKey(pm => pm.PartyId)
            .OnDelete(DeleteBehavior.Cascade);

        // PartyMember.UserId refs User.Id
        modelBuilder
            .Entity<PartyMember>()
            .HasOne(pm => pm.User)
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Party.LeaderId refs User.Id
        modelBuilder
            .Entity<Party>()
            .HasOne(p => p.Leader)
            .WithMany()
            .HasForeignKey(p => p.LeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Party.
        modelBuilder
            .Entity<Party>()
            .HasOne(p => p.Canvas)
            .WithMany()
            .HasForeignKey(p => p.CanvasId)
            .OnDelete(DeleteBehavior.SetNull);

        // PartyInvite refs Party
        modelBuilder
            .Entity<PartyInvite>()
            .HasOne(pi => pi.Party)
            .WithMany()
            .HasForeignKey(pi => pi.PartyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<PartyInvite>()
            .HasOne(pi => pi.InvitedBy)
            .WithMany()
            .HasForeignKey(pi => pi.InvitedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<PartyInvite>()
            .HasOne(pi => pi.InvitedUser)
            .WithMany()
            .HasForeignKey(pi => pi.InvitedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One live invite per user per party. Filtered on Pending so that superseded
        // invites (Expired/Declined/Accepted) never block a re-invite
        modelBuilder
            .Entity<PartyInvite>()
            .HasIndex(pi => new { pi.PartyId, pi.InvitedUserId })
            .IsUnique()
            .HasFilter("\"InviteStatus\" = 0");

        modelBuilder.Entity<BlockList>().HasKey(bl => new { bl.BlockedUserId, bl.UserId });

        modelBuilder
            .Entity<BlockList>()
            .HasOne(bl => bl.User)
            .WithMany()
            .HasForeignKey(bl => bl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<BlockList>()
            .HasOne(bl => bl.BlockedUser)
            .WithMany()
            .HasForeignKey(bl => bl.BlockedUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<CanvasOperation>()
            .Property(co => co.OperationData)
            .HasColumnType("jsonb");

        modelBuilder
            .Entity<CanvasOperation>()
            .HasOne(co => co.Canvas)
            .WithMany()
            .HasForeignKey(co => co.CanvasId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attribution only: a deleted account nulls the stroke's author but never
        // removes the stroke, otherwise replaying the canvas would skip operations
        modelBuilder
            .Entity<CanvasOperation>()
            .HasOne(co => co.User)
            .WithMany()
            .HasForeignKey(co => co.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Replay reads every operation for one canvas in order
        modelBuilder.Entity<CanvasOperation>().HasIndex(co => new { co.CanvasId, co.Timestamp });

        modelBuilder
            .Entity<Canvas>()
            .HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<FriendRequest>()
            .HasOne(fr => fr.Requestee)
            .WithMany()
            .HasForeignKey(fr => fr.RequesteeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<FriendRequest>()
            .HasOne(fr => fr.Requester)
            .WithMany()
            .HasForeignKey(fr => fr.RequesterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<FriendRequest>()
            .HasIndex(fr => new { fr.RequesterId, fr.RequesteeId })
            .IsUnique()
            .HasFilter("\"Status\" = 0");

        // fr.RequestId is indexed, but not RequesteeId
        modelBuilder.Entity<FriendRequest>().HasIndex(fr => fr.RequesteeId);

        modelBuilder.Entity<Friendship>().HasKey(f => new { f.UserId1, f.UserId2 });

        modelBuilder.Entity<Friendship>(entity =>
        {
            // canonical ordering
            entity.ToTable(t =>
                t.HasCheckConstraint("CK_Friendships_UserOrder", "\"UserId1\" < \"UserId2\"")
            );

            /// <summary>
            /// Since the composite key is (f.UserId1, f.UserId2), f.UserId1 is indexed,
            /// but if we query, we need to check f.UserId2 as well, which
            /// is not indexed on default
            /// </summary>
            entity.HasIndex(f => f.UserId2);

            entity
                .HasOne(f => f.User1)
                .WithMany()
                .HasForeignKey(f => f.UserId1)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(f => f.User2)
                .WithMany()
                .HasForeignKey(f => f.UserId2)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
