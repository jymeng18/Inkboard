using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Parties.DTO;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services
{
    public class PartyService : IPartyService
    {
        private readonly IPartyRepository partyRepository;
        private readonly IPartyInviteRepository partyInviteRepository;
        private readonly IBlockListRepository blockListRepository;
        private readonly IPartyNotifier partyNotifier;
        private readonly ICanvasService canvasService;
        private readonly ICanvasRepository canvasRepository;

        public PartyService(
            IPartyRepository partyRepository,
            IPartyInviteRepository partyInviteRepository,
            IBlockListRepository blockListRepository,
            IPartyNotifier partyNotifier,
            ICanvasService canvasService,
            ICanvasRepository canvasRepository
        )
        {
            this.partyRepository = partyRepository;
            this.partyInviteRepository = partyInviteRepository;
            this.blockListRepository = blockListRepository;
            this.partyNotifier = partyNotifier;
            this.canvasService = canvasService;
            this.canvasRepository = canvasRepository;
        }

        public async Task<Result> BlockUserAsync(Guid leaderId, Guid targetUserId)
        {
            if (leaderId == targetUserId)
                return Result.Fail(ErrorType.Validation, "You cannot block yourself.");

            var alreadyBlocked = await blockListRepository.IsBlockedAsync(leaderId, targetUserId);
            if (alreadyBlocked)
                return Result.Fail(ErrorType.Conflict, "This user is already blocked.");

            var block = new BlockList { UserId = leaderId, BlockedUserId = targetUserId };
            await blockListRepository.BlockUserAsync(block);

            return Result.Ok();
        }

        public async Task<Result<Party>> CreatePartyAsync(Guid leaderId, Guid canvasId)
        {
            Party newParty = new()
            {
                LeaderId = leaderId,
                CanvasId = canvasId,
                CreatedAt = DateTime.UtcNow,
            };

            var existingParty = await partyRepository.GetActivePartyForUserAsync(leaderId);
            if (existingParty is not null)
                return Result<Party>.Fail(ErrorType.Conflict, "An active party already exists.");

            await partyRepository.CreatePartyAsync(newParty);

            PartyMember partyMember = new()
            {
                PartyId = newParty.Id,
                UserId = leaderId,
                Role = UserRole.Leader,
            };
            await partyRepository.AddMemberAsync(partyMember);
            await partyNotifier.NotifyMemberJoined(newParty.Id, partyMember);

            return Result<Party>.Ok(newParty);
        }

        public async Task<Result<PartyDetailDto>> GetPartyByIdAsync(Guid partyId)
        {
            var party = await partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result<PartyDetailDto>.Fail(ErrorType.NotFound, "Party not found.");

            var members = await partyRepository.GetMembersAsync(partyId);

            var dto = new PartyDetailDto(
                party.Id,
                party.LeaderId,
                party.CanvasId,
                members.ConvertAll(m => new PartyMemberDto(m.UserId, m.Role.ToString(), m.JoinedAt))
            );

            return Result<PartyDetailDto>.Ok(dto);
        }

        public async Task<Result<PartyInvite>> InviteUserAsync(
            Guid partyId,
            Guid leaderId,
            Guid invitedUserId
        )
        {
            var party = await partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result<PartyInvite>.Fail(ErrorType.NotFound, "Party not found.");

            if (party.LeaderId != leaderId)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Forbidden,
                    "Only the leader can invite people."
                );
            }

            if (party.LeaderId == invitedUserId)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Validation,
                    "You cannot invite yourself."
                );
            }

            var alreadyMember = await partyRepository.IsUserInPartyAsync(partyId, invitedUserId);
            if (alreadyMember)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Conflict,
                    "User is already in the party."
                );
            }

            var isBlocked = await blockListRepository.IsBlockedAsync(party.LeaderId, invitedUserId);
            if (isBlocked)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Validation,
                    "You have blocked this user."
                );
            }

            var memberCount = await partyRepository.GetMemberCountAsync(partyId);
            if (memberCount >= 5)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Validation,
                    "Party is full. (Max 5 members)"
                );
            }

            var existingInvite = await partyInviteRepository.GetPendingInviteAsync(
                partyId,
                invitedUserId
            );
            if (existingInvite is not null)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Conflict,
                    "An invite is already pending for this user."
                );
            }

            PartyInvite partyInvite = new()
            {
                PartyId = partyId,
                InvitedByUserId = leaderId,
                InvitedUserId = invitedUserId,
                InviteStatus = InviteStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            };

            await partyInviteRepository.CreateInviteAsync(partyInvite);
            await partyNotifier.NotifyInvite(invitedUserId, partyInvite);

            return Result<PartyInvite>.Ok(partyInvite);
        }

        public async Task<Result> LeavePartyAsync(Guid partyId, Guid userId)
        {
            var party = await partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result.Fail(ErrorType.NotFound, "Party not found.");

            var member = await partyRepository.GetMemberAsync(partyId, userId);
            if (member is null)
                return Result.Fail(ErrorType.Validation, "You are not in this party.");

            var isLeader = party.LeaderId == userId;

            if (!isLeader)
            {
                await partyRepository.RemoveMemberAsync(member);
                await partyNotifier.NotifyMemberLeft(partyId, member.UserId);
                return Result.Ok();
            }

            var memberCount = await partyRepository.GetMemberCountAsync(partyId);
            if (memberCount == 1)
            {
                await partyRepository.RemoveMemberAsync(member);
                await partyNotifier.NotifyMemberLeft(partyId, member.UserId);
                await partyRepository.DeletePartyAsync(party);
                return Result.Ok();
            }

            // Transfer leadership to longest-standing member
            var newLeader = await partyRepository.GetOldestMemberAsync(partyId);

            // Completely shut Canvas and its users out, keep party alive
            var canvas = await canvasRepository.GetCanvasByPartyIdAsync(partyId);
            if (canvas is not null)
                await canvasService.ForceEndSessionAsync(canvas.Id, userId);

            // Shift party roles after force end
            party.LeaderId = newLeader.UserId;
            newLeader.Role = UserRole.Leader;

            await partyRepository.UpdatePartyAsync(party);
            await partyRepository.RemoveMemberAsync(member);

            await partyNotifier.NotifyMemberLeft(partyId, member.UserId);
            await partyNotifier.NotifyLeadershipTransferred(newLeader.UserId, partyId);

            return Result.Ok();
        }

        public async Task<Result> RemoveMemberAsync(Guid partyId, Guid leaderId, Guid targetUserId)
        {
            var party = await partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result.Fail(ErrorType.NotFound, "Party not found.");

            if (party.LeaderId != leaderId)
                return Result.Fail(ErrorType.Forbidden, "Only the leader can kick members.");

            if (party.LeaderId == targetUserId)
                return Result.Fail(ErrorType.Validation, "You cannot kick yourself.");

            var memberToBeRemoved = await partyRepository.GetMemberAsync(partyId, targetUserId);
            if (memberToBeRemoved is null)
                return Result.Fail(ErrorType.NotFound, "Member not found in party.");

            await partyRepository.RemoveMemberAsync(memberToBeRemoved);
            await partyNotifier.NotifyKick(memberToBeRemoved.UserId, partyId);

            return Result.Ok();
        }

        public async Task<Result<PartyInvite>> RespondToUserInviteAsync(
            Guid inviteId,
            Guid userId,
            bool accepted
        )
        {
            var invite = await partyInviteRepository.GetByIdAsync(inviteId);
            if (invite is null)
                return Result<PartyInvite>.Fail(ErrorType.NotFound, "Invite not found.");

            if (invite.InvitedUserId != userId)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Forbidden,
                    "This invite does not belong to you."
                );
            }

            if (invite.InviteStatus != InviteStatus.Pending)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Conflict,
                    "This invite has already been responded to."
                );
            }

            if (invite.ExpiresAt < DateTime.UtcNow)
                return Result<PartyInvite>.Fail(ErrorType.Validation, "This invite has expired.");

            // Check if user alr in a Party
            var activeParty = await partyRepository.GetActivePartyForUserAsync(userId);
            if (activeParty is not null)
            {
                return Result<PartyInvite>.Fail(ErrorType.Validation, "You are already in an active Party.");
            }


            if (!accepted)
            {
                invite.InviteStatus = InviteStatus.Declined;
                await partyInviteRepository.UpdateInviteAsync(invite);
                return Result<PartyInvite>.Ok(invite);
            }

            // Re-check block list — leader may have blocked this user since the invite was sent
            var isBlocked = await blockListRepository.IsBlockedAsync(
                invite.InvitedByUserId,
                userId
            );
            if (isBlocked)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Forbidden,
                    "You are no longer able to join this party."
                );
            }

            // Re-check membership cap — party may have filled up since the invite was sent
            var memberCount = await partyRepository.GetMemberCountAsync(invite.PartyId);
            if (memberCount >= 5)
                return Result<PartyInvite>.Fail(ErrorType.Validation, "Party is full.");

            var newMember = new PartyMember
            {
                PartyId = invite.PartyId,
                UserId = userId,
                Role = UserRole.Member,
            };

            await partyRepository.AddMemberAsync(newMember);

            invite.InviteStatus = InviteStatus.Accepted;
            await partyInviteRepository.UpdateInviteAsync(invite);

            await partyNotifier.NotifyMemberJoined(invite.PartyId, newMember);

            return Result<PartyInvite>.Ok(invite);
        }
    }
}
