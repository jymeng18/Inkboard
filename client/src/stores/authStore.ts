import { create } from 'zustand'

import type { SessionUser } from '@/types/auth'

/*
 * A non-sensitive marker that a refresh session probably exists on this device.
 * The refresh token is an httpOnly cookie the client can't read, so this flag,
 * set on login and cleared on logout, is what lets startup skip a refresh call
 * that would just 401 for someone who was never signed in here.
 */
const SESSION_HINT_KEY = 'inkboard:hasSession'

export function hasSessionHint(): boolean {
  return localStorage.getItem(SESSION_HINT_KEY) === '1'
}

interface AuthState {
  userId: string
  userName: string
  accessToken: string
  isAuthenticated: boolean
  isBootstrapping: boolean

  login: (user: SessionUser) => void
  logout: () => void
  finishBootstrap: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  userId: '',
  userName: '',
  accessToken: '',
  isAuthenticated: false,
  // The access token lives only in memory, so a reload starts blank and the
  // startup bootstrap tries a silent refresh before we decide anything.
  isBootstrapping: true,

  login: (user) => {
    localStorage.setItem(SESSION_HINT_KEY, '1')
    set({ ...user, isAuthenticated: true })
  },

  logout: () => {
    localStorage.removeItem(SESSION_HINT_KEY)
    set({ userId: '', userName: '', accessToken: '', isAuthenticated: false })
  },

  finishBootstrap: () => set({ isBootstrapping: false }),
}))
