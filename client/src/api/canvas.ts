import api from './client'

export const DEFAULT_CANVAS_NAME = 'Untitled canvas'

/* Mirrors CanvasService, which rejects names longer than 50 characters. */
export const CANVAS_NAME_MAX_LENGTH = 50

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

/*
 * Uploads a PNG snapshot of the canvas. The endpoint binds an IFormFile named
 * `file`, so this must be multipart. Setting the content type to
 * multipart/form-data keeps axios from JSON-stringifying the FormData; axios then
 * strips it back off so the browser can fill in the real boundary.
 */
export async function uploadSnapshot(canvasId: string, blob: Blob) {
  const form = new FormData()
  form.append('file', blob, `${canvasId}.png`)
  await api.post(`/canvas/${canvasId}/snapshot`, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

/*
 * Fetches the server-rendered preview thumbnail as raw bytes. The blob container
 * is private, so this read is proxied through our API rather than hitting Azure
 * directly; the caller turns the Blob into an object URL for an <img>.
 *
 * `version` (the canvas' lastModifiedAt) makes the URL change whenever the
 * snapshot does, so the long-lived browser cache the endpoint sets stays fresh
 * without ever re-hitting the backend for an unchanged preview.
 */
export async function getSnapshotPreview(canvasId: string, version: string): Promise<Blob> {
  const { data } = await api.get(`/canvas/${canvasId}/snapshot-preview`, {
    params: { v: version },
    responseType: 'blob',
  })
  return data as Blob
}

/*
 * The persisted op-log for a canvas, ordered oldest-first. Each entry is the
 * opaque `operationData` JSON (a serialized SceneItem); the caller parses and
 * replays them to rebuild the scene on open.
 */
export async function getCanvasOperations(canvasId: string): Promise<string[]> {
  const { data } = await api.get(`/canvas/${canvasId}/operations`)
  return data as string[]
}

/*
 * Persists a single committed op directly, for when it can't be broadcast over
 * the hub (a solo owner, or before the group join). `type` matches the hub's
 * ActionType: 0 = draw, 1 = erase.
 */
export async function saveCanvasOperation(canvasId: string, type: number, operationData: string) {
  await api.post(`/canvas/${canvasId}/operations`, { type, operationData })
}
