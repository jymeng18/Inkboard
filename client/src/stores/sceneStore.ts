import { create } from 'zustand'

import type { StrokeTool } from '@/components/canvas/strokeGeometry'
import type { ShapeKind } from './canvasUiStore'

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
  tool: StrokeTool
  color: string
  size: number
  points: number[][]
}

/*
 * An outline shape defined by its bounding box. Direction-independent, so the box
 * is stored already normalized (top-left origin + positive extents) and every
 * shape kind is drawn to fit it.
 */
export type ShapeItem = {
  id: string
  kind: 'shape'
  shape: ShapeKind
  color: string
  x: number
  y: number
  width: number
  height: number
  strokeWidth: number
}

/* A straight line segment (the ruler tool). Keeps real endpoints, since a line's
 * direction is lost if it's flattened to a bounding box. */
export type LineItem = {
  id: string
  kind: 'line'
  color: string
  x1: number
  y1: number
  x2: number
  y2: number
  strokeWidth: number
}

/* Legacy rectangle op, kept so op-logs written before the generalized ShapeItem
 * still replay. New rectangles are stored as ShapeItem with shape 'rectangle'. */
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

export type SceneItem = StrokeItem | ShapeItem | LineItem | RectItem

interface SceneState {
  items: SceneItem[]
  redoStack: SceneItem[]
  add: (item: SceneItem) => void
  addRemote: (item: SceneItem) => void
  addManyRemote: (items: SceneItem[]) => void
  undo: () => void
  redo: () => void
  clear: () => void
}

export const useSceneStore = create<SceneState>((set) => ({
  items: [],
  redoStack: [],

  add: (item) => set((s) => ({ items: [...s.items, item], redoStack: [] })),

  // Applies an op received from another client: append only if we don't already
  // have it (dedupe by id), and never clear our own redo stack.
  addRemote: (item) =>
    set((s) => (s.items.some((i) => i.id === item.id) ? s : { items: [...s.items, item] })),

  // Replay a batch (op-log hydrate on open) in one update, skipping any we already
  // have so it reconciles with live ops that arrived during the fetch.
  addManyRemote: (items) =>
    set((s) => {
      const seen = new Set(s.items.map((i) => i.id))
      const additions = items.filter((i) => !seen.has(i.id))
      return additions.length === 0 ? s : { items: [...s.items, ...additions] }
    }),
  
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
