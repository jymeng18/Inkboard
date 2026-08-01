import { useEffect, useRef } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'

import { useConnectionStore } from '@/stores/connectionStore'

/*
 * Follows the leader into a canvas they just opened for the party.
 *
 * Mounted app-wide rather than on the canvas page, because the members being
 * pulled in are usually sitting on the dashboard when it happens. It is the
 * mirror of navCount, which only ever ejects people already on a canvas.
 */
export function usePartyCanvasNavigation() {
  const canvasNav = useConnectionStore((s) => s.canvasNav)
  const navigate = useNavigate()
  const { pathname } = useLocation()

  /*
   * Seeded from whatever the store already holds rather than from zero. The
   * store outlives a sign-out, so starting at zero would fire the previous
   * account's last canvas-open at the next user the moment they sign in.
   */
  const lastCount = useRef(useConnectionStore.getState().canvasNav?.count ?? 0)

  useEffect(() => {
    if (!canvasNav || canvasNav.count <= lastCount.current) return
    lastCount.current = canvasNav.count

    /*
     * A leader re-opening the canvas the party is already on still broadcasts,
     * so the ones who never had to move stay put instead of collecting a
     * duplicate history entry and a toast about a trip they didn't take.
     */
    const path = `/canvas/${canvasNav.canvasId}`
    if (pathname === path) return

    toast.info('Your party opened a canvas — jumping in.')
    navigate(path)
  }, [canvasNav, pathname, navigate])
}
