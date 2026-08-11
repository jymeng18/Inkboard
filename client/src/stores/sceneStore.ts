import { create } from 'zustand'

/*
 * Committed canvas content. Kept deliberately flat and serialisable so it can be
 * broadcast/persisted as-is. Strokes store the raw [x, y, pressure] input, which
 * is the canonical operation: small, resolution-independent, and re-renderable
 * with any brush tuning. The perfect-freehand outline is computed (and memoized)
 * at render time rather than baked in here.
 */
export type StrokeItem = {
  id: string
  kind: 'stroke'
  tool: 'pencil' | 'brush' | 'eraser'
  color: string
  points: number[][]
}

export type RectItem = {
  id: string
  kind: 'rect'
  color: string
  x: number
  y: number
  width: number
  height: number
  strokeWidth: number
}

export type SceneItem = StrokeItem | RectItem

interface SceneState {
  items: SceneItem[]
  redoStack: SceneItem[]
  add: (item: SceneItem) => void
  undo: () => void
  redo: () => void
  clear: () => void
}

export const useSceneStore = create<SceneState>((set) => ({
  items: [],
  redoStack: [],

  add: (item) => set((s) => ({ items: [...s.items, item], redoStack: [] })),
  
  undo: () =>
    set((s) => {
      if (s.items.length === 0) return s
      const last = s.items[s.items.length - 1]
      return { items: s.items.slice(0, -1), redoStack: [...s.redoStack, last] }
    }),
    
  redo: () =>
    set((s) => {
      if (s.redoStack.length === 0) return s
      const last = s.redoStack[s.redoStack.length - 1]
      return { items: [...s.items, last], redoStack: s.redoStack.slice(0, -1) }
    }),

  clear: () => set({ items: [], redoStack: [] }),
}))
