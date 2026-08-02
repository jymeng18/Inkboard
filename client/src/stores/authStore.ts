import { create } from 'zustand'

import { clearSession, readSession, saveSession } from '@/lib/session'
import type { SessionUser } from '@/types/auth'

interface AuthState {
  userId: string
  userName: string
  accessToken: string
  isAuthenticated: boolean

  login: (user: SessionUser, refreshToken: string) => void
  logout: () => void
}

const stored = readSession()

export const useAuthStore = create<AuthState>((set) => ({
  userId: stored?.userId ?? '',
  userName: stored?.userName ?? '',
  accessToken: stored?.accessToken ?? '',
  isAuthenticated: stored !== null,

  login: (user, refreshToken) => {
    saveSession(user, refreshToken)
    set({ ...user, isAuthenticated: true })
  },

  logout: () => {
    clearSession()
    set({ userId: '', userName: '', accessToken: '', isAuthenticated: false })
  },
}))
