import { Bell, LogOut, Palette, UserCog } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

import { Button } from '@/components/ui/button'

const SETTINGS: { icon: LucideIcon; title: string; description: string }[] = [
  { icon: UserCog, title: 'Account', description: 'Display name, email, and password.' },
  { icon: Palette, title: 'Canvas defaults', description: 'Starting tool, grid, and colors.' },
  { icon: Bell, title: 'Notifications', description: 'Party invites and friend requests.' },
]

interface SettingsViewProps {
  userName: string
  onLogout: () => void
}

export default function SettingsView({ userName, onLogout }: SettingsViewProps) {
  return (
    <section className="max-w-2xl">
      <div className="mb-6">
        <h1 className="font-display text-3xl leading-none sm:text-4xl">Settings</h1>
        <p className="mt-1 font-body text-sm text-on-background/70">
          Signed in as {userName || 'your account'}.
        </p>
      </div>

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
