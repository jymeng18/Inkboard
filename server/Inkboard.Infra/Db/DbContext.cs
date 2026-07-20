using System.Net.NetworkInformation;
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

    // area for db constraints/restrictions, etc..
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PartyMember composite key
        modelBuilder.Entity<PartyMember>().HasKey(pm => new { pm.PartyId, pm.UserId });

        // PartyMember.PartyId refs Party.Id (N to 1)
        modelBuilder
            .Entity<PartyMember>()
            .HasOne(pm => pm.Party)
            .WithMany()
            .HasForeignKey(pm => pm.PartyId);

        // PartyMember.UserId refs User.Id
        modelBuilder
            .Entity<PartyMember>()
            .HasOne(pm => pm.User)
            .WithMany()
            .HasForeignKey(pm => pm.UserId);

        // Party.LeaderId refs User.Id
        modelBuilder
            .Entity<Party>()
            .HasOne(p => p.Leader)
            .WithMany()
            .HasForeignKey(p => p.LeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        // PartyInvite refs Party
        modelBuilder
            .Entity<PartyInvite>()
            .HasOne(pi => pi.Party)
            .WithMany()
            .HasForeignKey(pi => pi.PartyId);

        modelBuilder
            .Entity<PartyInvite>()
            .HasOne(pi => pi.InvitedBy)
            .WithMany()
            .HasForeignKey(pi => pi.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<PartyInvite>()
            .HasOne(pi => pi.InvitedUser)
            .WithMany()
            .HasForeignKey(pi => pi.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BlockList>().HasKey(bl => new { bl.BlockedUserId, bl.UserId });

        modelBuilder
            .Entity<BlockList>()
            .HasOne(bl => bl.User)
            .WithMany()
            .HasForeignKey(bl => bl.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<BlockList>()
            .HasOne(bl => bl.BlockedUser)
            .WithMany()
            .HasForeignKey(bl => bl.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Party.
        modelBuilder
            .Entity<Party>()
            .HasOne(p => p.Canvas)
            .WithMany()
            .HasForeignKey(p => p.CanvasId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder
            .Entity<CanvasOperation>()
            .HasOne(co => co.User)
            .WithMany()
            .HasForeignKey(co => co.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<Canvas>()
            .HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
