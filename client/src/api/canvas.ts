import api from './client'

export const DEFAULT_CANVAS_NAME = 'Untitled canvas'
export const CANVAS_NAME_MAX_LENGTH = 60

export type CanvasDto = {
  id: string
  ownerId: string
  // Nullable on the server, and canvases created before naming existed have no name.
  name: string | null
  snapshotURL: string | null
  createdAt: string
  lastModifiedAt: string
}

export function canvasDisplayName(canvas: CanvasDto): string {
  return canvas.name?.trim() || DEFAULT_CANVAS_NAME
}

export function normalizeCanvasName(name: string): string {
  return name.trim().slice(0, CANVAS_NAME_MAX_LENGTH) || DEFAULT_CANVAS_NAME
}

export async function createCanvas(name: string) {
  const { data } = await api.post('/canvas', { name })
  return data as CanvasDto
}

export async function getCanvases() {
  const { data } = await api.get('/canvas')
  return data as CanvasDto[]
}

export async function deleteCanvas(canvasId: string) {
  await api.delete(`/canvas/${canvasId}`)
}

export async function renameCanvas(canvasId: string, name: string) {
  await api.put(`/canvas/${canvasId}`, { name })
}
