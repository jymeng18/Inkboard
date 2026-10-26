import { useEffect, useState } from 'react'
import { toast } from 'sonner'

import { statusToast } from '@/lib/notify'
import { BookUser, ChevronRight, UserPlus, X } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  extractErrorMessage,
  INVITED_USER_EMAIL_MAX_LENGTH,
} from '@/api/party'

/*
 * Invite-by-email dialog. Invites are the one place a party is born (it's
 * created on the first invite from inside a canvas), so this is the real entry
 * point. `onInvite` runs createParty-if-needed + inviteUserByEmail under the hood.
 */
export default function InviteDialog({
  open,
  onClose,
  onInvite,
  onPickFromFriends,
}: {
  open: boolean
  onClose: () => void
  onInvite: (email: string) => Promise<void>
  onPickFromFriends: () => void
}) {
  const [email, setEmail] = useState('')
  const [sending, setSending] = useState(false)

  useEffect(() => {
    if (!open) return
    const onKeyDown = (event: KeyboardEvent) => event.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [open, onClose])

  if (!open) return null

  async function handleSend() {
    const address = email.trim()
    if (!address || sending) return

    setSending(true)
    try {
      await onInvite(address)
      statusToast.success('Invite sent')
      setEmail('')
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
          Enter a friend's Inkboard email to pull them into this canvas. Up to 5 artists per party.
        </p>

        <label htmlFor="invite-email" className="mt-5 mb-1.5 ml-4 block font-label text-xs font-bold">
          Email address
        </label>
        <form
          className="flex gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            void handleSend()
          }}
        >
          <input
            id="invite-email"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            maxLength={INVITED_USER_EMAIL_MAX_LENGTH}
            placeholder="friend@example.com"
            disabled={sending}
            className="min-w-0 flex-1 rounded-full border-[3px] border-outline bg-background px-4 py-2.5 font-body text-sm outline-none placeholder:text-on-background/35 focus:border-primary"
          />
          <Button type="submit" size="sm" disabled={sending || !email.trim()}>
            <UserPlus />
            {sending ? 'Sending…' : 'Invite'}
          </Button>
        </form>

        <p className="mt-4 font-body text-xs text-on-background/50">
          Invites expire after 5 minutes. Only the party leader can invite.
        </p>

        <div className="mt-5 flex items-center gap-3">
          <span className="h-0.5 flex-1 bg-outline/15" aria-hidden />
          <span className="font-label text-xs font-bold text-on-background/40">or</span>
          <span className="h-0.5 flex-1 bg-outline/15" aria-hidden />
        </div>

        {/* The other way in: pulls up the friends list to invite from directly. */}
        <button
          type="button"
          onClick={onPickFromFriends}
          className="mt-4 flex w-full items-center gap-3 rounded-2xl border-[3px] border-outline bg-background px-4 py-3 text-left transition-transform hover:-translate-y-0.5"
        >
          <span className="flex size-9 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-secondary-container">
            <BookUser className="size-4" aria-hidden />
          </span>
          <span className="min-w-0 flex-1">
            <span className="block font-label text-sm font-bold">Invite from friends list</span>
            <span className="block font-body text-xs text-on-background/60">
              Pick from people you've already added.
            </span>
          </span>
          <ChevronRight className="size-5 shrink-0 text-on-background/40" aria-hidden />
        </button>
      </div>
    </div>
  )
}
