import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Check, X } from 'lucide-react'

import { extractErrorMessage, getParty, respondToInvite } from '@/api/party'
import { useInviteStore } from '@/stores/inviteStore'
import { usePartyStore } from '@/stores/partyStore'

export interface InviteSummary {
  id: string
  partyId: string
  invitedByUserId: string
  expiresAt: string
}

/*
 * The card body of the party-invite toast: on-brand, with inline Accept /
 * Decline that call respondToInvite and hydrate the party on accept.
 */
export default function PartyInviteToast({
  toastId,
  invite,
}: {
  toastId: string | number
  invite: InviteSummary
}) {
  const [pending, setPending] = useState<'accept' | 'decline' | null>(null)
  const navigate = useNavigate()
  const inviter = `${invite.invitedByUserId.slice(0, 8)}…`

  async function respond(accepted: boolean) {
    setPending(accepted ? 'accept' : 'decline')
    try {
      // Order matters: respond first (adds you to the party), then getParty so
      // the member list already includes you.
      await respondToInvite(invite.id, accepted)
      useInviteStore.getState().removeInvite(invite.id)

      if (accepted) {
        const party = await getParty(invite.partyId)
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
      } else {
        toast('Invite declined')
      }
      toast.dismiss(toastId)
    } catch (err) {
      toast.error(extractErrorMessage(err))
      setPending(null)
    }
  }

  return (
    <div className="flex w-full items-center gap-3 rounded-2xl border-[3px] border-outline bg-surface p-4 font-body text-on-background sticker-shadow">
      <img src="/pen.svg" alt="" className="h-9 -rotate-[30deg]" />

      <div className="min-w-0 flex-1">
        <p className="font-display text-lg leading-none">Party invite</p>
        <p className="truncate font-body text-xs text-on-background/70">from {inviter}</p>
      </div>

      <button
        type="button"
        disabled={pending !== null}
        onClick={() => respond(true)}
        aria-label="Accept invite"
        className="flex size-9 items-center justify-center rounded-full border-[3px] border-outline bg-primary text-white transition-transform hover:-translate-y-0.5 disabled:opacity-50"
      >
        <Check className="size-4" aria-hidden />
      </button>
      <button
        type="button"
        disabled={pending !== null}
        onClick={() => respond(false)}
        aria-label="Decline invite"
        className="flex size-9 items-center justify-center rounded-full border-[3px] border-outline bg-surface transition-transform hover:-translate-y-0.5 disabled:opacity-50"
      >
        <X className="size-4" aria-hidden />
      </button>
    </div>
  )
}
