import { useState } from 'react'

import { loginUser, logoutUser, registerUser } from '@/api/auth'
import { markMobileNoticePending } from '@/lib/firstRun'
import { sessionFromToken } from '@/lib/session'
import { useAuthStore } from '@/stores/authStore'
import { usePartyStore } from '@/stores/partyStore'

/** Pulls a human-readable message out of the various server error shapes. */
function getErrorMessage(err: unknown, fallback: string): string {
  const data = (err as { response?: { data?: unknown } }).response?.data
  if (typeof data === 'string') return data

  const problem = data as
    | { detail?: string; message?: string; errors?: Record<string, string[]> }
    | undefined

  if (problem?.errors) return Object.values(problem.errors).flat().join(' ')
  return problem?.detail ?? problem?.message ?? fallback
}

export function useAuth() {
  const setSession = useAuthStore((s) => s.login)
  const clearSessionState = useAuthStore((s) => s.logout)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  /*
   * Logs in and stores the session in memory. Register reuses this because the
   * register endpoint returns no tokens, so we log the new user in right after.
   * The display name rides in the access token's claims now.
   */
  async function openSession(email: string, password: string) {
    const { access_token } = await loginUser(email, password)
    setSession(sessionFromToken(access_token))
  }

  async function login(email: string, password: string) {
    setLoading(true)
    setError(null)
    try {
      await openSession(email, password)
      return true
    } catch (err) {
      setError(getErrorMessage(err, 'Could not log in. Check your email and password.'))
      return false
    } finally {
      setLoading(false)
    }
  }

  async function register(userName: string, email: string, password: string) {
    setLoading(true)
    setError(null)
    try {
      await registerUser(userName, email, password)

      // Flagged off the account being created rather than the sign-in that
      // follows, so the notice still waits for them if auto-login below fails
      // and they come back to log in by hand.
      markMobileNoticePending()

      await openSession(email, password)
      return true
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create your account.'))
      return false
    } finally {
      setLoading(false)
    }
  }

  async function logout() {
    try {
      await logoutUser()
    } catch {
      //
    }
    // Drop the persisted party too, so the next account on this device doesn't
    // inherit a stale activePartyId.
    usePartyStore.getState().clearParty()
    clearSessionState()
  }

  return { login, register, logout, loading, error }
}
