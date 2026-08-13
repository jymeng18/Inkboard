import { create } from 'zustand'

export type Tool = 'pencil' | 'brush' | 'eraser' | 'shapes' | 'hand'

/*
 * Stroke thickness range, in world pixels, shared by the size slider, the drawing
 * cursor, and the stored strokes. The slider's lowest position is `default`, and
 * dragging up moves toward `max`. Change these to retune the whole range at once.
 */
export const STROKE_SIZE = {
  min: 4,
  max: 40,
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
  setColor: (color: string) => void
  setSize: (size: number) => void
  togglePanel: () => void
  openFriends: () => void
  closeFriends: () => void
}

export const useCanvasUiStore = create<CanvasUiState>((set) => ({
  tool: 'pencil',
  color: '#2d2926',
  size: STROKE_SIZE.default,
  panelOpen: true,
  friendsOpen: false,
  setTool: (tool) => set({ tool }),
  setColor: (color) => set({ color }),
  setSize: (size) => set({ size }),
  togglePanel: () => set((s) => ({ panelOpen: !s.panelOpen })),
  openFriends: () => set({ friendsOpen: true }),
  closeFriends: () => set({ friendsOpen: false }),
}))
