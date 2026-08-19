import { useEffect } from 'react'
import { Monitor, X } from 'lucide-react'

import { Button } from '@/components/ui/button'

interface MobileExperienceNoticeProps {
  onDismiss: () => void
}

/*
 * First-run heads-up for people who signed up on a phone.
 *
 * Deliberately not a sonner toast: those dock to the top-right corner and this
 * one has to sit in the middle of the screen and stay there until it is
 * acknowledged, so it borrows CanvasNameDialog's centred-overlay shape instead.
 */
export default function MobileExperienceNotice({ onDismiss }: MobileExperienceNoticeProps) {
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => event.key === 'Escape' && onDismiss()
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [onDismiss])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/*
       * Unlike the canvas dialogs this scrim is inert. On a phone the card fills
       * most of the screen, and a stray tap just outside it should not count as
       * having read the thing.
       */}
      <div className="absolute inset-0 bg-outline/30" aria-hidden />

      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="mobile-notice-title"
        aria-describedby="mobile-notice-body"
        className="relative w-full max-w-md rounded-3xl border-4 border-outline bg-surface p-6 sticker-shadow-lg"
      >
        <button
          type="button"
          onClick={onDismiss}
          aria-label="Dismiss"
          className="absolute top-4 right-4 flex size-8 items-center justify-center rounded-full text-on-background/60 hover:bg-background hover:text-on-background"
        >
          <X className="size-5" aria-hidden />
        </button>

        <div className="flex size-12 items-center justify-center rounded-2xl border-[3px] border-outline bg-primary-container sticker-shadow-sm">
          <Monitor className="size-6" aria-hidden />
        </div>

        <h2 id="mobile-notice-title" className="mt-4 font-display text-3xl leading-none">
          Better on a big screen
        </h2>
        <p id="mobile-notice-body" className="mt-2 font-body text-sm text-on-background/70">
          Inkboard is built for drawing with a mouse or stylus, so it feels a lot better on a
          computer. Mobile support is limited for now — some tools and party controls are cramped
          or missing on a phone.
        </p>

        <div className="mt-5 flex justify-end">
          <Button type="button" size="sm" onClick={onDismiss} autoFocus>
            Got it
          </Button>
        </div>
      </div>
    </div>
  )
}
