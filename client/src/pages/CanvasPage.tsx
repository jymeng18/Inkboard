import { useEffect, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import CanvasStage from '@/components/canvas/scene/CanvasStage'
import CursorLayer from '@/components/canvas/scene/CursorLayer'
import CanvasToolbar from '@/components/canvas/controls/CanvasToolbar'
import SizeSlider from '@/components/canvas/controls/SizeSlider'
import ZoomControls from '@/components/canvas/controls/ZoomControls'
import CanvasFriendsPanel from '@/components/canvas/party/CanvasFriendsPanel'
import PartyPanel from '@/components/canvas/party/PartyPanel'
import { useCanvasHub } from '@/hooks/useCanvasHub'
import { useCanvasOperations } from '@/hooks/useCanvasOperations'
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

  // Live cursor + operation sync for this canvas.
  useCanvasHub(canvasId)

  // Rebuild the scene from the persisted op-log on open (owner reopen / mid-session join).
  const { isHydrating } = useCanvasOperations(canvasId)

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
      <SizeSlider />
      <ZoomControls />
      <PartyPanel party={party} snapshot={snapshot} />
      <CanvasFriendsPanel party={party} />
      {isHydrating && <CanvasLoadingOverlay />}
    </div>
  )
}

/* Covers the brief op-log replay when a canvas opens. */
function CanvasLoadingOverlay() {
  return (
    <div className="pointer-events-auto absolute inset-0 z-100 flex flex-col items-center justify-center gap-4 bg-background/80 backdrop-blur-sm">
      <div className="size-10 animate-spin rounded-full border-4 border-outline border-t-transparent" />
      <p className="font-label text-sm font-bold text-on-background/70">Setting up your canvas…</p>
    </div>
  )
}
