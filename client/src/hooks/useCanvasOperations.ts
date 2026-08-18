import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'

import { getCanvasOperations } from '@/api/canvas'
import { useAuthStore } from '@/stores/authStore'
import { useSceneStore, type SceneItem } from '@/stores/sceneStore'

/*
 * Hydrates the scene from the persisted op-log when a canvas opens, so an owner
 * reopening or a member joining mid-session sees the existing work. Fetched fresh
 * each open (no caching), and applied via addManyRemote so live ops that land
 * during the fetch reconcile by id rather than duplicating.
 */
export function useCanvasOperations(canvasId: string | undefined) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  const { data, isLoading } = useQuery({
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

  return { isHydrating: isLoading }
}
