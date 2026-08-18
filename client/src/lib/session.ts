import axios from 'axios'
import { jwtDecode } from 'jwt-decode'

import { hasSessionHint, useAuthStore } from '@/stores/authStore'
import type { AuthTokens, JwtClaims, SessionUser } from '@/types/auth'

export function decodeClaims(token: string): JwtClaims | null {
  try {
    return jwtDecode<JwtClaims>(token)
  } catch {
    return null
  }
}

/** Rebuilds the signed-in user from the access token's claims. */
export function sessionFromToken(accessToken: string): SessionUser {
  const claims = decodeClaims(accessToken)
  return {
    userId: claims?.sub ?? '',
    userName: claims?.name ?? claims?.email ?? '',
    accessToken,
  }
}

let refreshInFlight: Promise<string> | null = null

/*
 * Silent refresh off the httpOnly cookie. Deduped so concurrent 401s and the
 * startup bootstrap share one request. Resolves with the new access token and
 * hydrates the store; rejects when there is no valid session.
 */
export function refreshAccessToken(): Promise<string> {
  if (!refreshInFlight) {
    refreshInFlight = axios
      .post<AuthTokens>('/api/auth/refresh', null, { withCredentials: true })
      .then(({ data }) => {
        useAuthStore.getState().login(sessionFromToken(data.access_token))
        return data.access_token
      })
      .finally(() => {
        refreshInFlight = null
      })
  }
  return refreshInFlight
}

/** Runs once on app load to restore a session from the refresh cookie. */
export async function bootstrapSession(): Promise<void> {
  // Never signed in on this device means no refresh cookie to try, so skip the
  // request that would only 401 (e.g. a first visit to the landing page).
  if (!hasSessionHint()) {
    useAuthStore.getState().finishBootstrap()
    return
  }

  try {
    await refreshAccessToken()
  } catch {
    // The hint outlived the cookie (expired or revoked); clear it via logout so
    // the next reload doesn't retry.
    useAuthStore.getState().logout()
  } finally {
    useAuthStore.getState().finishBootstrap()
  }
}
