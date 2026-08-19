import { useEffect, useMemo } from 'react'
import { ImageIcon, Pencil } from 'lucide-react'

import { canvasDisplayName, type CanvasDto } from '@/api/canvas'
import { useSnapshotPreview } from '@/hooks/useSnapshotPreview'

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
  onRename: (canvas: CanvasDto) => void
}

export default function CanvasCard({
  canvas,
  onOpen,
  onRename,
}: CanvasCardProps) {
  const name = canvasDisplayName(canvas)

  /*
   * snapshotURL is the private blob URI and is never browser-usable, so we treat
   * it only as a flag that a snapshot exists and fetch the bytes through the
   * proxy. The object URL is revoked when the blob changes or the card unmounts.
   */
  const hasSnapshot = !!canvas.snapshotURL
  const { data: previewBlob } = useSnapshotPreview(
    canvas.id,
    canvas.lastModifiedAt,
    hasSnapshot,
  )
  const previewUrl = useMemo(
    () => (previewBlob ? URL.createObjectURL(previewBlob) : null),
    [previewBlob],
  )

  useEffect(() => {
    if (!previewUrl) return
    return () => URL.revokeObjectURL(previewUrl)
  }, [previewUrl])

  return (
    /*
     * The rename control is a sibling of the open button rather than a child:
     * a button inside a button is invalid, and the browser would swallow one of
     * the two clicks.
     */
    <div className="group relative overflow-hidden rounded-2xl border-[3px] border-outline bg-surface transition-transform sticker-shadow hover:-translate-x-0.5 hover:-translate-y-0.5">
      <button
        type="button"
        onClick={() => onOpen(canvas)}
        aria-label={`Open ${name}`}
        className="flex w-full flex-col text-left"
      >
        <div className="relative aspect-4/3 w-full overflow-hidden border-b-[3px] border-outline bg-background canvas-bg">
          {previewUrl ? (
            <img
              src={previewUrl}
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

        <div className="w-full p-3">
          <p className="truncate font-display text-lg leading-tight">{name}</p>
          <p className="font-body text-xs text-on-background/60">
            Edited {timeAgo(canvas.lastModifiedAt)}
          </p>
        </div>
      </button>

      <button
        type="button"
        onClick={() => onRename(canvas)}
        aria-label={`Rename ${name}`}
        title="Rename"
        className="absolute top-2 right-2 flex size-9 items-center justify-center rounded-full border-[3px] border-outline bg-surface text-on-background/70 transition-colors outline-none hover:bg-primary-container hover:text-on-background focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary sticker-shadow-sm"
      >
        <Pencil className="size-4" aria-hidden />
      </button>
    </div>
  )
}
