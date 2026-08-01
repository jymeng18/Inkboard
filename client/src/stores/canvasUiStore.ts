import { create } from 'zustand'

export type Tool = 'pencil' | 'brush' | 'eraser' | 'shapes' | 'hand'

/*
 * UI-only canvas state: the selected tool, colour, and which side panel is
 * open. Pan/zoom live in viewportStore; committed content in sceneStore.
 * Kept separate so the Konva stage subscribes to just what it needs.
 */
interface CanvasUiState {
  tool: Tool
  color: string
  panelOpen: boolean
  friendsOpen: boolean
  setTool: (tool: Tool) => void
  setColor: (color: string) => void
  togglePanel: () => void
  toggleFriends: () => void
}

export const useCanvasUiStore = create<CanvasUiState>((set) => ({
  tool: 'pencil',
  color: '#2d2926',
  panelOpen: true,
  friendsOpen: false,
  setTool: (tool) => set({ tool }),
  setColor: (color) => set({ color }),

  /*
   * Both panels dock to the same right-hand rail, so opening one closes the
   * other rather than stacking two sheets over the drawing surface.
   */
  togglePanel: () =>
    set((s) => {
      const panelOpen = !s.panelOpen
      return { panelOpen, friendsOpen: panelOpen ? false : s.friendsOpen }
    }),

  toggleFriends: () =>
    set((s) => {
      const friendsOpen = !s.friendsOpen
      return { friendsOpen, panelOpen: friendsOpen ? false : s.panelOpen }
    }),
}))
