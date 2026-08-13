import { useEffect, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import CanvasFriendsPanel from '@/components/canvas/CanvasFriendsPanel'
import CanvasStage from '@/components/canvas/CanvasStage'
import CanvasToolbar from '@/components/canvas/CanvasToolbar'
import CursorLayer from '@/components/canvas/CursorLayer'
import PartyPanel from '@/components/canvas/PartyPanel'
import ZoomControls from '@/components/canvas/ZoomControls'
import { useCanvasHub } from '@/hooks/useCanvasHub'
import { useCanvasParty } from '@/hooks/useCanvasParty'
import { useCanvases } from '@/hooks/useCanvases'
import { useCanvasSnapshot } from '@/hooks/useCanvasSnapshot'
import { useAuthStore } from '@/stores/authStore'
import { useConnectionStore } from '@/stores/connectionStore'
import { useSceneStore } from '@/stores/sceneStore'
import { useViewportStore } from '@/stores/viewportStore'

export default function CanvasPage() {
  const { canvasId } = useParams()
  const navigate = useNavigate()
  const party = useCanvasParty(canvasId)

  // Only the canvas owner uploads snapshots. getCanvases returns solely the
  // caller's own canvases, so finding it here is the ownership check.
  const userId = useAuthStore((s) => s.userId)
  const { data: canvases } = useCanvases()
  const isOwner = !!canvasId && !!canvases?.some((c) => c.id === canvasId && c.ownerId === userId)
  const snapshot = useCanvasSnapshot(canvasId, isOwner)

  // Live cursor sync for this canvas (operations still out of scope).
  useCanvasHub(canvasId)

  // The scene and viewport stores are app-wide singletons, so without this a
  // client-side move to another canvas would inherit the previous one's strokes
  // and pan/zoom. Start each canvas clean. (Snapshot/op hydration lands here later.)
  useEffect(() => {
    useSceneStore.getState().clear()
    useViewportStore.getState().reset()
  }, [canvasId])

  // Hub-driven exit: being kicked or a leadership transfer force-ends the canvas
  // session (link breaks) and bounces every affected member to the dashboard.
  const navCount = useConnectionStore((s) => s.navCount)
  const lastNavCount = useRef(navCount)
  useEffect(() => {
    if (navCount > lastNavCount.current) {
      lastNavCount.current = navCount
      navigate('/dashboard', { replace: true })
    }
  }, [navCount, navigate])

  return (
    <div className="relative h-screen w-screen overflow-hidden bg-background">
      <CanvasStage />
      <CursorLayer />
      <CanvasToolbar party={party} snapshot={snapshot} />
      <ZoomControls />
      <PartyPanel party={party} snapshot={snapshot} />
      <CanvasFriendsPanel party={party} />
    </div>
  )
}
