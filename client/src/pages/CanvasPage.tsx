import { useEffect, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import CanvasStage from '@/components/canvas/CanvasStage'
import CanvasToolbar from '@/components/canvas/CanvasToolbar'
import CursorLayer, { type RemoteCursor } from '@/components/canvas/CursorLayer'
import PartyPanel from '@/components/canvas/PartyPanel'
import ZoomControls from '@/components/canvas/ZoomControls'
import { useCanvasParty } from '@/hooks/useCanvasParty'
import { useConnectionStore } from '@/stores/connectionStore'

// TODO: This is for all the other users cursors in the party, (CanvasHub doesn't work yet)
const NO_CURSORS: RemoteCursor[] = []

export default function CanvasPage() {
  const { canvasId } = useParams()
  const navigate = useNavigate()
  const party = useCanvasParty(canvasId)

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
      <CursorLayer cursors={NO_CURSORS} />
      <CanvasToolbar />
      <ZoomControls />
      <PartyPanel party={party} />
    </div>
  )
}
