import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Palette, Settings, Users } from 'lucide-react'

import type { CanvasDto } from '../api/canvas'
import { extractErrorMessage, removeMember as removeMemberApi } from '../api/party'
import CanvasesView from '../components/dashboard/CanvasesView'
import DashboardSidebar, { type DashboardView } from '../components/dashboard/DashboardSidebar'
import DashboardTopBar from '../components/dashboard/DashboardTopBar'
import FriendsPanel from '../components/dashboard/FriendsPanel'
import PartyView from '../components/dashboard/PartyView'
import SettingsView from '../components/dashboard/SettingsView'
import { useAuth } from '../hooks/useAuth'
import { useAuthStore } from '../stores/authStore'
import { useConnectionStore } from '../stores/connectionStore'
import { usePartyStore } from '../stores/partyStore'
import type { Friend, FriendRequest } from '../types/social'

const MOBILE_TABS: { view: DashboardView; label: string; icon: typeof Palette }[] = [
  { view: 'canvases', label: 'Canvases', icon: Palette },
  { view: 'party', label: 'Party', icon: Users },
  { view: 'settings', label: 'Settings', icon: Settings },
]

// No backend for friends yet — the panel renders its empty states until then.
const FRIENDS: Friend[] = []
const FRIEND_REQUESTS: FriendRequest[] = []

export default function DashboardPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const userName = useAuthStore((s) => s.userName)
  const currentUserId = useAuthStore((s) => s.userId)

  const partyId = usePartyStore((s) => s.partyId)
  const members = usePartyStore((s) => s.members)
  const removeMemberFromStore = usePartyStore((s) => s.removeMember)
  const presence = useConnectionStore((s) => s.presence)

  const [view, setView] = useState<DashboardView>('canvases')
  const [friendsOpen, setFriendsOpen] = useState(false)

  // Canvas fetching is a separate wire-up; the gallery shows its empty state for now.
  const canvases: CanvasDto[] = []

  async function handleLogout() {
    await logout()
    navigate('/login', { replace: true })
  }

  async function handleKick(targetUserId: string) {
    if (!partyId) return
    try {
      await removeMemberApi(partyId, targetUserId)
      removeMemberFromStore(targetUserId)
      toast.success('Removed from party')
    } catch (err) {
      toast.error(extractErrorMessage(err))
    }
  }

  const canvasComingSoon = () => toast.info('Canvas actions are coming soon.')

  return (
    <div className="flex min-h-screen flex-col bg-background font-body text-on-background">
      <DashboardTopBar
        userName={userName}
        friendsOpen={friendsOpen}
        requestCount={FRIEND_REQUESTS.length}
        onToggleFriends={() => setFriendsOpen((prev) => !prev)}
        onLogout={handleLogout}
      />

      <div className="flex flex-1">
        <DashboardSidebar active={view} onSelect={setView} partySize={members.length} />

        <main className="flex-1 overflow-x-hidden p-5 sm:p-8">
          <nav className="mb-6 flex gap-2 overflow-x-auto lg:hidden">
            {MOBILE_TABS.map(({ view: tab, label, icon: Icon }) => (
              <button
                key={tab}
                type="button"
                onClick={() => setView(tab)}
                className={`flex items-center gap-2 rounded-full border-[3px] px-4 py-2 font-label text-sm font-bold whitespace-nowrap transition-colors ${
                  view === tab
                    ? 'border-outline bg-primary text-white sticker-shadow-sm'
                    : 'border-outline/30 text-on-background/70'
                }`}
              >
                <Icon className="size-4" aria-hidden />
                {label}
              </button>
            ))}
          </nav>

          {view === 'canvases' && (
            <CanvasesView
              canvases={canvases}
              onNewCanvas={canvasComingSoon}
              onOpenCanvas={canvasComingSoon}
            />
          )}

          {view === 'party' && (
            <PartyView
              members={members}
              currentUserId={currentUserId}
              presence={presence}
              onKick={handleKick}
              onGoToCanvases={() => setView('canvases')}
            />
          )}

          {view === 'settings' && <SettingsView userName={userName} onLogout={handleLogout} />}
        </main>
      </div>

      <FriendsPanel
        open={friendsOpen}
        onClose={() => setFriendsOpen(false)}
        friends={FRIENDS}
        requests={FRIEND_REQUESTS}
      />
    </div>
  )
}
