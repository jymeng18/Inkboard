import { Navigate, Outlet } from 'react-router-dom'

import { usePartyHub } from '../hooks/usePartyHub'
import { useAuthStore } from '../stores/authStore'

/*
 * Gate for every page that isn't the landing or login. Redirects signed-out
 * visitors to /login and, once authorized, keeps a single PartyHub connection
 * alive across all protected routes so presence and invites work everywhere.
 */
export default function RequireAuth() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  // Hooks must run unconditionally; the hub no-ops until there's a token.
  usePartyHub()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
