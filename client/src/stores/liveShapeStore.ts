import { create } from 'zustand'

import type { DrawShape } from './drawingStore'

/*
 * Other users' in-progress shapes and ruler lines, streamed while they drag them
 * out and cleared when the finished op commits. Ephemeral like cursors and live
 * strokes: kept off the committed scene and keyed by userId, since each user is
 * dragging at most one shape at a time. A frame is the whole shape (its two
 * corner points), so unlike strokes there's nothing to accumulate — each frame
 * replaces the last.
 */
export interface LiveShapeFrame {
  shape: DrawShape
  color: string
  start: [number, number]
  head: [number, number]
}

export interface LiveShape extends LiveShapeFrame {
  updatedAt: number
}

interface LiveShapeState {
  shapes: Record<string, LiveShape>
  upsert: (userId: string, frame: LiveShapeFrame) => void
  remove: (userId: string) => void
  pruneOlderThan: (maxAgeMs: number) => void
  clear: () => void
}

export const useLiveShapeStore = create<LiveShapeState>((set) => ({
  shapes: {},

  upsert: (userId, frame) =>
    set((s) => ({ shapes: { ...s.shapes, [userId]: { ...frame, updatedAt: Date.now() } } })),

  remove: (userId) =>
    set((s) => {
      if (!(userId in s.shapes)) return s
      const next = { ...s.shapes }
      delete next[userId]
      return { shapes: next }
    }),

  pruneOlderThan: (maxAgeMs) =>
    set((s) => {
      const cutoff = Date.now() - maxAgeMs
      const entries = Object.entries(s.shapes).filter(([, sh]) => sh.updatedAt >= cutoff)
      if (entries.length === Object.keys(s.shapes).length) return s
      return { shapes: Object.fromEntries(entries) }
    }),

  clear: () => set({ shapes: {} }),
}))
