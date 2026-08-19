import { useEffect, useRef, useState } from 'react'
import { X } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { CANVAS_NAME_MAX_LENGTH, DEFAULT_CANVAS_NAME } from '@/api/canvas'

interface CanvasNameDialogProps {
  title: string
  description: string
  submitLabel: string
  initialName?: string
  pending?: boolean
  onSubmit: (name: string) => void
  onClose: () => void
}

/*
 * Names a canvas, both on the way in (create) and after the fact (rename).
 * Mount it only while it should be open. State seeds from `initialName` on
 * mount, so a fresh mount per canvas is what keeps the field from going stale.
 */
export default function CanvasNameDialog({
  title,
  description,
  submitLabel,
  initialName = '',
  pending = false,
  onSubmit,
  onClose,
}: CanvasNameDialogProps) {
  const [name, setName] = useState(initialName)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) =>
      event.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  // Preselected, so typing over a suggested name takes no extra keystrokes.
  useEffect(() => {
    inputRef.current?.select()
  }, [])

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (pending) return
    onSubmit(name)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-outline/30"
        onClick={onClose}
        aria-hidden
      />

      <form
        onSubmit={handleSubmit}
        role="dialog"
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

        <h2 className="font-display text-3xl leading-none">{title}</h2>
        <p className="mt-2 font-body text-sm text-on-background/70">
          {description}
        </p>

        <label
          htmlFor="canvas-name"
          className="mt-5 mb-1.5 ml-4 block font-label text-xs font-bold"
        >
          Canvas name
        </label>
        <input
          id="canvas-name"
          ref={inputRef}
          value={name}
          onChange={(event) => setName(event.target.value)}
          maxLength={CANVAS_NAME_MAX_LENGTH}
          placeholder={DEFAULT_CANVAS_NAME}
          autoFocus
          className="w-full rounded-full border-[3px] border-outline bg-background px-4 py-2.5 font-body text-sm outline-none placeholder:text-on-background/35 focus:border-primary"
        />

        <div className="mt-5 flex items-center justify-end gap-2">
          <Button type="button" variant="ghost" size="sm" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" size="sm" disabled={pending}>
            {pending ? 'Saving…' : submitLabel}
          </Button>
        </div>
      </form>
    </div>
  )
}
