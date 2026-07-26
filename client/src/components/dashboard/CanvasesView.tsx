import { Plus, RotateCw } from 'lucide-react'

import { Button } from '@/components/ui/button'
import type { CanvasDto } from '../../api/canvas'
import CanvasCard from './CanvasCard'

interface CanvasesViewProps {
  canvases: CanvasDto[]
  isLoading: boolean
  isError: boolean
  onRetry: () => void
  onNewCanvas: () => void
  onOpenCanvas: (canvas: CanvasDto) => void
  onRenameCanvas: (canvas: CanvasDto) => void
}

export default function CanvasesView({
  canvases,
  isLoading,
  isError,
  onRetry,
  onNewCanvas,
  onOpenCanvas,
  onRenameCanvas,
}: CanvasesViewProps) {
  return (
    <section>
      <div className="mb-6 flex items-center justify-between gap-4">
        <div>
          <h1 className="font-display text-3xl leading-none sm:text-4xl">My Canvases</h1>
          <p className="mt-1 font-body text-sm text-on-background/70">
            Every board you own, ready to jump back into.
          </p>
        </div>
        <Button size="md" onClick={onNewCanvas}>
          <Plus />
          <span className="hidden sm:inline">New Canvas</span>
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-5 sm:grid-cols-3 xl:grid-cols-4">
        <button
          type="button"
          onClick={onNewCanvas}
          className="flex aspect-4/3 flex-col items-center justify-center gap-2 rounded-2xl border-[3px] border-dashed border-outline/50 bg-surface/50 font-label text-sm font-bold text-on-background/60 transition-colors hover:border-outline hover:bg-surface hover:text-on-background"
        >
          <Plus className="size-8" aria-hidden />
          New Canvas
        </button>

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

      {!isLoading && !isError && canvases.length === 0 && (
        <p className="mt-8 text-center font-body text-sm text-on-background/60">
          No canvases yet — start your first one and it'll show up here.
        </p>
      )}
    </section>
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
