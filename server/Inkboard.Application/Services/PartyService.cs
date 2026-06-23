using Inkboard.Application.Interfaces;
using Inkboard.Domain.Models;
using Inkboard.Domain.Repositories;

namespace Inkboard.Application.Services
{
    public class PartyService : IPartyService
    {
        private readonly IPartyRepository partyRepository;
        private readonly IPartyInviteRepository partyInviteRepository;
        private readonly IBlockListRepository blockListRepository;

        public PartyService(
            IPartyRepository partyRepository,
            IPartyInviteRepository partyInviteRepository,
            IBlockListRepository blockListRepository
        )
        {
            this.partyRepository = partyRepository;
            this.partyInviteRepository = partyInviteRepository;
            this.blockListRepository = blockListRepository;
        }

        public async Task BlockUserAsync(Guid leaderId, Guid targetUserId)
        {
            if (leaderId == targetUserId)
                throw new PartyValidationException("You cannot block yourself.");

            var alreadyBlocked = await blockListRepository.IsBlockedAsync(leaderId, targetUserId);
            if (alreadyBlocked)
                throw new PartyValidationException("This user is already blocked.");

            var block = new BlockList { UserId = leaderId, BlockedUserId = targetUserId };

            await blockListRepository.BlockUserAsync(block);
        }

        public async Task<Party> CreatePartyAsync(Guid leaderId)
        {
            Party newParty = new()
            {
                LeaderId = leaderId,
                CanvasId = null,
                CreatedAt = DateTime.UtcNow,
            };
            await partyRepository.CreatePartyAsync(newParty);

            // add leader as a member
            PartyMember partyMember = new()
            {
                PartyId = newParty.Id,
                UserId = leaderId,
                Role = UserRole.Leader,
            };
            await partyRepository.AddMemberAsync(partyMember);

            return newParty;
        }

        public async Task<PartyInvite> InviteUserAsync(
            Guid partyId,
            Guid leaderId,
            Guid invitedUserId
        )
        {
            var party = await partyRepository.GetByIdAsync(partyId) ?? throw new PartyNotFoundException("Party not found.");
            if (party.LeaderId != invitedUserId)
                throw new PartyForbiddenException("Only leader can invite people.");

            if (party.LeaderId == invitedUserId)
                throw new PartyValidationException("You cannot invite yourself.");

            var alreadyMember = await partyRepository.IsUserInPartyAsync(partyId, invitedUserId);
            if (alreadyMember)
                throw new PartyValidationException("User is already in party.");

            var isBlocked = await blockListRepository.IsBlockedAsync(party.LeaderId, invitedUserId);
            if (isBlocked)
                throw new PartyValidationException("You have blocked this user.");

            var memberCount = await partyRepository.GetMemberCountAsync(partyId);
            if (memberCount >= 5)
                throw new PartyValidationException("Party is full. (Max 5 Members)");

            var existingInvite = await partyInviteRepository.GetPendingInviteAsync(
                partyId,
                invitedUserId
            );
            if (existingInvite is not null)
            {
                throw new PartyValidationException("An invite is already pending for this user.");
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
            return partyInvite;
        }

        public async Task LeavePartyAsync(Guid partyId, Guid userId)
        {
            var party = await partyRepository.GetByIdAsync(partyId) ?? throw new PartyNotFoundException("Party not found.");
            var member = await partyRepository.GetMemberAsync(partyId, userId) ?? throw new PartyValidationException("You are not in a party.");
            var isLeader = party.LeaderId == userId;

            if (!isLeader)
            {
                await partyRepository.RemoveMemberAsync(member);
                return;
            }

            var memberCount = await partyRepository.GetMemberCountAsync(partyId);
            if (memberCount == 1)
            {
                // leader last person, dissolve party
                await partyRepository.RemoveMemberAsync(member);
                await partyRepository.DeletePartyAsync(party);
                return;
            }

            // transfer leadership
            var newLeader = await partyRepository.GetOldestMemberAsync(partyId);
            party.LeaderId = newLeader.UserId;
            newLeader.Role = UserRole.Leader;

            await partyRepository.UpdatePartyAsync(party);
            await partyRepository.RemoveMemberAsync(member);
        }

        public async Task RemoveMemberAsync(Guid partyId, Guid leaderId, Guid targetUserId)
        {
            var party = await partyRepository.GetByIdAsync(partyId) ?? throw new PartyNotFoundException("Party not found.");
            if (party.LeaderId != leaderId)
                throw new PartyForbiddenException("Only leader can kick members.");

            if (party.LeaderId == targetUserId)
                throw new PartyValidationException("You cannot kick yourself.");

            var isMember = await partyRepository.IsUserInPartyAsync(partyId, targetUserId);
            if (!isMember)
                throw new PartyValidationException("Member not in party.");

            var memberToBeRemoved = await partyRepository.GetMemberAsync(partyId, targetUserId) ?? throw new PartyValidationException("Member not found.");
            await partyRepository.RemoveMemberAsync(memberToBeRemoved);
            return;
        }

        public async Task<PartyInvite> RespondToUserInviteAsync(
            Guid inviteId,
            Guid userId,
            bool accepted
        )
        {
            var invite = await partyInviteRepository.GetByIdAsync(inviteId) ?? throw new PartyNotFoundException("Invite not found.");
            if (invite.InvitedUserId != userId)
                throw new PartyForbiddenException("This invite does not belong to you.");

            if (invite.InviteStatus != InviteStatus.Pending)
                throw new PartyValidationException("This invite has already been responded to.");

            if (invite.ExpiresAt < DateTime.UtcNow)
                throw new PartyValidationException("This invite has expired.");

            if (!accepted)
            {
                invite.InviteStatus = InviteStatus.Declined;
                await partyInviteRepository.UpdateInviteAsync(invite);
                return invite;
            }

            // re-check block list — leader may have blocked this user since the invite was sent
            var isBlocked = await blockListRepository.IsBlockedAsync(
                invite.InvitedByUserId,
                userId
            );
            if (isBlocked)
                throw new PartyValidationException("You are no longer able to join this party.");

            // re-check membership cap — party may have filled up since the invite was sent
            var memberCount = await partyRepository.GetMemberCountAsync(invite.PartyId);
            if (memberCount >= 5)
                throw new PartyValidationException("Party is full.");

            var newMember = new PartyMember
            {
                PartyId = invite.PartyId,
                UserId = userId,
                Role = UserRole.Member,
            };

            await partyRepository.AddMemberAsync(newMember);

            invite.InviteStatus = InviteStatus.Accepted;
            await partyInviteRepository.UpdateInviteAsync(invite);

            return invite;
        }
    }
}
