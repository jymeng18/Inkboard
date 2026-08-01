import { useEffect } from 'react'
import { AlertTriangle, X } from 'lucide-react'

import { Button } from '@/components/ui/button'

interface ConfirmDialogProps {
  title: string
  description: string
  confirmLabel: string
  cancelLabel?: string
  /** Tints the icon and confirm button red, for anything that can't be undone. */
  destructive?: boolean
  pending?: boolean
  onConfirm: () => void
  onClose: () => void
}

/*
 * Yes/no gate for actions that reach other people: ending a session, dragging a
 * party into a canvas. Mount it only while it should be open.
 */
export default function ConfirmDialog({
  title,
  description,
  confirmLabel,
  cancelLabel = 'Cancel',
  destructive = false,
  pending = false,
  onConfirm,
  onClose,
}: ConfirmDialogProps) {
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => event.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  return (
    /*
     * pointer-events-auto is load bearing, not decoration: the canvas toolbar
     * that opens this is pointer-events-none so drawing works through it, and a
     * modal inheriting that is invisible to the mouse. Clicks fall through to
     * the Konva stage, which is also why the drawing cursor shows over it.
     */
    <div className="pointer-events-auto fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-outline/30" onClick={onClose} aria-hidden />

      <div
        role="alertdialog"
        aria-modal="true"
        aria-label={title}
        className="relative w-full max-w-md rounded-3xl border-4 border-outline bg-surface p-6 sticker-shadow-lg"
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Close"
          className="absolute top-4 right-4 flex size-8 items-center justify-center rounded-full text-on-background/60 hover:bg-background hover:text-on-background"
        >
          <X className="size-5" aria-hidden />
        </button>

        <span
          className={`mb-4 flex size-12 items-center justify-center rounded-full border-[3px] border-outline sticker-shadow-sm ${
            destructive ? 'bg-primary text-white' : 'bg-primary-container'
          }`}
        >
          <AlertTriangle className="size-6" aria-hidden />
        </span>

        <h2 className="font-display text-3xl leading-none">{title}</h2>
        <p className="mt-2 font-body text-sm text-on-background/70">{description}</p>

        <div className="mt-6 flex items-center justify-end gap-2">
          <Button type="button" variant="ghost" size="sm" onClick={onClose} disabled={pending}>
            {cancelLabel}
          </Button>
          <Button
            type="button"
            variant={destructive ? 'primary' : 'accent'}
            size="sm"
            onClick={onConfirm}
            disabled={pending}
          >
            {pending ? 'Working…' : confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  )
}
