import { Navigate, Outlet } from 'react-router-dom'

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
  usePartyCanvasNavigation()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
