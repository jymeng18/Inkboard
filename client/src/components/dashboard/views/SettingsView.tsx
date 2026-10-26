import { useState } from 'react'
import { toast } from 'sonner'
import { Bell, Check, Copy, LoaderCircle, LockKeyhole, LogOut, Save, SlidersHorizontal, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import UserAvatar from '@/components/ui/UserAvatar'
import DeleteAccountDialog from '@/components/dashboard/views/DeleteAccountDialog'
import { useAccount } from '@/hooks/useAccount'
import { usePreferencesStore } from '@/stores/preferencesStore'

interface SettingsViewProps {
  userId: string
  userName: string
  onLogout: () => void
}

export default function SettingsView({ userId, userName, onLogout }: SettingsViewProps) {
  const { data: account, isPending, isError, refetch } = useAccount()
  const [deleteOpen, setDeleteOpen] = useState(false)

  return (
    <section className="max-w-3xl space-y-8 pb-4">
      <header>
        <p className="mb-2 font-label text-xs font-bold tracking-widest text-on-background/50 uppercase">Your corner of Inkboard</p>
        <h1 className="font-display text-4xl leading-none sm:text-5xl">Settings</h1>
        <p className="mt-3 font-body text-sm text-on-background/70">Your account, and the little things that make drawing feel like you.</p>
      </header>

      <section aria-labelledby="account-heading" className="overflow-hidden rounded-3xl border-[3px] border-outline bg-surface sticker-shadow">
        <div className="flex items-center gap-4 border-b-[3px] border-outline bg-primary-container px-5 py-5 sm:px-6">
          <UserAvatar name={account?.userName || userName || '?'} userId={userId} size="size-14" />
          <div className="min-w-0 flex-1">
            <h2 id="account-heading" className="font-display text-3xl leading-none">Your account</h2>
            <p className="mt-1 truncate font-label text-sm font-bold">{account?.userName || userName}</p>
          </div>
          <span className="hidden rotate-3 rounded-full border-2 border-outline bg-surface px-3 py-1 font-handwriting text-lg sm:block">Made for creating</span>
        </div>
        <div className="p-5 sm:p-6">
          {isPending && <p role="status" className="flex items-center gap-2 py-4 font-body text-sm text-on-background/60"><LoaderCircle className="size-4 animate-spin" aria-hidden />Loading your account…</p>}
          {isError && (
            <div role="alert" className="mb-4 rounded-2xl border-2 border-outline bg-background p-4">
              <p className="font-body text-sm">We couldn&apos;t load your account details.</p>
              <Button variant="surface" size="sm" className="mt-3" onClick={() => void refetch()}>Try again</Button>
            </div>
          )}
          {account && (
            <>
              <dl className="mb-5 grid gap-5 sm:grid-cols-2">
                <div>
                  <dt className="font-label text-xs font-bold text-on-background/50">Display name</dt>
                  <dd className="mt-1 break-words font-body text-sm font-bold">{account.userName}</dd>
                </div>
                <div>
                  <dt className="font-label text-xs font-bold text-on-background/50">Email address</dt>
                  <dd className="mt-1 break-all font-body text-sm font-bold">{account.email}</dd>
                </div>
                <div>
                  <dt className="font-label text-xs font-bold text-on-background/50">Member since</dt>
                  <dd className="mt-1 font-body text-sm font-bold"><time dateTime={account.createdAt}>{new Date(account.createdAt).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })}</time></dd>
                </div>
                <div>
                  <dt className="font-label text-xs font-bold text-on-background/50">Sign-in method</dt>
                  <dd className="mt-1 flex items-center gap-1.5 font-body text-sm font-bold"><LockKeyhole className="size-3.5" aria-hidden />Email &amp; password</dd>
                </div>
              </dl>
              <UserIdCard userId={account.id} />
            </>
          )}
          <Button variant="surface" size="sm" className="mt-3" onClick={onLogout}><LogOut />Log out</Button>
        </div>
      </section>

      <section aria-labelledby="preferences-heading">
        <div className="mb-4 flex items-start gap-3">
          <SlidersHorizontal className="mt-1 size-5" aria-hidden />
          <div>
            <h2 id="preferences-heading" className="font-display text-3xl leading-none">Make it yours</h2>
            <p className="mt-1 font-body text-xs text-on-background/60">These preferences are saved on this browser.</p>
          </div>
        </div>
        <NotificationsCard />
        <AutosaveCard />
      </section>

      <section aria-labelledby="delete-heading" className="rounded-3xl border-[3px] border-outline bg-primary/10 p-5 sticker-shadow-sm sm:p-6">
        <div className="flex items-center gap-3">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-2xl border-[3px] border-outline bg-primary"><Trash2 className="size-4" aria-hidden /></span>
          <h2 id="delete-heading" className="font-display text-3xl leading-none">A permanent goodbye</h2>
        </div>
        <p className="mt-3 max-w-lg font-body text-sm leading-relaxed text-on-background/75">
          Deleting your account removes your profile and every canvas you own. You&apos;ll confirm with your password before anything is deleted.
        </p>
        <Button size="sm" className="mt-5" onClick={() => setDeleteOpen(true)} disabled={!account}><Trash2 />Delete account</Button>
      </section>

      {deleteOpen && account && <DeleteAccountDialog email={account.email} onClose={() => setDeleteOpen(false)} />}
    </section>
  )
}

