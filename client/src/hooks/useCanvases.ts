import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import {
  createCanvas,
  getCanvases,
  renameCanvas,
  type CanvasDto,
} from '@/api/canvas'
import { useAuthStore } from '@/stores/authStore'

/*
 * Query keys for canvas data. Keeping them here means future mutations
 * (create / rename / delete) can invalidate `canvasKeys.all` to refresh the
 * gallery without every call site re-typing the key.
 */
export const canvasKeys = {
  all: ['canvases'] as const,
}

/** Fetches the signed-in user's canvases, cached and deduped by React Query. */
export function useCanvases() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  return useQuery({
    queryKey: canvasKeys.all,
    queryFn: getCanvases,
    enabled: isAuthenticated,
  })
}

/*
 * Creates a canvas under the given name. Callers usually want to navigate into
 * the new canvas, so pass an `onSuccess` to `mutate` — it runs after this one.
 */
export function useCreateCanvas() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (name: string) => createCanvas(name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: canvasKeys.all })
    },
  })
}

/*
 * Renames a canvas, writing the new name into the cached gallery up front so
 * the card changes under the user's cursor rather than after a round trip. The
 * server also bumps `lastModifiedAt`, hence the refetch once it settles.
 */
export function useRenameCanvas() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ canvasId, name }: { canvasId: string; name: string }) =>
      renameCanvas(canvasId, name),

    onMutate: async ({ canvasId, name }) => {
      await queryClient.cancelQueries({ queryKey: canvasKeys.all })
      const previous = queryClient.getQueryData<CanvasDto[]>(canvasKeys.all)

      queryClient.setQueryData<CanvasDto[]>(canvasKeys.all, (canvases) =>
        canvases?.map((canvas) =>
          canvas.id === canvasId ? { ...canvas, name } : canvas,
        ),
      )

      return { previous }
    },

    onError: (_error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(canvasKeys.all, context.previous)
      }
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: canvasKeys.all })
    },
  })
}
