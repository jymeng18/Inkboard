import { create } from 'zustand'

import { STROKE_SIZE, type Tool } from './canvasUiStore'

/*
 * The single in-progress stroke or shape. Only <CurrentStroke> subscribes here,
 * so updating it while drawing re-renders that one component and nothing else.
 * Points are [x, y, pressure] in world coordinates.
 */
type Mode = 'stroke' | 'shape' | null

interface DrawingState {
  mode: Mode
  tool: Tool
  color: string
  size: number
  points: number[][]
  start: [number, number] | null
  head: [number, number] | null
  beginStroke: (tool: Tool, color: string, size: number, point: number[]) => void
  setPoints: (points: number[][]) => void
  beginShape: (tool: Tool, color: string, point: [number, number]) => void
  setHead: (point: [number, number]) => void
  reset: () => void
}

export const useDrawingStore = create<DrawingState>((set) => ({
  mode: null,
  tool: 'pencil',
  color: '#2d2926',
  size: STROKE_SIZE.default,
  points: [],
  start: null,
  head: null,
  beginStroke: (tool, color, size, point) =>
    set({ mode: 'stroke', tool, color, size, points: [point], start: null, head: null }),
  setPoints: (points) => set({ points }),
  beginShape: (tool, color, point) =>
    set({ mode: 'shape', tool, color, points: [], start: point, head: point }),
  setHead: (head) => set({ head }),
  reset: () => set({ mode: null, points: [], start: null, head: null }),
}))
