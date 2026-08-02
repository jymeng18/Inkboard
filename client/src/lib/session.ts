import Cookies from 'js-cookie'
import { jwtDecode } from 'jwt-decode'

import type { AuthTokens, JwtClaims, SessionUser } from '@/types/auth'

const ACCESS_TOKEN = 'accessToken'
const REFRESH_TOKEN = 'refreshToken'
const USER_ID = 'userId'
const USER_NAME = 'userName'

const OPTIONS: Cookies.CookieAttributes = { expires: 7, sameSite: 'strict' }

export function getAccessToken() {
  return Cookies.get(ACCESS_TOKEN)
}

export function getRefreshToken() {
  return Cookies.get(REFRESH_TOKEN)
}

/** Rebuilds the user from cookies, or null if no one is signed in. */
export function readSession(): SessionUser | null {
  const accessToken = Cookies.get(ACCESS_TOKEN)
  const userId = Cookies.get(USER_ID)
  if (!accessToken || !userId) return null
  return { userId, userName: Cookies.get(USER_NAME) ?? '', accessToken }
}

export function saveSession(user: SessionUser, refreshToken: string) {
  Cookies.set(ACCESS_TOKEN, user.accessToken, OPTIONS)
  Cookies.set(REFRESH_TOKEN, refreshToken, OPTIONS)
  Cookies.set(USER_ID, user.userId, OPTIONS)
  Cookies.set(USER_NAME, user.userName, OPTIONS)
}

/** Swaps in a freshly refreshed token pair, leaving the user cookies untouched. */
export function setTokens(tokens: AuthTokens) {
  Cookies.set(ACCESS_TOKEN, tokens.access_token, OPTIONS)
  Cookies.set(REFRESH_TOKEN, tokens.refresh_token, OPTIONS)
}

export function clearSession() {
  ;[ACCESS_TOKEN, REFRESH_TOKEN, USER_ID, USER_NAME].forEach((key) => Cookies.remove(key))
}

export function decodeClaims(token: string): JwtClaims | null {
  try {
    return jwtDecode<JwtClaims>(token)
  } catch {
    return null
  }
}
