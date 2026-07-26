import { create } from 'zustand'

export const MIN_SCALE = 0.25
export const MAX_SCALE = 4

const clampScale = (scale: number) => Math.min(MAX_SCALE, Math.max(MIN_SCALE, scale))

/*
 * Pan (x, y) and zoom (scale) for the stage. @use-gesture drives pan/zoom into
 * here; the stage reads it as its transform. zoomTo keeps the point under the
 * cursor fixed while scaling.
 */
interface ViewportState {
  x: number
  y: number
  scale: number
  panBy: (dx: number, dy: number) => void
  zoomTo: (scale: number, center: { x: number; y: number }) => void
  reset: () => void
}

export const useViewportStore = create<ViewportState>((set) => ({
  x: 0,
  y: 0,
  scale: 1,
  panBy: (dx, dy) => set((s) => ({ x: s.x + dx, y: s.y + dy })),

  zoomTo: (scale, center) =>
    set((s) => {
      const next = clampScale(scale)
      const worldX = (center.x - s.x) / s.scale
      const worldY = (center.y - s.y) / s.scale
      return { scale: next, x: center.x - worldX * next, y: center.y - worldY * next }
    }),
    
  reset: () => set({ x: 0, y: 0, scale: 1 }),
}))
