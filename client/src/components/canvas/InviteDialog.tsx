import { useEffect, useState } from 'react'
import { toast } from 'sonner'

import { statusToast } from '@/lib/notify'
import { UserPlus, X } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  extractErrorMessage,
  INVITED_USER_ID_MAX_LENGTH,
  isGuid,
} from '@/api/party'

/*
 * Invite-by-user-ID dialog. Invites are the one place a party is born (it's
 * created on the first invite from inside a canvas), so this is the real entry
 * point. `onInvite` runs createParty-if-needed + inviteUser under the hood.
 */
export default function InviteDialog({
  open,
  onClose,
  onInvite,
}: {
  open: boolean
  onClose: () => void
  onInvite: (userId: string) => Promise<void>
}) {
  const [userId, setUserId] = useState('')
  const [sending, setSending] = useState(false)

  useEffect(() => {
    if (!open) return
    const onKeyDown = (event: KeyboardEvent) => event.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [open, onClose])

  if (!open) return null

  async function handleSend() {
    const id = userId.trim()
    if (!id || sending) return

    // The server rejects anything that isn't a parseable GUID, so don't spend a
    // round trip on a typo (or on a paste the length cap just truncated).
    if (!isGuid(id)) {
      toast.error("That doesn't look like a user ID.")
      return
    }

    setSending(true)
    try {
      await onInvite(id)
      statusToast.success('Invite sent')
      setUserId('')
    } catch (err) {
      toast.error(extractErrorMessage(err))
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-outline/30" onClick={onClose} aria-hidden />

      <div
        role="dialog"
        aria-modal="true"
        aria-label="Invite to party"
        className="relative w-full max-w-md rounded-3xl border-4 border-outline bg-surface p-6 sticker-shadow-lg"
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Close"
          className="absolute top-4 right-4 flex size-8 items-center justify-center rounded-full text-on-background/60 hover:bg-background hover:text-on-background"
        >
          <X className="size-5" aria-hidden />
        </button>

        <h2 className="font-display text-3xl leading-none">Invite to party</h2>
        <p className="mt-2 font-body text-sm text-on-background/70">
          Paste a friend's user ID to pull them into this canvas. Up to 5 artists per party.
        </p>

        <label htmlFor="invite-user-id" className="mt-5 mb-1.5 ml-4 block font-label text-xs font-bold">
          User ID
        </label>
        <div className="flex gap-2">
          <input
            id="invite-user-id"
            value={userId}
            onChange={(event) => setUserId(event.target.value)}
            onKeyDown={(event) => event.key === 'Enter' && handleSend()}
            maxLength={INVITED_USER_ID_MAX_LENGTH}
            placeholder="e.g. 3f9a1c2e-…"
            className="min-w-0 flex-1 rounded-full border-[3px] border-outline bg-background px-4 py-2.5 font-body text-sm outline-none placeholder:text-on-background/35 focus:border-primary"
          />
          <Button size="sm" onClick={handleSend} disabled={sending}>
            <UserPlus />
            {sending ? 'Sending…' : 'Invite'}
          </Button>
        </div>

        <p className="mt-4 font-body text-xs text-on-background/50">
          Invites expire after 5 minutes. Only the party leader can invite.
        </p>
      </div>
    </div>
  )
}
