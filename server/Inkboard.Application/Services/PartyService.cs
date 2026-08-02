using Inkboard.Application.Common;
using Inkboard.Application.Interfaces;
using Inkboard.Application.Parties.DTO;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services
{
    public class PartyService : IPartyService
    {
        private readonly IPartyRepository _partyRepository;
        private readonly IPartyInviteRepository _partyInviteRepository;
        private readonly IBlockListRepository _blockListRepository;
        private readonly IPartyNotifier _partyNotifier;
        private readonly ICanvasService _canvasService;
        private readonly ICanvasRepository _canvasRepository;

        public PartyService(
            IPartyRepository partyRepository,
            IPartyInviteRepository partyInviteRepository,
            IBlockListRepository blockListRepository,
            IPartyNotifier partyNotifier,
            ICanvasService canvasService,
            ICanvasRepository canvasRepository
        )
        {
            _partyRepository = partyRepository;
            _partyInviteRepository = partyInviteRepository;
            _blockListRepository = blockListRepository;
            _partyNotifier = partyNotifier;
            _canvasService = canvasService;
            _canvasRepository = canvasRepository;
        }

        public async Task<Result> BlockUserAsync(Guid leaderId, Guid targetUserId)
        {
            if (leaderId == targetUserId)
                return Result.Fail(ErrorType.Validation, "You cannot block yourself.");

            var alreadyBlocked = await _blockListRepository.IsBlockedAsync(leaderId, targetUserId);
            if (alreadyBlocked)
                return Result.Fail(ErrorType.Conflict, "This user is already blocked.");

            var block = new BlockList { UserId = leaderId, BlockedUserId = targetUserId };
            await _blockListRepository.BlockUserAsync(block);

            return Result.Ok();
        }

        public async Task<Result<Party>> CreatePartyAsync(Guid leaderId, Guid canvasId)
        {
            var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
            if (canvas is null)
            {
                return Result<Party>.Fail(ErrorType.NotFound, "Canvas does not exist.");
            }

            Party newParty = new()
            {
                LeaderId = leaderId,
                CanvasId = canvasId,
                CreatedAt = DateTime.UtcNow,
            };

            var existingParty = await _partyRepository.GetActivePartyForUserAsync(leaderId);
            if (existingParty is not null)
                return Result<Party>.Fail(ErrorType.Conflict, "An active party already exists.");

            await _partyRepository.CreatePartyAsync(newParty);

            PartyMember partyMember = new()
            {
                PartyId = newParty.Id,
                UserId = leaderId,
                Role = UserRole.Leader,
            };
            await _partyRepository.AddMemberAsync(partyMember);
            await _partyNotifier.NotifyMemberJoined(newParty.Id, partyMember);

            return Result<Party>.Ok(newParty);
        }

        public async Task<Result<PartyDetailDto>> GetPartyByIdAsync(Guid partyId)
        {
            var party = await _partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result<PartyDetailDto>.Fail(ErrorType.NotFound, "Party not found.");

            var members = await _partyRepository.GetMembersAsync(partyId);

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
            var party = await _partyRepository.GetByIdAsync(partyId);
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

            var alreadyMember = await _partyRepository.IsUserInPartyAsync(partyId, invitedUserId);
            if (alreadyMember)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Conflict,
                    "User is already in the party."
                );
            }

            var isBlocked = await _blockListRepository.IsBlockedAsync(
                party.LeaderId,
                invitedUserId
            );
            if (isBlocked)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Validation,
                    "You have blocked this user."
                );
            }

            var memberCount = await _partyRepository.GetMemberCountAsync(partyId);
            if (memberCount >= 5)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Validation,
                    "Party is full. (Max 5 members)"
                );
            }

            var existingInvite = await _partyInviteRepository.GetPendingInviteAsync(
                partyId,
                invitedUserId
            );
            if (existingInvite is not null)
            {
                // If inv not expired, then its an active invite, reject ops
                if (existingInvite.ExpiresAt > DateTime.UtcNow)
                {
                    return Result<PartyInvite>.Fail(
                        ErrorType.Conflict,
                        "An invite is already pending for this user."
                    );
                }
                existingInvite.InviteStatus = InviteStatus.Expired;
                await _partyInviteRepository.UpdateInviteAsync(existingInvite);
            }

            PartyInvite partyInvite = new()
            {
                PartyId = partyId,
                InvitedByUserId = leaderId,
                InvitedUserId = invitedUserId,
                InviteStatus = InviteStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            };

            await _partyInviteRepository.CreateInviteAsync(partyInvite);
            await _partyNotifier.NotifyInvite(invitedUserId, partyInvite);

            return Result<PartyInvite>.Ok(partyInvite);
        }

        public async Task<Result> LeavePartyAsync(Guid partyId, Guid userId)
        {
            var party = await _partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result.Fail(ErrorType.NotFound, "Party not found.");

            var member = await _partyRepository.GetMemberAsync(partyId, userId);
            if (member is null)
                return Result.Fail(ErrorType.Validation, "You are not in this party.");

            var isLeader = party.LeaderId == userId;
            var memberCount = await _partyRepository.GetMemberCountAsync(partyId);

            // * We check cnt <= 2, and not cnt == 1, because this is checking before party dissolves
            if (memberCount <= 2)
            {
                // If its not the leader who is initating the call, let member leave, but dont kick out leader
                if (!isLeader)
                {
                    await _partyRepository.RemoveMemberAsync(member);
                    await _partyNotifier.NotifyMemberLeft(partyId, member.UserId);
                    return Result.Ok();
                }

                await DissolvePartyAsync(party);
                return Result.Ok();
            }

            if (!isLeader)
            {
                await _partyRepository.RemoveMemberAsync(member);
                await _partyNotifier.NotifyMemberLeft(partyId, member.UserId);
                return Result.Ok();
            }

            // Leader is leaving a party of 3+. Hand leadership to the
            // longest-standing member and break the canvas link, keeping the
            // party alive for everyone else.
            var newLeader = await _partyRepository.GetOldestMemberAsync(partyId);

            var canvas = await _canvasRepository.GetCanvasByPartyIdAsync(partyId);
            if (canvas is not null)
                await _canvasService.ForceEndSessionAsync(canvas.Id, userId);

            party.LeaderId = newLeader.UserId;
            newLeader.Role = UserRole.Leader;

            await _partyRepository.UpdatePartyAsync(party);
            await _partyRepository.RemoveMemberAsync(member);

            await _partyNotifier.NotifyMemberLeft(partyId, member.UserId);
            await _partyNotifier.NotifyLeadershipTransferred(newLeader.UserId, partyId);

            return Result.Ok();
        }


        private async Task DissolvePartyAsync(Party party)
        {
            var members = await _partyRepository.GetMembersAsync(party.Id);
            var memberIds = members.ConvertAll(m => m.UserId);

            await _partyNotifier.NotifyPartyEnded(party.Id, memberIds);

            foreach (var member in members)
                await _partyRepository.RemoveMemberAsync(member);

            await _partyRepository.DeletePartyAsync(party);
        }

        /*
         * The deliberate end of a session. LeavePartyAsync exists for one person
         * stepping out, which keeps the party alive under a new leader; this is
         * the leader closing the whole thing down, so every member is notified,
         * every membership row goes, and the party itself is deleted.
         */
        public async Task<Result> EndSessionAsync(Guid partyId, Guid leaderId)
        {
            var party = await _partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result.Fail(ErrorType.NotFound, "Party not found.");

            if (party.LeaderId != leaderId)
                return Result.Fail(ErrorType.Forbidden, "Only the leader can end the session.");

            await DissolvePartyAsync(party);

            return Result.Ok();
        }

        /*
         * Re-links a party that already exists to a different canvas. 
         */
        public async Task<Result> SetPartyCanvasAsync(Guid partyId, Guid leaderId, Guid canvasId)
        {
            var party = await _partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result.Fail(ErrorType.NotFound, "Party not found.");

            if (party.LeaderId != leaderId)
                return Result.Fail(ErrorType.Forbidden, "Only the leader can open a canvas for the party.");

            var canvas = await _canvasRepository.GetCanvasByIdAsync(canvasId);
            if (canvas is null)
                return Result.Fail(ErrorType.NotFound, "Canvas does not exist.");

            party.CanvasId = canvasId;
            await _partyRepository.UpdatePartyAsync(party);

            // Everyone but the leader, who is already on their way there.
            var members = await _partyRepository.GetMembersAsync(partyId);
            var memberIds = members.ConvertAll(m => m.UserId).FindAll(id => id != leaderId);

            if (memberIds.Count > 0)
                await _partyNotifier.NotifyPartyCanvasOpened(partyId, canvasId, memberIds);

            return Result.Ok();
        }

        public async Task<Result> RemoveMemberAsync(Guid partyId, Guid leaderId, Guid targetUserId)
        {
            var party = await _partyRepository.GetByIdAsync(partyId);
            if (party is null)
                return Result.Fail(ErrorType.NotFound, "Party not found.");

            if (party.LeaderId != leaderId)
                return Result.Fail(ErrorType.Forbidden, "Only the leader can kick members.");

            if (party.LeaderId == targetUserId)
                return Result.Fail(ErrorType.Validation, "You cannot kick yourself.");

            var memberToBeRemoved = await _partyRepository.GetMemberAsync(partyId, targetUserId);
            if (memberToBeRemoved is null)
                return Result.Fail(ErrorType.NotFound, "Member not found in party.");

            await _partyRepository.RemoveMemberAsync(memberToBeRemoved);
            await _partyNotifier.NotifyKick(memberToBeRemoved.UserId, partyId);

            return Result.Ok();
        }

        public async Task<Result<PartyInvite>> RespondToUserInviteAsync(
            Guid inviteId,
            Guid userId,
            bool accepted
        )
        {
            var invite = await _partyInviteRepository.GetByIdAsync(inviteId);
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
            var activeParty = await _partyRepository.GetActivePartyForUserAsync(userId);
            if (activeParty is not null)
            {
                return Result<PartyInvite>.Fail(
                    ErrorType.Validation,
                    "You are already in an active Party."
                );
            }

            if (!accepted)
            {
                invite.InviteStatus = InviteStatus.Declined;
                await _partyInviteRepository.UpdateInviteAsync(invite);
                return Result<PartyInvite>.Ok(invite);
            }

            // Re-check block list — leader may have blocked user since the invite was sent
            var isBlocked = await _blockListRepository.IsBlockedAsync(
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
            var memberCount = await _partyRepository.GetMemberCountAsync(invite.PartyId);
            if (memberCount >= 5)
                return Result<PartyInvite>.Fail(ErrorType.Validation, "Party is full.");

            var newMember = new PartyMember
            {
                PartyId = invite.PartyId,
                UserId = userId,
                Role = UserRole.Member,
            };

            await _partyRepository.AddMemberAsync(newMember);

            invite.InviteStatus = InviteStatus.Accepted;
            await _partyInviteRepository.UpdateInviteAsync(invite);

            await _partyNotifier.NotifyMemberJoined(invite.PartyId, newMember);

            return Result<PartyInvite>.Ok(invite);
        }

        public async Task<Result<List<PartyInvite>>> GetPartyInvitesByUserIdAsync(Guid userId)
        {
            List<PartyInvite> partyInvites = await _partyInviteRepository.GetAllPendingByUserIdAsync(userId);
            return Result<List<PartyInvite>>.Ok(data: partyInvites);
        }

    }
}
