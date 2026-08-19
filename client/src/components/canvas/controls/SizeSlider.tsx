import { STROKE_SIZE, useCanvasUiStore } from '@/stores/canvasUiStore'

/*
 * Vertical stroke-thickness slider on the canvas's left edge. The native range
 * input is a horizontal control turned a quarter-turn, so its low end sits at the
 * bottom and dragging up thickens the stroke. The chosen size feeds both the
 * committed stroke width and the drawing cursor.
 */
export default function SizeSlider() {
  const size = useCanvasUiStore((s) => s.size)
  const setSize = useCanvasUiStore((s) => s.setSize)

  return (
    <div className="absolute top-1/2 left-4 z-20 flex -translate-y-1/2 flex-col items-center gap-3 rounded-full border-[3px] border-outline bg-surface px-2 py-4 sticker-shadow-sm">
      <span className="font-display text-sm tabular-nums" aria-hidden>
        {size}
      </span>
      <div className="relative h-[min(14rem,45vh)] w-8">
        <input
          type="range"
          min={STROKE_SIZE.min}
          max={STROKE_SIZE.max}
          step={STROKE_SIZE.step}
          value={size}
          onChange={(e) => setSize(Number(e.target.value))}
          aria-label="Stroke thickness"
          className="size-slider absolute top-1/2 left-1/2 h-8 w-[min(14rem,45vh)] -translate-x-1/2 -translate-y-1/2 -rotate-90"
        />
      </div>
    </div>
  )
}
