import { useEffect, useState } from 'react'
import { Users } from 'lucide-react'

const RING_RADIUS = 52
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS

/*
 * The spotlight that holds a member in place for a few seconds before the party
 * pulls them into the leader's new canvas. The screen goes dark, one circle of
 * light rests on the card, and nothing outside it can be touched: the old
 * session is over, so there's nothing left to draw on anyway.
 */
export default function FollowCountdownOverlay({
  duration = 5000,
  onComplete,
}: {
  duration?: number
  onComplete: () => void
}) {
  const [remaining, setRemaining] = useState(duration)

  // A single rAF loop drives both the number and the ring, so they drain
  // together off one clock. It pauses with the tab, which is fine here.
  useEffect(() => {
    const start = performance.now()
    let frame = 0

    const tick = (now: number) => {
      const left = Math.max(0, duration - (now - start))
      setRemaining(left)
      if (left <= 0) {
        onComplete()
        return
      }
      frame = requestAnimationFrame(tick)
    }

    frame = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(frame)
  }, [duration, onComplete])

  // Lock the page and swallow every key: watching the countdown is the only
  // thing left to do until it lands.
  useEffect(() => {
    const prevOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    const swallow = (event: KeyboardEvent) => {
      event.preventDefault()
      event.stopPropagation()
    }
    window.addEventListener('keydown', swallow, true)

    return () => {
      document.body.style.overflow = prevOverflow
      window.removeEventListener('keydown', swallow, true)
    }
  }, [])

  const seconds = Math.ceil(remaining / 1000)
  const progress = remaining / duration

  return (
    <div
      role="alertdialog"
      aria-modal="true"
      aria-label="Your party is opening a new canvas"
      className="pointer-events-auto fixed inset-0 z-200 flex items-center justify-center p-4"
    >
      <div
        aria-hidden
        className="absolute inset-0 animate-spotlight-in"
        style={{
          background:
            'radial-gradient(circle at center, rgba(45,41,38,0.30) 0%, rgba(45,41,38,0.45) 20%, rgba(45,41,38,0.82) 52%, rgba(45,41,38,0.94) 78%)',
        }}
      />

      <div className="animate-pop-in relative flex w-full max-w-sm flex-col items-center rounded-3xl border-4 border-outline bg-surface p-8 text-center sticker-shadow-lg">
        <span className="mb-5 flex size-12 items-center justify-center rounded-full border-[3px] border-outline bg-primary-container sticker-shadow-sm">
          <Users className="size-6" aria-hidden />
        </span>

        <h2 className="font-display text-3xl leading-none">Party's on the move</h2>
        <p className="mt-2 font-body text-sm text-on-background/70">
          Your leader opened a new canvas. Taking you there…
        </p>

        <div className="relative mt-7 flex size-32 items-center justify-center">
          <svg className="absolute inset-0 -rotate-90" viewBox="0 0 120 120" aria-hidden>
            <circle
              cx="60"
              cy="60"
              r={RING_RADIUS}
              fill="none"
              stroke="var(--color-outline-variant)"
              strokeWidth="8"
            />
            <circle
              cx="60"
              cy="60"
              r={RING_RADIUS}
              fill="none"
              stroke="var(--color-primary)"
              strokeWidth="8"
              strokeLinecap="round"
              strokeDasharray={RING_CIRCUMFERENCE}
              strokeDashoffset={RING_CIRCUMFERENCE * (1 - progress)}
            />
          </svg>
          <span className="font-display text-5xl leading-none tabular-nums" aria-live="polite">
            {seconds}
          </span>
        </div>

        <p className="mt-6 font-label text-[11px] font-bold tracking-wide text-on-background/50 uppercase">
          Sit tight — drawing's paused
        </p>
      </div>
    </div>
  )
}
