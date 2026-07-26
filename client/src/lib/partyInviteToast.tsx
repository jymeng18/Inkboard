import { toast } from 'sonner'

import PartyInviteToast, { type InviteSummary } from '@/components/dashboard/PartyInviteToast'

/**
 * Shows the party invite as a custom, on-brand sonner toast with inline
 * Accept / Decline actions. Stays up until the invite expires or is answered.
 */
export function showPartyInviteToast(invite: InviteSummary) {
  const msLeft = new Date(invite.expiresAt).getTime() - Date.now()
  if (msLeft <= 0) return

  toast.custom((id) => <PartyInviteToast toastId={id} invite={invite} />, {
    duration: msLeft,
  })
}
