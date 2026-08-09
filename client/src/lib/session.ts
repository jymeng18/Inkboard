import axios from 'axios'
import { jwtDecode } from 'jwt-decode'

import { useAuthStore } from '@/stores/authStore'
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
  try {
    await refreshAccessToken()
  } catch {
    // No valid refresh cookie; the user stays signed out.
  } finally {
    useAuthStore.getState().finishBootstrap()
  }
}
