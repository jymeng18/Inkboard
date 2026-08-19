import { toast } from 'sonner'

import PartyInviteToast, { type InviteSummary } from '@/components/dashboard/social/PartyInviteToast'

/*
 * A stable, per-invite toast id. Answering the invite anywhere — the Inbox tab
 * or the toast itself — can then dismiss this exact toast so an already-answered
 * invite never lingers on screen.
 */
export const partyInviteToastId = (inviteId: string) => `party-invite:${inviteId}`

/*
 * Shows the party invite as a custom, on-brand sonner toast with inline
 * Accept / Decline. It's just a 15s heads-up now; the invite also lives in the
 * Inbox for its full 5-minute window, so this doesn't need to linger.
 */
export function showPartyInviteToast(invite: InviteSummary) {
  if (new Date(invite.expiresAt).getTime() <= Date.now()) return

  toast.custom((id) => <PartyInviteToast toastId={id} invite={invite} />, {
    id: partyInviteToastId(invite.id),
    duration: 15_000,
  })
}
