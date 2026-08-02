import { ArrowRight, LogOut, Plus, RotateCw } from 'lucide-react'

import { Button } from '@/components/ui/button'
import type { CanvasDto } from '@/api/canvas'
import CanvasCard from './CanvasCard'

/*
 * The party's live-session state for this view. When `active`, the party is on a
 * canvas; `locked` (members only) blocks creating/opening canvases here, leaving
 * "return to the canvas" or "leave the party" as the only ways forward.
 */
interface CanvasSession {
  active: boolean
  isLeader: boolean
  locked: boolean
  onReturn: () => void
  onLeave: () => void
}

interface CanvasesViewProps {
  canvases: CanvasDto[]
  isLoading: boolean
  isError: boolean
  onRetry: () => void
  onNewCanvas: () => void
  onOpenCanvas: (canvas: CanvasDto) => void
  onRenameCanvas: (canvas: CanvasDto) => void
  session: CanvasSession
}

export default function CanvasesView({
  canvases,
  isLoading,
  isError,
  onRetry,
  onNewCanvas,
  onOpenCanvas,
  onRenameCanvas,
  session,
}: CanvasesViewProps) {
  return (
    <section>
      {session.active && <PartySessionBanner session={session} />}

      <div className="mb-6 flex items-center justify-between gap-4">
        <div>
          <h1 className="font-display text-3xl leading-none sm:text-4xl">My Canvases</h1>
          <p className="mt-1 font-body text-sm text-on-background/70">
            Every board you own, ready to jump back into.
          </p>
        </div>
        <Button size="md" onClick={onNewCanvas} disabled={session.locked}>
          <Plus />
          <span className="hidden sm:inline">New Canvas</span>
        </Button>
      </div>

      <div
        className={`grid grid-cols-2 gap-5 sm:grid-cols-3 xl:grid-cols-4 ${
          session.locked ? 'pointer-events-none opacity-50 select-none' : ''
        }`}
      >
        {!session.locked && (
          <button
            type="button"
            onClick={onNewCanvas}
            className="flex aspect-4/3 flex-col items-center justify-center gap-2 rounded-2xl border-[3px] border-dashed border-outline/50 bg-surface/50 font-label text-sm font-bold text-on-background/60 transition-colors hover:border-outline hover:bg-surface hover:text-on-background"
          >
            <Plus className="size-8" aria-hidden />
            New Canvas
          </button>
        )}

        {isLoading
          ? Array.from({ length: 3 }).map((_, i) => <CanvasCardSkeleton key={i} />)
          : canvases.map((canvas) => (
              <CanvasCard
                key={canvas.id}
                canvas={canvas}
                onOpen={onOpenCanvas}
                onRename={onRenameCanvas}
              />
            ))}
      </div>

      {isError && (
        <div className="mt-8 flex flex-col items-center gap-3 text-center">
          <p className="font-body text-sm text-on-background/70">
            Couldn't load your canvases.
          </p>
          <Button variant="surface" size="sm" onClick={onRetry}>
            <RotateCw />
            Try again
          </Button>
        </div>
      )}

      {!isLoading && !isError && !session.locked && canvases.length === 0 && (
        <p className="mt-8 text-center font-body text-sm text-on-background/60">
          No canvases yet — start your first one and it'll show up here.
        </p>
      )}
    </section>
  )
}

/*
 * Loud, on-brand reminder that the party is mid-session. The pulsing dot and
 * container fill are there so a member who back-buttoned to the dashboard can't
 * miss which canvas they're still part of.
 */
function PartySessionBanner({ session }: { session: CanvasSession }) {
  return (
    <div className="mb-6 flex flex-col gap-4 rounded-2xl border-[3px] border-outline bg-primary-container p-5 sticker-shadow sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-start gap-3">
        <span className="flex size-11 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-surface">
          <span className="relative flex size-3">
            <span className="absolute inline-flex size-full animate-ping rounded-full bg-primary/70" />
            <span className="relative inline-flex size-3 rounded-full bg-primary" />
          </span>
        </span>
        <div>
          <p className="font-display text-xl leading-none">You're in a live party session</p>
          <p className="mt-1 font-body text-sm text-on-background/70">
            {session.locked
              ? "Jump back into your party's canvas, or leave the party to start your own."
              : 'Your party is drawing here — jump back in whenever you like.'}
          </p>
        </div>
      </div>

      <div className="flex shrink-0 items-center gap-2">
        <Button size="md" onClick={session.onReturn}>
          <ArrowRight />
          Return to canvas
        </Button>
        {session.locked && (
          <Button variant="surface" size="md" onClick={session.onLeave}>
            <LogOut />
            Leave party
          </Button>
        )}
      </div>
    </div>
  )
}

function CanvasCardSkeleton() {
  return (
    <div className="flex animate-pulse flex-col overflow-hidden rounded-2xl border-[3px] border-outline bg-surface">
      <div className="aspect-4/3 border-b-[3px] border-outline bg-surface-dim/40" />
      <div className="space-y-2 p-3">
        <div className="h-4 w-2/3 rounded bg-surface-dim/50" />
        <div className="h-3 w-1/3 rounded bg-surface-dim/40" />
      </div>
    </div>
  )
}
