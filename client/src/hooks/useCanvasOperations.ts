import { useEffect, useRef } from 'react'
import { useQuery } from '@tanstack/react-query'

import { getCanvasOperations } from '@/api/canvas'
import { useAuthStore } from '@/stores/authStore'
import { useConnectionStore } from '@/stores/connectionStore'
import { useSceneStore, type SceneItem } from '@/stores/sceneStore'

// The server persists hub ops through a background write queue that flushes on a
// ~2s window, so an op committed just before we join the group is neither live to
// us yet nor in the DB. Wait past that window before the reconciling backfill.
const RECONCILE_DELAY_MS = 2500

/*
 * Hydrates the scene from the persisted op-log when a canvas opens, so an owner
 * reopening or a member joining mid-session sees the existing work. Fetched fresh
 * each open (no caching), and applied via addManyRemote so live ops that land
 * during the fetch reconcile by id rather than duplicating.
 *
 * When this client confirms its canvas-group join (a party member landing in the
 * leader's canvas), a second fetch backfills any ops the leader made during the
 * follow countdown that the first fetch raced. The dedupe makes the replay safe.
 */
export function useCanvasOperations(canvasId: string | undefined) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const lastCanvasJoin = useConnectionStore((s) => s.lastCanvasJoin)

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['canvas-operations', canvasId],
    queryFn: () => getCanvasOperations(canvasId as string),
    enabled: !!canvasId && isAuthenticated,
    staleTime: 0,
    gcTime: 0,
  })

  useEffect(() => {
    if (!data) return
    const items: SceneItem[] = []
    for (const raw of data) {
      try {
        items.push(JSON.parse(raw) as SceneItem)
      } catch {
        // Skip a malformed op rather than failing the whole replay.
      }
    }
    if (items.length > 0) useSceneStore.getState().addManyRemote(items)
  }, [data])

  const lastJoinCount = useRef(0)
  useEffect(() => {
    if (!lastCanvasJoin || lastCanvasJoin.canvasId !== canvasId) return
    if (lastCanvasJoin.count <= lastJoinCount.current) return
    lastJoinCount.current = lastCanvasJoin.count

    const id = window.setTimeout(() => void refetch(), RECONCILE_DELAY_MS)
    return () => window.clearTimeout(id)
  }, [lastCanvasJoin, canvasId, refetch])

  return { isHydrating: isLoading }
}
