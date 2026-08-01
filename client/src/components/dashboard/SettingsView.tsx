import { useState } from 'react'
import { toast } from 'sonner'
import { Bell, Check, Copy, LogOut, Palette, UserCog } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { usePreferencesStore } from '@/stores/preferencesStore'

const SETTINGS: { icon: LucideIcon; title: string; description: string }[] = [
  { icon: UserCog, title: 'Account', description: 'Display name, email, and password.' },
  { icon: Palette, title: 'Canvas defaults', description: 'Starting tool, grid, and colors.' },
]

interface SettingsViewProps {
  userId: string
  userName: string
  onLogout: () => void
}

export default function SettingsView({ userId, userName, onLogout }: SettingsViewProps) {
  return (
    <section className="max-w-2xl">
      <div className="mb-6">
        <h1 className="font-display text-3xl leading-none sm:text-4xl">Settings</h1>
        <p className="mt-1 font-body text-sm text-on-background/70">
          Signed in as {userName || 'your account'}.
        </p>
      </div>

      <UserIdCard userId={userId} />

      <NotificationsCard />

      <ul className="flex flex-col gap-3">
        {SETTINGS.map(({ icon: Icon, title, description }) => (
          <li
            key={title}
            className="flex items-center gap-4 rounded-2xl border-[3px] border-outline bg-surface p-4 sticker-shadow-sm"
          >
            <span className="flex size-11 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-primary-container">
              <Icon className="size-5" aria-hidden />
            </span>
            <div className="flex-1">
              <p className="font-label font-bold">{title}</p>
              <p className="font-body text-xs text-on-background/60">{description}</p>
            </div>
          </li>
        ))}
      </ul>

      <Button variant="surface" size="md" className="mt-6" onClick={onLogout}>
        <LogOut />
        Log out
      </Button>
    </section>
  )
}

/*
 * The one live setting so far: mute the ambient party and social activity
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
          Share this so friends can invite you to their party.
        </p>
      </div>
      <span className="flex size-9 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-surface">
        {copied ? <Check className="size-4 text-primary" aria-hidden /> : <Copy className="size-4" aria-hidden />}
      </span>
    </button>
  )
}
