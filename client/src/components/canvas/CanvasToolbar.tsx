import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import {
  Brush,
  Eraser,
  Hand,
  History,
  LayoutDashboard,
  Pencil,
  PowerOff,
  Redo2,
  Share2,
  Shapes,
  Undo2,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

import { extractErrorMessage } from '@/api/party'
import { Button } from '@/components/ui/button'
import ConfirmDialog from '@/components/ui/ConfirmDialog'
import Dock from '@/components/dashboard/Dock'
import type { CanvasSnapshotApi } from '@/hooks/useCanvasSnapshot'
import type { useCanvasParty } from '@/hooks/useCanvasParty'
import { useAuthStore } from '@/stores/authStore'
import { useCanvasUiStore, type Tool } from '@/stores/canvasUiStore'
import { useSceneStore } from '@/stores/sceneStore'
import ColorPicker from './ColorPicker'

type Party = ReturnType<typeof useCanvasParty>

const TOOLS: { tool: Tool; icon: LucideIcon; label: string }[] = [
  { tool: 'pencil', icon: Pencil, label: 'Pencil (P)' },
  { tool: 'brush', icon: Brush, label: 'Brush (B)' },
  { tool: 'eraser', icon: Eraser, label: 'Eraser (E)' },
  { tool: 'shapes', icon: Shapes, label: 'Shapes (U)' },
  { tool: 'hand', icon: Hand, label: 'Hand (H)' },
]

/*
 * Kept modest on purpose: the dock shifts its neighbours as it swells, and
 * these are targets people click mid-drawing, so a big pop makes them slippery.
 */
const TOOL_SIZE = 36
const TOOL_MAGNIFICATION = 50

const soon = () => toast.info('This action is coming soon.')

export default function CanvasToolbar({
  party,
  snapshot,
}: {
  party: Party
  snapshot?: CanvasSnapshotApi
}) {
  const tool = useCanvasUiStore((s) => s.tool)
  const setTool = useCanvasUiStore((s) => s.setTool)
  const undo = useSceneStore((s) => s.undo)
  const redo = useSceneStore((s) => s.redo)
  const canUndo = useSceneStore((s) => s.items.length > 0)
  const canRedo = useSceneStore((s) => s.redoStack.length > 0)
  const userName = useAuthStore((s) => s.userName)
  const initial = userName.trim().charAt(0).toUpperCase() || '?'

  const navigate = useNavigate()

  const [confirmEnd, setConfirmEnd] = useState(false)
  const [ending, setEnding] = useState(false)

  /*
   * Only the leader can tear a session down. Everyone else leaves through the
   * party panel, and a solo drawer just walks out via the dashboard link.
   */
  const canEndSession = party.partyId !== null && party.isLeader
  const otherMembers = party.members.filter((m) => m.userId !== party.currentUserId).length

  const endDescription =
    otherMembers > 0
      ? `This closes the canvas for everyone and disbands the party. ${otherMembers} other ${
          otherMembers === 1 ? 'person' : 'people'
        } will be sent back to their dashboard.`
      : 'This closes the canvas and disbands your party.'

  // Work is persisted continuously now, so leaving just refreshes the dashboard
  // thumbnail (owner-only, no-op otherwise) and navigates.
  async function handleEndSession() {
    setEnding(true)
    try {
      void snapshot?.save()
      await party.endSession()
      navigate('/dashboard', { replace: true })
    } catch (err) {
      toast.error(extractErrorMessage(err))
      setEnding(false)
      setConfirmEnd(false)
    }
  }

  return (
    <>
      <div className="pointer-events-none absolute inset-x-0 top-0 z-20 flex flex-wrap items-start justify-between gap-2 p-2 sm:gap-3 sm:p-4 xl:flex-nowrap">
        <div className="pointer-events-auto order-1 flex items-center gap-2 xl:order-none">
          <Link
            to="/dashboard"
            onClick={() => void snapshot?.save()}
            className="flex items-center gap-2 rounded-full border-[3px] border-outline bg-surface px-3 py-2 sticker-shadow-sm transition-transform hover:-translate-y-0.5 sm:px-4"
            title="Back to dashboard"
          >
            <LayoutDashboard className="size-6 sm:hidden" aria-hidden />
            <span className="hidden font-display text-xl sm:inline">Dashboard</span>
          </Link>

          {/* Mirrors the dashboard link's own classes rather than using Button,
              so the pair sits at exactly the same height and weight. */}
          {canEndSession && (
            <button
              type="button"
              onClick={() => setConfirmEnd(true)}
              title="End session"
              className="flex items-center gap-2 rounded-full border-[3px] border-outline bg-surface px-4 py-2 sticker-shadow-sm transition-transform hover:-translate-y-0.5 hover:text-primary"
            >
              <PowerOff className="size-6" aria-hidden />
              <span className="hidden font-display text-xl sm:inline">End session</span>
            </button>
          )}
        </div>

        <div className="pointer-events-none order-3 flex w-full justify-center xl:order-none xl:w-auto">
          <div className="pointer-events-auto flex items-center gap-2 rounded-full border-[3px] border-outline bg-surface px-3 py-1.5 sticker-shadow">
            <Dock
              items={TOOLS.map(({ tool: value, icon: Icon, label }) => ({
                label,
                active: tool === value,
                onClick: () => setTool(value),
                icon: <Icon className="size-5" aria-hidden />,
              }))}
              baseItemSize={TOOL_SIZE}
              magnification={TOOL_MAGNIFICATION}
              distance={130}
            />

            <span className="mx-1 h-7 w-0.5 rounded bg-outline/15" aria-hidden />

            <ColorPicker />
          </div>
        </div>

        <div className="pointer-events-auto order-2 flex items-center gap-2 xl:order-none">
          <div className="flex items-center gap-1 rounded-full border-[3px] border-outline bg-surface px-2 py-1.5 sticker-shadow-sm">
            <IconButton icon={Undo2} label="Undo (Ctrl+Z)" onClick={undo} disabled={!canUndo} />
            <IconButton
              icon={Redo2}
              label="Redo (Ctrl+Shift+Z)"
              onClick={redo}
              disabled={!canRedo}
            />
            <IconButton icon={History} label="Version history" onClick={soon} />
          </div>

          <Button size="sm" onClick={soon}>
            <Share2 />
            <span className="hidden sm:inline">Export</span>
          </Button>

          <span
            className="flex size-10 items-center justify-center rounded-full border-[3px] border-outline bg-secondary-container font-display text-lg sticker-shadow-sm"
            title={userName || 'You'}
          >
            {initial}
          </span>
        </div>
      </div>

      {/*
       * Outside the toolbar wrapper on purpose: that wrapper is
       * pointer-events-none (so the canvas stays drawable through the gaps) and
       * its z-20 opens a stacking context, either of which would leave a modal
       * nested inside it unclickable or painted under the side panels.
       */}
      {confirmEnd && (
        <ConfirmDialog
          title="End this session?"
          description={endDescription}
          confirmLabel="End session"
          cancelLabel="Keep drawing"
          destructive
          pending={ending}
          onConfirm={handleEndSession}
          onClose={() => setConfirmEnd(false)}
        />
      )}
    </>
  )
}

function IconButton({
  icon: Icon,
  label,
  onClick,
  disabled,
}: {
  icon: LucideIcon
  label: string
  onClick: () => void
  disabled?: boolean
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      title={label}
      aria-label={label}
      className="flex size-8 items-center justify-center rounded-full text-on-background/70 transition-colors hover:bg-background hover:text-on-background disabled:pointer-events-none disabled:opacity-30"
    >
      <Icon className="size-4" aria-hidden />
    </button>
  )
}
