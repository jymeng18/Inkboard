import { create } from 'zustand'

/*
 * Remote collaborators' cursors, in world coordinates. Kept out of React Query
 * (this is a live socket feed, not a server read) and off the Konva stage, so
 * the high-frequency updates only ever repaint the cursor overlay.
 */
export interface RemoteCursor {
  userId: string
  x: number
  y: number
  updatedAt: number
}

interface CursorState {
  cursors: Record<string, RemoteCursor>
  upsert: (userId: string, x: number, y: number) => void
  pruneOlderThan: (maxAgeMs: number) => void
  clear: () => void
}

export const useCursorStore = create<CursorState>((set) => ({
  cursors: {},

  upsert: (userId, x, y) =>
    set((s) => ({
      cursors: { ...s.cursors, [userId]: { userId, x, y, updatedAt: Date.now() } },
    })),

  // Drop cursors that have gone quiet (a collaborator who left, went idle, or
  // dropped) so they don't linger frozen on the board.
  pruneOlderThan: (maxAgeMs) =>
    set((s) => {
      const cutoff = Date.now() - maxAgeMs
      const kept = Object.values(s.cursors).filter((c) => c.updatedAt >= cutoff)
      if (kept.length === Object.keys(s.cursors).length) return s
      return { cursors: Object.fromEntries(kept.map((c) => [c.userId, c])) }
    }),

  clear: () => set({ cursors: {} }),
}))
