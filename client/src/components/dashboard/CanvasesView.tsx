import { Plus } from 'lucide-react'

import { Button } from '@/components/ui/button'
import type { CanvasDto } from '../../api/canvas'
import CanvasCard from './CanvasCard'

interface CanvasesViewProps {
  canvases: CanvasDto[]
  onNewCanvas: () => void
  onOpenCanvas: (canvas: CanvasDto) => void
}

export default function CanvasesView({ canvases, onNewCanvas, onOpenCanvas }: CanvasesViewProps) {
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

        {canvases.map((canvas) => (
          <CanvasCard key={canvas.id} canvas={canvas} onOpen={onOpenCanvas} />
        ))}
      </div>

      {canvases.length === 0 && (
        <p className="mt-8 text-center font-body text-sm text-on-background/60">
          No canvases yet — start your first one and it'll show up here.
        </p>
      )}
    </section>
  )
}
