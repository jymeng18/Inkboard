import { MousePointer2 } from 'lucide-react'

export interface RemoteCursor {
  id: string
  name: string
  color: string
  xPct: number
  yPct: number
}

/*
 * Remote collaborators' cursors, positioned over the board. Isolated from the
 * chrome so the high-frequency position updates (the real feed runs ~30/sec)
 * only ever repaint this layer. When wired, each cursor should move via a ref /
 * transform rather than React state per frame.
 */
export default function CursorLayer({ cursors }: { cursors: RemoteCursor[] }) {
  return (
    <div className="pointer-events-none absolute inset-0 z-10 overflow-hidden">
      {cursors.map((cursor) => (
        <div
          key={cursor.id}
          className="absolute flex flex-col items-start"
          style={{ left: `${cursor.xPct}%`, top: `${cursor.yPct}%` }}
        >
          <MousePointer2
            className="size-5 drop-shadow-[1px_1px_0_var(--color-outline)]"
            style={{ color: cursor.color, fill: cursor.color }}
            aria-hidden
          />
          <span
            className="mt-0.5 ml-3 rounded-full border-2 border-outline px-2 py-0.5 font-label text-[10px] font-bold text-white sticker-shadow-sm"
            style={{ backgroundColor: cursor.color }}
          >
            {cursor.name}
          </span>
        </div>
      ))}
    </div>
  )
}
