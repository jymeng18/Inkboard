import { QueryClient } from '@tanstack/react-query'

/*
 * One client for the app. Defaults tuned for a dashboard: treat data as fresh
 * for 30s (so tab-switches and remounts don't refetch), retry once on failure,
 * and revalidate when the window regains focus so a canvas made elsewhere shows
 * up on return.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: true,
    },
  },
})
