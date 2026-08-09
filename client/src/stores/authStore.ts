import { create } from 'zustand'

import type { SessionUser } from '@/types/auth'

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

  login: (user) => set({ ...user, isAuthenticated: true }),

  logout: () => set({ userId: '', userName: '', accessToken: '', isAuthenticated: false }),

  finishBootstrap: () => set({ isBootstrapping: false }),
}))
