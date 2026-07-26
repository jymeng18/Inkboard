import { create } from 'zustand'

export type Tool = 'pencil' | 'brush' | 'eraser' | 'shapes' | 'hand'

/*
 * UI-only canvas state: the selected tool, colour, and whether the party panel
 * is open. Pan/zoom live in viewportStore; committed content in sceneStore.
 * Kept separate so the Konva stage subscribes to just what it needs.
 */
interface CanvasUiState {
  tool: Tool
  color: string
  panelOpen: boolean
  setTool: (tool: Tool) => void
  setColor: (color: string) => void
  togglePanel: () => void
}

export const useCanvasUiStore = create<CanvasUiState>((set) => ({
  tool: 'pencil',
  color: '#2d2926',
  panelOpen: true,
  setTool: (tool) => set({ tool }),
  setColor: (color) => set({ color }),
  togglePanel: () => set((s) => ({ panelOpen: !s.panelOpen })),
}))
