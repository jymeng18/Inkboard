import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'

import {
  extractErrorMessage,
  getParty,
  getPartyInvites,
  respondToInvite,
  type PendingPartyInvite,
} from '@/api/party'
import { partyInviteToastId } from '@/lib/partyInviteToast'
import { useAuthStore } from '@/stores/authStore'
import { usePartyStore } from '@/stores/partyStore'

export const inviteKeys = {
  all: ['party-invites'] as const,
}

/* Frozen so a default never hands consumers a fresh identity each render. */
const NO_INVITES: readonly PendingPartyInvite[] = Object.freeze([])

/*
 * Pending party invites for the signed-in user.
 *
 * The realtime hub writes new invites straight into this cache (see
 * usePartyHub's ReceiveInvite), so the fetch itself is only backfill: it runs
 * on mount / focus / reconnect to survive refreshes and catch anything that
 * arrived while the socket was down. No polling interval — the push is the live
 * path, this is the durable one. Expiry is left to the caller: the server
 * filters by Pending status, not by time.
 */
export function usePartyInvites() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  const { data, ...rest } = useQuery({
    queryKey: inviteKeys.all,
    queryFn: getPartyInvites,
    enabled: isAuthenticated,
  })

  return { data: data ?? NO_INVITES, ...rest }
}

/*
 * Accept or decline. Mirrors useRespondToFriendRequest: the row leaves the
 * cache up front so it disappears from the inbox (and the toast) instantly,
 * then accepting hydrates the party and jumps into its canvas. Shared by the
 * Inbox tab and the arrival toast so the two behave identically.
 */
export function useRespondToInvite() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: ({
      inviteId,
      accepted,
    }: {
      inviteId: string
      partyId: string
      accepted: boolean
    }) => respondToInvite(inviteId, accepted),

    onMutate: async ({ inviteId }) => {
      // Answered now, wherever from, so clear the arrival toast if it's still up.
      toast.dismiss(partyInviteToastId(inviteId))

      await queryClient.cancelQueries({ queryKey: inviteKeys.all })
      const previous = queryClient.getQueryData<PendingPartyInvite[]>(inviteKeys.all)
      queryClient.setQueryData<PendingPartyInvite[]>(inviteKeys.all, (invites) =>
        invites?.filter((invite) => invite.id !== inviteId),
      )
      return { previous }
    },

    onError: (error, _variables, context) => {
      if (context?.previous) queryClient.setQueryData(inviteKeys.all, context.previous)
      toast.error(extractErrorMessage(error))
    },

    onSuccess: async (_data, { partyId, accepted }) => {
      if (!accepted) {
        toast('Invite declined')
        return
      }
      // respondToInvite already added us; getParty now includes us in members.
      const party = await getParty(partyId)
      const store = usePartyStore.getState()
      store.setParty(party.id, party.leaderId)
      for (const member of party.members) {
        if (member.userId !== party.leaderId) store.addMember(member.userId)
      }
      if (party.canvasId) {
        navigate(`/canvas/${party.canvasId}`)
        toast.success('Invite accepted!')
      } else {
        toast.info('Joined party (no active canvas)')
      }
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: inviteKeys.all })
    },
  })
}
