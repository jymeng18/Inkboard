import { useCallback, useEffect, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import { uploadSnapshot } from '@/api/canvas'
import { canvasKeys } from '@/hooks/useCanvases'
import { snapshotPreviewKeys } from '@/hooks/useSnapshotPreview'
import { renderSnapshotBlob } from '@/lib/canvasSnapshot'
import { usePreferencesStore } from '@/stores/preferencesStore'
import { useSceneStore, type SceneItem } from '@/stores/sceneStore'

// Interval for the opt-in background safety-net save.
const SNAPSHOT_INTERVAL_MS = 15 * 60 * 1000

export interface CanvasSnapshotApi {
  // Renders and uploads a snapshot. `wait` awaits the upload (used by the exit
  // prompts) and surfaces a toast if it fails; without it the call is background.
  save: (opts?: { wait?: boolean }) => Promise<void>
  // Whether the owner has drawn something not yet in the latest saved snapshot.
  hasUnsavedChanges: () => boolean
}

/*
 * Owns snapshot capture for the canvas the owner is on. Snapshots are the owner's
 * job only, so this no-ops unless `enabled`. Saving is otherwise the owner's
 * explicit choice on exit; the periodic save is a safety net gated behind a
 * preference. Capture reads the scene from the store and rasterises it off-stage,
 * so it is independent of the live canvas and finishes even while the page unmounts.
 */
export function useCanvasSnapshot(canvasId: string | undefined, enabled: boolean): CanvasSnapshotApi {
  const queryClient = useQueryClient()
  const autosave = usePreferencesStore((s) => s.autosaveSnapshots)

  // Reference identity of the last scene we uploaded. Every commit/undo/redo/clear
  // swaps the items array, so an unchanged reference means nothing new to save.
  const lastUploadedItems = useRef<SceneItem[] | null>(null)
  const inFlight = useRef(false)

  const save = useCallback(
    async ({ wait = false }: { wait?: boolean } = {}) => {
      if (!enabled || !canvasId) return

      const items = useSceneStore.getState().items
      if (items === lastUploadedItems.current || inFlight.current) return

      // Capture is best effort and must never reject, so a render failure can
      // never trap the owner mid-exit (the save prompt awaits this).
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
          // Only nag when the owner explicitly asked to save; background ticks
          // fail quietly and retry on the next tick or exit.
          if (wait) toast.error('Could not save your canvas. Your changes are still here.')
        })
        .finally(() => {
          inFlight.current = false
        })

      if (wait) await upload
    },
    [canvasId, enabled, queryClient],
  )

  const hasUnsavedChanges = useCallback(() => {
    if (!enabled) return false
    const items = useSceneStore.getState().items
    return items.length > 0 && items !== lastUploadedItems.current
  }, [enabled])

  useEffect(() => {
    if (!enabled || !canvasId || !autosave) return
    const id = window.setInterval(() => void save(), SNAPSHOT_INTERVAL_MS)
    return () => window.clearInterval(id)
  }, [enabled, canvasId, autosave, save])

  return { save, hasUnsavedChanges }
}