/*
 * Mute the ambient party and social activity
 * toasts. Invites and friend requests sent to the user are deliberately out of
 * scope here and keep coming through.
 */
function NotificationsCard() {
  const enabled = usePreferencesStore((s) => s.statusNotifications)
  const setEnabled = usePreferencesStore((s) => s.setStatusNotifications)

  return (
    <div className="mb-3 flex items-center gap-4 rounded-2xl border-[3px] border-outline bg-surface p-4 sticker-shadow-sm">
      <span className="flex size-11 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-primary-container">
        <Bell className="size-5" aria-hidden />
      </span>
      <div className="min-w-0 flex-1">
        <p className="font-label font-bold">Status notifications</p>
        <p className="font-body text-xs text-on-background/60">
          Party activity like members joining, invites you send, and kicks.
          Invites and friend requests sent to you always come through.
        </p>
      </div>
      <Switch
        checked={enabled}
        onToggle={() => setEnabled(!enabled)}
        label="Status notifications"
      />
    </div>
  )
}

/*
 * Opt-in safety net: periodically snapshot the canvas you own while it's open.
 * Off by default, since leaving a canvas already asks whether to save. This is
 * for people who'd rather not risk a crash between saves.
 */
function AutosaveCard() {
  const enabled = usePreferencesStore((s) => s.autosaveSnapshots)
  const setEnabled = usePreferencesStore((s) => s.setAutosaveSnapshots)

  return (
    <div className="mb-3 flex items-center gap-4 rounded-2xl border-[3px] border-outline bg-surface p-4 sticker-shadow-sm">
      <span className="flex size-11 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-primary-container">
        <Save className="size-5" aria-hidden />
      </span>
      <div className="min-w-0 flex-1">
        <p className="font-label font-bold">Auto-save snapshots</p>
        <p className="font-body text-xs text-on-background/60">
          Every 15 minutes, quietly save a snapshot of a canvas you own as a
          fallback. You're still asked whether to save when you leave.
        </p>
      </div>
      <Switch
        checked={enabled}
        onToggle={() => setEnabled(!enabled)}
        label="Auto-save snapshots"
      />
    </div>
  )
}

function Switch({
  checked,
  onToggle,
  label,
}: {
  checked: boolean
  onToggle: () => void
  label: string
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      onClick={onToggle}
      className={`relative h-8 w-14 shrink-0 rounded-full border-[3px] border-outline transition-colors ${
        checked ? 'bg-primary' : 'bg-surface-dim'
      }`}
    >
      <span
        className={`absolute top-1/2 left-1 size-4 -translate-y-1/2 rounded-full border-2 border-outline bg-white transition-transform ${
          checked ? 'translate-x-6' : 'translate-x-0'
        }`}
      />
    </button>
  )
}

function UserIdCard({ userId }: { userId: string }) {
  const [copied, setCopied] = useState(false)

  async function copy() {
    if (!userId) return
    try {
      await navigator.clipboard.writeText(userId)
      setCopied(true)
      toast.success('User ID copied')
      setTimeout(() => setCopied(false), 1500)
    } catch {
      toast.error('Could not copy to clipboard')
    }
  }

  return (
    <button
      type="button"
      onClick={copy}
      title="Click to copy your user ID"
      className="mb-3 flex w-full items-center gap-4 rounded-2xl border-[3px] border-outline bg-primary-container p-4 text-left sticker-shadow-sm transition-transform hover:-translate-y-0.5"
    >
      <div className="min-w-0 flex-1">
        <p className="font-label text-xs font-bold">Your User ID</p>
        <p className="mt-0.5 truncate font-mono text-sm font-medium select-all">
          {userId || '—'}
        </p>
        <p className="mt-1 font-body text-[11px] text-on-background/60">
          Share this so friends can add you on Inkboard.
        </p>
      </div>
      <span className="flex size-9 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-surface">
        {copied ? <Check className="size-4 text-primary" aria-hidden /> : <Copy className="size-4" aria-hidden />}
      </span>
    </button>
  )
}
