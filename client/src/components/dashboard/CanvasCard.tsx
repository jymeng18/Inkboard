import { ImageIcon } from 'lucide-react'

import type { CanvasDto } from '../../api/canvas'

/** Formats an ISO timestamp as a short relative string like "3d ago". */
function timeAgo(iso: string): string {
  const seconds = Math.round((Date.now() - new Date(iso).getTime()) / 1000)
  const units: [Intl.RelativeTimeFormatUnit, number][] = [
    ['year', 31536000],
    ['month', 2592000],
    ['day', 86400],
    ['hour', 3600],
    ['minute', 60],
  ]
  const formatter = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' })
  for (const [unit, secondsInUnit] of units) {
    if (Math.abs(seconds) >= secondsInUnit) {
      return formatter.format(-Math.round(seconds / secondsInUnit), unit)
    }
  }
  return 'just now'
}

interface CanvasCardProps {
  canvas: CanvasDto
  onOpen: (canvas: CanvasDto) => void
}

export default function CanvasCard({ canvas, onOpen }: CanvasCardProps) {
  return (
    <button
      type="button"
      onClick={() => onOpen(canvas)}
      className="group flex flex-col overflow-hidden rounded-2xl border-[3px] border-outline bg-surface text-left transition-transform sticker-shadow hover:-translate-x-0.5 hover:-translate-y-0.5"
    >
      <div className="relative aspect-4/3 overflow-hidden border-b-[3px] border-outline bg-background canvas-bg">
        {canvas.snapshotURL ? (
          <img
            src={canvas.snapshotURL}
            alt=""
            className="size-full object-cover"
            loading="lazy"
          />
        ) : (
          <div className="flex size-full items-center justify-center text-on-background/25">
            <ImageIcon className="size-10" aria-hidden />
          </div>
        )}
      </div>

      <div className="p-3">
        <p className="truncate font-display text-lg leading-tight">{canvas.name}</p>
        <p className="font-body text-xs text-on-background/60">
          Edited {timeAgo(canvas.lastModifiedAt)}
        </p>
      </div>
    </button>
  )
}
