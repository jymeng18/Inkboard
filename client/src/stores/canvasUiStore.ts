import { create } from 'zustand'

export type Tool = 'pencil' | 'brush' | 'eraser' | 'shapes' | 'ruler' | 'hand'

/*
 * The brush sub-tools, chosen from the brush dropdown. These map 1:1 onto the
 * perfect-freehand tunings in strokeGeometry, so the active variant is what gets
 * stored on a committed stroke and replayed everywhere.
 */
export type BrushVariant = 'brush' | 'marker' | 'calligraphy'

/* The outline shapes drawn by the shapes tool, chosen from the shapes dropdown. */
export type ShapeKind = 'rectangle' | 'ellipse' | 'triangle' | 'diamond' | 'star'

/*
 * Stroke thickness range, in world pixels, shared by the size slider, the drawing
 * cursor, and the stored strokes. The slider's lowest position is `default`, and
 * dragging up moves toward `max`. Change these to retune the whole range at once.
 */
export const STROKE_SIZE = {
  min: 4,
  max: 100,
  default: 4,
  step: 1,
} as const

/*
 * UI-only canvas state: the selected tool, colour, and which docked panel is
 * open. Pan/zoom live in viewportStore; committed content in sceneStore.
 * Kept separate so the Konva stage subscribes to just what it needs.
 */
interface CanvasUiState {
  tool: Tool
  /* Which brush is active; used whenever `tool` is 'brush'. */
  brushVariant: BrushVariant
  /* Which shape the shapes tool draws. */
  shapeKind: ShapeKind
  color: string
  size: number
  panelOpen: boolean
  /*
   * The friends list is an invite picker layered over the party lobby. It only
   * opens from inside the invite dialog, and closes on an outside click, so it
   * gets explicit open/close rather than a public toggle.
   */
  friendsOpen: boolean
  setTool: (tool: Tool) => void
  setBrushVariant: (variant: BrushVariant) => void
  setShapeKind: (shape: ShapeKind) => void
  setColor: (color: string) => void
  setSize: (size: number) => void
  togglePanel: () => void
  openFriends: () => void
  closeFriends: () => void
}

export const useCanvasUiStore = create<CanvasUiState>((set) => ({
  tool: 'pencil',
  brushVariant: 'brush',
  shapeKind: 'rectangle',
  color: '#2d2926',
  size: STROKE_SIZE.default,
  panelOpen: true,
  friendsOpen: false,
  setTool: (tool) => set({ tool }),
  setBrushVariant: (brushVariant) => set({ brushVariant }),
  setShapeKind: (shapeKind) => set({ shapeKind }),
  setColor: (color) => set({ color }),
  setSize: (size) => set({ size }),
  togglePanel: () => set((s) => ({ panelOpen: !s.panelOpen })),
  openFriends: () => set({ friendsOpen: true }),
  closeFriends: () => set({ friendsOpen: false }),
}))
