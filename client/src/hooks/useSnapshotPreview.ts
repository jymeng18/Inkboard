import { useQuery } from '@tanstack/react-query'

import { getSnapshotPreview } from '@/api/canvas'

/*
 * Query keys for canvas preview thumbnails. `canvas` is the prefix used to
 * invalidate every version of one canvas' preview; `canvasVersion` is the full
 * key, versioned by lastModifiedAt so a new snapshot fetches fresh bytes.
 */
export const snapshotPreviewKeys = {
  all: ['snapshot-preview'] as const,
  canvas: (canvasId: string) => ['snapshot-preview', canvasId] as const,
  canvasVersion: (canvasId: string, version: string) =>
    ['snapshot-preview', canvasId, version] as const,
}

/*
 * Fetches the proxied preview thumbnail for one canvas as a Blob. Only enabled
 * when the canvas actually has a stored snapshot, so cards without one never hit
 * the endpoint (which would 404). Cached long here and in the browser; a changed
 * `version` is what pulls a fresh image.
 */
export function useSnapshotPreview(canvasId: string, version: string, enabled: boolean) {
  return useQuery({
    queryKey: snapshotPreviewKeys.canvasVersion(canvasId, version),
    queryFn: () => getSnapshotPreview(canvasId, version),
    enabled,
    staleTime: 5 * 60 * 1000,
  })
}
