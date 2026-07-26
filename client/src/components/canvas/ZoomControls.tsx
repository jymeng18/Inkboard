import { Maximize, Minus, Plus } from 'lucide-react'

import { useViewportStore } from '@/stores/viewportStore'

const screenCenter = () => ({ x: window.innerWidth / 2, y: window.innerHeight / 2 })

export default function ZoomControls() {
  const scale = useViewportStore((s) => s.scale)
  const zoomTo = useViewportStore((s) => s.zoomTo)
  const reset = useViewportStore((s) => s.reset)

  return (
    <div className="absolute bottom-4 left-4 z-20 flex items-center gap-1 rounded-full border-[3px] border-outline bg-surface px-2 py-1.5 sticker-shadow-sm">
      <ZoomButton label="Zoom out" onClick={() => zoomTo(scale / 1.2, screenCenter())}>
        <Minus className="size-4" aria-hidden />
      </ZoomButton>

      <button
        type="button"
        onClick={reset}
        title="Reset zoom"
        className="w-14 rounded-full py-1 text-center font-label text-sm font-bold hover:bg-background"
      >
        {Math.round(scale * 100)}%
      </button>

      <ZoomButton label="Zoom in" onClick={() => zoomTo(scale * 1.2, screenCenter())}>
        <Plus className="size-4" aria-hidden />
      </ZoomButton>

      <span className="mx-0.5 h-5 w-0.5 rounded bg-outline/15" aria-hidden />

      <ZoomButton label="Reset view" onClick={reset}>
        <Maximize className="size-4" aria-hidden />
      </ZoomButton>
    </div>
  )
}

function ZoomButton({
  label,
  onClick,
  children,
}: {
  label: string
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={label}
      aria-label={label}
      className="flex size-8 items-center justify-center rounded-full text-on-background/70 transition-colors hover:bg-background hover:text-on-background"
    >
      {children}
    </button>
  )
}
