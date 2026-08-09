import { useCallback, useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'

import { useConnectionStore } from '@/stores/connectionStore'

/*
 * Follows the leader into a canvas they just opened for the party.
 *
 * Mounted app-wide rather than on the canvas page, because the members being
 * pulled in are usually sitting on the dashboard when it happens. It is the
 * mirror of navCount, which only ever ejects people already on a canvas.
 *
 * The jump isn't instant: a broadcast arms a pending path, and the countdown
 * overlay (rendered by the caller) is what finally calls follow(). The old
 * session is already gone by then, so the wait is spent watching, not drawing.
 */
export function usePartyCanvasNavigation() {
  const navigate = useNavigate()
  const { pathname } = useLocation()

  const [pendingPath, setPendingPath] = useState<string | null>(null)

  /*
   * Seeded from whatever the store already holds rather than from zero. The
   * store outlives a sign-out, so starting at zero would fire the previous
   * account's last canvas-open at the next user the moment they sign in.
   */
  const lastCount = useRef(useConnectionStore.getState().canvasNav?.count ?? 0)

  // Read inside the store subscription, which is set up once, so it always sees
  // where the member actually is rather than the pathname captured at mount.
  const pathnameRef = useRef(pathname)
  useEffect(() => {
    pathnameRef.current = pathname
  }, [pathname])

  useEffect(() => {
    const arm = (nav: { canvasId: string; count: number } | null) => {
      if (!nav || nav.count <= lastCount.current) return
      lastCount.current = nav.count

      /*
       * A leader re-opening the canvas the party is already on still broadcasts,
       * so the ones who never had to move stay put instead of collecting a
       * duplicate history entry and a countdown for a trip they didn't take.
       */
      const path = `/canvas/${nav.canvasId}`
      if (pathnameRef.current === path) return

      setPendingPath(path)
    }

    return useConnectionStore.subscribe((s) => arm(s.canvasNav))
  }, [])

  const follow = useCallback(() => {
    if (!pendingPath) return
    navigate(pendingPath)
    setPendingPath(null)
  }, [pendingPath, navigate])

  return pendingPath ? { path: pendingPath, follow } : null
}
