import { useQuery } from '@tanstack/react-query'

import { getCanvases } from '../api/canvas'
import { useAuthStore } from '../stores/authStore'

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
