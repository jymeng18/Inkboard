import { Navigate, Outlet } from 'react-router-dom'

import FollowCountdownOverlay from '@/components/canvas/party/FollowCountdownOverlay'
import { useFriendRequestNotifications } from '@/hooks/useFriendRequestNotifications'
import { usePartyCanvasNavigation } from '@/hooks/usePartyCanvasNavigation'
import { usePartyHub } from '@/hooks/usePartyHub'
import { usePartyHydration } from '@/hooks/usePartyHydration'
import { useAuthStore } from '@/stores/authStore'

/*
 * Gate for every page that isn't the landing or login. Redirects signed-out
 * visitors to /login and, once authorized, keeps a single PartyHub connection
 * alive across all protected routes so presence and invites work everywhere.
 */
export default function RequireAuth() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const isBootstrapping = useAuthStore((s) => s.isBootstrapping)

  // Hooks must run unconditionally; the hub no-ops until there's a token.
  usePartyHub()

  // Rebuilds party state from the server after a reload wipes the in-memory
  // stores, so the session lock (and party UI) survive a hard refresh.
  usePartyHydration()

  // One poll for the whole session, so a friend request finds the user wherever
  // they are rather than only on the dashboard.
  useFriendRequestNotifications()

  // Members are usually on the dashboard when the leader opens a canvas, so the
  // listener that follows them in has to live above both pages.
  const pendingFollow = usePartyCanvasNavigation()

  // Don't decide until the startup silent refresh resolves, or a reload would
  // bounce a signed-in user to /login before their session is restored.
  if (isBootstrapping) {
    return (
      <div className="flex min-h-screen items-center justify-center font-label text-on-background/60">
        Restoring your session…
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return (
    <>
      <Outlet />
      {pendingFollow && (
        <FollowCountdownOverlay key={pendingFollow.path} onComplete={pendingFollow.follow} />
      )}
    </>
  )
}
