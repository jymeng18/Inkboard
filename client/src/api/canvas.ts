import api from './client'

export type CanvasDto = {
  id: string
  ownerId: string
  name: string
  snapshotURL: string | null
  createdAt: string
  lastModifiedAt: string
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
