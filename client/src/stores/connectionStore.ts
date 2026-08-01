import { create } from 'zustand'

type ConnectionState = {
  connected: boolean
  lastEvent: { event: string; userId: string } | null
  navCount: number
  /*
   * Set when the leader opens a canvas for the party. The counter is what makes
   * it re-fire for the same canvas twice in a row.
   */
  canvasNav: { canvasId: string; count: number } | null
  /** userId -> online. Absent means "assume online" (see usePresence default). */
  presence: Record<string, boolean>
  setConnected: (val: boolean) => void
  setLastEvent: (event: { event: string; userId: string } | null) => void
  triggerNavToDashboard: () => void
  triggerNavToCanvas: (canvasId: string) => void
  setPresence: (userId: string, online: boolean) => void
  resetPresence: () => void
}

export const useConnectionStore = create<ConnectionState>((set) => ({
  connected: false,
  lastEvent: null,
  navCount: 0,
  canvasNav: null,
  presence: {},
  setConnected: (connected) => set({ connected }),
  setLastEvent: (lastEvent) => set({ lastEvent }),
  triggerNavToDashboard: () => set((s) => ({ navCount: s.navCount + 1 })),
  triggerNavToCanvas: (canvasId) =>
    set((s) => ({ canvasNav: { canvasId, count: (s.canvasNav?.count ?? 0) + 1 } })),
  setPresence: (userId, online) =>
    set((s) => ({ presence: { ...s.presence, [userId]: online } })),
  resetPresence: () => set({ presence: {} }),
}))
