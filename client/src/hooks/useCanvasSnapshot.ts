import { useCallback, useEffect, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { uploadSnapshot } from '@/api/canvas'
import { canvasKeys } from '@/hooks/useCanvases'
import { snapshotPreviewKeys } from '@/hooks/useSnapshotPreview'
import { renderSnapshotBlob } from '@/lib/canvasSnapshot'
import { useSceneStore, type SceneItem } from '@/stores/sceneStore'

// Periodic safety-net save while a canvas is open, per the snapshot design.
const SNAPSHOT_INTERVAL_MS = 15 * 60 * 1000

export type CaptureSnapshot = (opts?: { wait?: boolean }) => Promise<void>

/*
 * Owns snapshot capture for the canvas the owner is on. Snapshots are the owner's
 * job only, so this no-ops unless `enabled`. Runs a slow interval as a safety net
 * and returns a capture function the exit paths (end session, leave, back to
 * dashboard) call before navigating away.
 *
 * Capture reads the scene from the store and rasterises it off-stage, so it is
 * independent of the live canvas and finishes even while the page unmounts.
 */
export function useCanvasSnapshot(canvasId: string | undefined, enabled: boolean): CaptureSnapshot {
  const queryClient = useQueryClient()

  // Reference identity of the last scene we uploaded. Every commit/undo/redo/clear
  // swaps the items array, so an unchanged reference means nothing new to save.
  const lastUploadedItems = useRef<SceneItem[] | null>(null)
  const inFlight = useRef(false)

  const captureAndUpload = useCallback<CaptureSnapshot>(
    async ({ wait = false } = {}) => {
      if (!enabled || !canvasId) return

      const items = useSceneStore.getState().items
      if (items === lastUploadedItems.current || inFlight.current) return

      // Capture is best effort and must never reject, so a render failure can
      // never trap the owner mid-exit (end session awaits this).
      let blob: Blob | null
      try {
        blob = await renderSnapshotBlob(items)
      } catch {
        return
      }
      if (!blob) return

      inFlight.current = true
      const upload = uploadSnapshot(canvasId, blob)
        .then(() => {
          lastUploadedItems.current = items
          queryClient.invalidateQueries({ queryKey: canvasKeys.all })
          queryClient.invalidateQueries({ queryKey: snapshotPreviewKeys.canvas(canvasId) })
        })
        .catch(() => {
          // A failed save is retried on the next tick or exit.
        })
        .finally(() => {
          inFlight.current = false
        })

      if (wait) await upload
    },
    [canvasId, enabled, queryClient],
  )

  useEffect(() => {
    if (!enabled || !canvasId) return
    const id = window.setInterval(() => void captureAndUpload(), SNAPSHOT_INTERVAL_MS)
    return () => window.clearInterval(id)
  }, [enabled, canvasId, captureAndUpload])

  return captureAndUpload
}
