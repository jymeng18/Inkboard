import { useEffect } from 'react'
import { Save, X } from 'lucide-react'

import { Button } from '@/components/ui/button'

interface SaveExitDialogProps {
  title: string
  description: string
  /** True while the save is running, so the buttons lock and show progress. */
  saving?: boolean
  onSave: () => void
  onDiscard: () => void
  onClose: () => void
}

/*
 * Three-way save gate for leaving a canvas, laid out the way these prompts
 * conventionally are: the safe pair (Save + Cancel) grouped together, and the
 * destructive Don't Save set apart across a gap so it's hard to hit by reflex.
 * Mirrors ConfirmDialog's theme and its pointer-events-auto note (the canvas
 * toolbar that opens this is pointer-events-none, so the modal must re-enable it).
 */
export default function SaveExitDialog({
  title,
  description,
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

        {/* Save + Cancel grouped, Don't Save split off across the gap. */}
        <div className="mt-7 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2.5">
            <Button
              type="button"
              variant="surface"
              size="sm"
              className="bg-secondary text-white"
              onClick={onSave}
              disabled={saving}
            >
              {saving ? 'Saving…' : 'Save'}
            </Button>
            <Button
              type="button"
              variant="surface"
              size="sm"
              onClick={onClose}
              disabled={saving}
            >
              Cancel
            </Button>
          </div>
          <Button
            type="button"
            variant="surface"
            size="sm"
            className="border-primary text-primary"
            onClick={onDiscard}
            disabled={saving}
          >
            Don't Save
          </Button>
        </div>
      </div>
    </div>
  )
}
