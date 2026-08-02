import { toast } from 'sonner'
import { Check, X } from 'lucide-react'

import { useRespondToInvite } from '@/hooks/usePartyInvites'

export interface InviteSummary {
  id: string
  partyId: string
  invitedByUserId: string
  expiresAt: string
}

/*
 * The card body of the party-invite toast: on-brand, with inline Accept /
 * Decline. Both answer through the shared useRespondToInvite mutation, so the
 * toast and the Inbox tab stay in lockstep.
 */
export default function PartyInviteToast({
  toastId,
  invite,
}: {
  toastId: string | number
  invite: InviteSummary
}) {
  const respond = useRespondToInvite()
  const inviter = `${invite.invitedByUserId.slice(0, 8)}…`

  function handleRespond(accepted: boolean) {
    respond.mutate(
      { inviteId: invite.id, partyId: invite.partyId, accepted },
      // The mutation reports the outcome; this card just gets out of the way.
      { onSuccess: () => toast.dismiss(toastId) },
    )
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
        disabled={respond.isPending}
        onClick={() => handleRespond(true)}
        aria-label="Accept invite"
        className="flex size-9 items-center justify-center rounded-full border-[3px] border-outline bg-primary text-white transition-transform hover:-translate-y-0.5 disabled:opacity-50"
      >
        <Check className="size-4" aria-hidden />
      </button>
      <button
        type="button"
        disabled={respond.isPending}
        onClick={() => handleRespond(false)}
        aria-label="Decline invite"
        className="flex size-9 items-center justify-center rounded-full border-[3px] border-outline bg-surface transition-transform hover:-translate-y-0.5 disabled:opacity-50"
      >
        <X className="size-4" aria-hidden />
      </button>
    </div>
  )
}
