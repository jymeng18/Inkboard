import { create } from 'zustand'

export type Tool = 'pencil' | 'brush' | 'eraser' | 'shapes' | 'hand'

/*
 * UI-only canvas state: the selected tool, colour, and which docked panel is
 * open. Pan/zoom live in viewportStore; committed content in sceneStore.
 * Kept separate so the Konva stage subscribes to just what it needs.
 */
interface CanvasUiState {
  tool: Tool
  color: string
  panelOpen: boolean
  /*
   * The friends list is an invite picker layered over the party lobby. It only
   * opens from inside the invite dialog, and closes on an outside click, so it
   * gets explicit open/close rather than a public toggle.
   */
  friendsOpen: boolean
  setTool: (tool: Tool) => void
  setColor: (color: string) => void
  togglePanel: () => void
  openFriends: () => void
  closeFriends: () => void
}

export const useCanvasUiStore = create<CanvasUiState>((set) => ({
  tool: 'pencil',
  color: '#2d2926',
  panelOpen: true,
  friendsOpen: false,
  setTool: (tool) => set({ tool }),
  setColor: (color) => set({ color }),
  togglePanel: () => set((s) => ({ panelOpen: !s.panelOpen })),
  openFriends: () => set({ friendsOpen: true }),
  closeFriends: () => set({ friendsOpen: false }),
}))
