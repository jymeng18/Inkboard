import { create } from 'zustand'

import type { StrokeTool } from '@/components/canvas/strokeGeometry'

/*
 * Other users' in-progress strokes, streamed while they draw and cleared when the
 * finished op commits. Ephemeral like cursors (kept out of React Query and off the
 * committed scene), keyed by userId — each user has at most one live stroke.
 */
export interface LiveStroke {
  id: string
  tool: StrokeTool
  color: string
  size: number
  points: number[][]
  updatedAt: number
}

// The incremental frame a peer sends: metadata plus the points since their last frame.
export interface LiveStrokeFrame {
  id: string
  tool: StrokeTool
  color: string
  size: number
  points: number[][]
}

interface LiveStrokeState {
  strokes: Record<string, LiveStroke>
  // A new id (or a first frame) starts fresh; the same id appends.
  apply: (userId: string, frame: LiveStrokeFrame) => void
  remove: (userId: string) => void
  pruneOlderThan: (maxAgeMs: number) => void
  clear: () => void
}

export const useLiveStrokeStore = create<LiveStrokeState>((set) => ({
  strokes: {},

  apply: (userId, frame) =>
    set((s) => {
      const current = s.strokes[userId]
      const fresh = !current || current.id !== frame.id
      const next: LiveStroke = fresh
        ? { ...frame, updatedAt: Date.now() }
        : { ...current, points: [...current.points, ...frame.points], updatedAt: Date.now() }
      return { strokes: { ...s.strokes, [userId]: next } }
    }),

  remove: (userId) =>
    set((s) => {
      if (!(userId in s.strokes)) return s
      const next = { ...s.strokes }
      delete next[userId]
      return { strokes: next }
    }),

  pruneOlderThan: (maxAgeMs) =>
    set((s) => {
      const cutoff = Date.now() - maxAgeMs
      const entries = Object.entries(s.strokes).filter(([, st]) => st.updatedAt >= cutoff)
      if (entries.length === Object.keys(s.strokes).length) return s
      return { strokes: Object.fromEntries(entries) }
    }),

  clear: () => set({ strokes: {} }),
}))
