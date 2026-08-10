import { useEffect } from 'react'
import { Save, X } from 'lucide-react'

import { Button } from '@/components/ui/button'

interface SaveExitDialogProps {
  title: string
  description: string
  saveLabel: string
  discardLabel: string
  cancelLabel?: string
  /** True while the save is running, so the buttons lock and show progress. */
  saving?: boolean
  onSave: () => void
  onDiscard: () => void
  onClose: () => void
}

/*
 * Three-way save gate for leaving a canvas: keep the changes, drop them, or stay.
 * Mirrors ConfirmDialog's theme and its pointer-events-auto note (the canvas
 * toolbar that opens this is pointer-events-none, so the modal must re-enable it).
 * Mount it only while it should be open.
 */
export default function SaveExitDialog({
  title,
  description,
  saveLabel,
  discardLabel,
  cancelLabel = 'Keep drawing',
  saving = false,
  onSave,
  onDiscard,
  onClose,
}: SaveExitDialogProps) {
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !saving) onClose()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onClose, saving])

  return (
    <div className="pointer-events-auto fixed inset-0 z-50 flex items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-outline/30"
        onClick={() => !saving && onClose()}
        aria-hidden
      />

      <div
        role="alertdialog"
        aria-modal="true"
        aria-label={title}
        className="relative w-full max-w-md rounded-3xl border-4 border-outline bg-surface p-6 sticker-shadow-lg"
      >
        <button
          type="button"
          onClick={onClose}
          disabled={saving}
          aria-label="Close"
          className="absolute top-4 right-4 flex size-8 items-center justify-center rounded-full text-on-background/60 hover:bg-background hover:text-on-background disabled:opacity-40"
        >
          <X className="size-5" aria-hidden />
        </button>

        <span className="mb-4 flex size-12 items-center justify-center rounded-full border-[3px] border-outline bg-primary-container sticker-shadow-sm">
          <Save className="size-6" aria-hidden />
        </span>

        <h2 className="font-display text-3xl leading-none">{title}</h2>
        <p className="mt-2 font-body text-sm text-on-background/70">{description}</p>

        {/* Full-width stack: the labels are long enough that a row wraps ragged,
            so stacking keeps every button the same width and border weight, with
            the filled button carrying the emphasis. */}
        <div className="mt-6 flex flex-col gap-2.5">
          <Button
            type="button"
            variant="accent"
            size="sm"
            className="w-full"
            onClick={onSave}
            disabled={saving}
          >
            {saving ? 'Saving…' : saveLabel}
          </Button>
          <Button
            type="button"
            variant="surface"
            size="sm"
            className="w-full"
            onClick={onDiscard}
            disabled={saving}
          >
            {discardLabel}
          </Button>
          <Button
            type="button"
            variant="surface"
            size="sm"
            className="w-full"
            onClick={onClose}
            disabled={saving}
          >
            {cancelLabel}
          </Button>
        </div>
      </div>
    </div>
  )
}
