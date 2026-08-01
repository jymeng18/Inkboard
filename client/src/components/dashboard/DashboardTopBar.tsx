import { BookUser, LogOut } from 'lucide-react'

import { Button } from '@/components/ui/button'

interface DashboardTopBarProps {
  userName: string
  friendsOpen: boolean
  requestCount: number
  onToggleFriends: () => void
  onLogout: () => void
}

export default function DashboardTopBar({
  userName,
  friendsOpen,
  requestCount,
  onToggleFriends,
  onLogout,
}: DashboardTopBarProps) {
  const initial = userName.trim().charAt(0).toUpperCase() || '?'

  return (
    <header className="sticky top-0 z-30 flex items-center justify-between border-b-4 border-outline bg-surface px-5 py-3">
      <div className="flex items-center gap-3">
        <img src="/pen.svg" alt="" className="h-8 -rotate-[30deg]" />
        <span className="font-display text-2xl">Inkboard</span>
      </div>

      <div className="flex items-center gap-3">
        <Button
          variant={friendsOpen ? 'primary' : 'surface'}
          size="sm"
          onClick={onToggleFriends}
          aria-expanded={friendsOpen}
          className="relative"
        >
          <BookUser />
          <span className="hidden sm:inline">Friends</span>
          {requestCount > 0 && (
            <span className="absolute -top-2 -right-2 flex size-5 items-center justify-center rounded-full border-2 border-outline bg-primary font-label text-[10px] font-bold text-white">
              {requestCount}
            </span>
          )}
        </Button>

        <span
          className="flex size-10 items-center justify-center rounded-full border-[3px] border-outline bg-secondary-container font-display text-lg sticker-shadow-sm"
          title={userName || 'You'}
        >
          {initial}
        </span>

        <Button variant="ghost" size="icon" onClick={onLogout} aria-label="Log out">
          <LogOut />
        </Button>
      </div>
    </header>
  )
}
