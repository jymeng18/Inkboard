import { create } from 'zustand'

type ConnectionState = {
  connected: boolean
  lastEvent: { event: string; userId: string } | null
  navCount: number
  setConnected: (val: boolean) => void
  setLastEvent: (event: { event: string; userId: string } | null) => void
  triggerNavToDashboard: () => void
}

export const useConnectionStore = create<ConnectionState>((set) => ({
  connected: false,
  lastEvent: null,
  navCount: 0,
  setConnected: (connected) => set({ connected }),
  setLastEvent: (lastEvent) => set({ lastEvent }),
  triggerNavToDashboard: () => set((s) => ({ navCount: s.navCount + 1 })),
}))
