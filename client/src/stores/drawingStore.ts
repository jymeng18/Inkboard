import { create } from 'zustand'

import type { StrokeTool } from '@/components/canvas/strokeGeometry'
import { STROKE_SIZE, type ShapeKind } from './canvasUiStore'

/*
 * The single in-progress stroke or shape. Only <CurrentStroke> subscribes here,
 * so updating it while drawing re-renders that one component and nothing else.
 * Points are [x, y, pressure] in world coordinates.
 */
type Mode = 'stroke' | 'shape' | null

/* The ruler draws a straight line, previewed with the same start/head drag as the
 * shapes, so it rides the shape machinery under this extra kind. */
export type DrawShape = ShapeKind | 'line'

interface DrawingState {
  mode: Mode
  // Stable id assigned at stroke start, so live frames and the committed op share it.
  id: string | null
  tool: StrokeTool
  shape: DrawShape | null
  color: string
  size: number
  points: number[][]
  start: [number, number] | null
  head: [number, number] | null
  beginStroke: (id: string, tool: StrokeTool, color: string, size: number, point: number[]) => void
  setPoints: (points: number[][]) => void
  beginShape: (shape: DrawShape, color: string, point: [number, number]) => void
  setHead: (point: [number, number]) => void
  reset: () => void
}

export const useDrawingStore = create<DrawingState>((set) => ({
  mode: null,
  id: null,
  tool: 'pencil',
  shape: null,
  color: '#2d2926',
  size: STROKE_SIZE.default,
  points: [],
  start: null,
  head: null,
  beginStroke: (id, tool, color, size, point) =>
    set({ mode: 'stroke', id, tool, color, size, points: [point], start: null, head: null }),
  setPoints: (points) => set({ points }),
  beginShape: (shape, color, point) =>
    set({ mode: 'shape', id: null, shape, color, points: [], start: point, head: point }),
  setHead: (head) => set({ head }),
  reset: () => set({ mode: null, id: null, shape: null, points: [], start: null, head: null }),
}))
