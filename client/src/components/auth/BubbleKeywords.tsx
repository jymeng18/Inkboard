import { useEffect, useRef } from 'react'
import { gsap } from 'gsap'

import { usePrefersReducedMotion } from '@/hooks/useMediaQuery'

/*
 * Adapted from the reactbits "Bubble Menu" (design/Bubble.tsx). The original pops
 * its pills in on a hamburger click; here the reveal auto-plays the moment the
 * panel mounts, so landing on the auth page kicks off the effect. The nav / logo /
 * toggle chrome is dropped — these pills are a decorative showcase of what the app
 * is about, not navigation.
 *
 * Layout is the source's honeycomb: three chunky capsules across the top, two
 * nestled underneath, each tilted a few degrees, with a bit more air between them
 * than the tightly-packed reference. Row layout + hover live on the <li>; GSAP
 * owns the inner pill's scale + rotation so the two transforms never fight.
 */
type Keyword = {
  label: string
  bg: string
  color: string
  rotation: number
  size: string
}

// Drawn from the app's own theme palette so the bubbles read as Inkboard. Split
// into the top row of three and the bottom row of two.
const TOP_ROW: Keyword[] = [
  { label: 'Draw', bg: '#ff7070', color: '#ffffff', rotation: -6, size: 'text-xl xl:text-4xl px-4 py-3 xl:px-6 xl:py-4' },
  { label: 'Live', bg: '#ffd23f', color: '#2d2926', rotation: 5, size: 'text-xl xl:text-4xl px-4 py-3 xl:px-6 xl:py-4' },
  { label: 'Chaos', bg: '#a06cd5', color: '#ffffff', rotation: -3, size: 'text-xl xl:text-4xl px-4 py-3 xl:px-6 xl:py-4' },
]

const BOTTOM_ROW: Keyword[] = [
  { label: 'Doodle', bg: '#7bc86c', color: '#2d2926', rotation: 7, size: 'text-xl xl:text-4xl px-4 py-3 xl:px-6 xl:py-4' },
  { label: 'Parties', bg: '#00c1fd', color: '#2d2926', rotation: -6, size: 'text-xl xl:text-4xl px-4 py-3 xl:px-6 xl:py-4' },
]

const KEYWORDS = [...TOP_ROW, ...BOTTOM_ROW]

export default function BubbleKeywords() {
  const bubblesRef = useRef<HTMLDivElement[]>([])
  const labelsRef = useRef<HTMLSpanElement[]>([])
  const reduced = usePrefersReducedMotion()

  useEffect(() => {
    const bubbles = bubblesRef.current.filter(Boolean)
    const labels = labelsRef.current.filter(Boolean)
    if (!bubbles.length) return

    // Reduced motion: land on the finished state, no pop.
    if (reduced) {
      bubbles.forEach((bubble, i) =>
        gsap.set(bubble, { scale: 1, rotation: KEYWORDS[i]?.rotation ?? 0 }),
      )
      gsap.set(labels, { y: 0, autoAlpha: 1 })
      return
    }

    gsap.killTweensOf([...bubbles, ...labels])
    bubbles.forEach((bubble, i) =>
      gsap.set(bubble, {
        scale: 0,
        rotation: KEYWORDS[i]?.rotation ?? 0,
        transformOrigin: '50% 50%',
      }),
    )
    gsap.set(labels, { y: 20, autoAlpha: 0 })

    bubbles.forEach((bubble, i) => {
      const delay = i * 0.11 + gsap.utils.random(-0.06, 0.06)
      const tl = gsap.timeline({ delay })
      tl.to(bubble, { scale: 1, duration: 0.5, ease: 'back.out(1.6)' })
      if (labels[i]) {
        tl.to(
          labels[i],
          { y: 0, autoAlpha: 1, duration: 0.5, ease: 'power3.out' },
          '-=0.45',
        )
      }
    })

    return () => gsap.killTweensOf([...bubbles, ...labels])
  }, [reduced])

  const renderPill = (keyword: Keyword, idx: number) => (
    <li
      key={keyword.label}
      className="relative transition-transform duration-200 hover:z-10 hover:scale-105"
    >
      <div
        ref={(el) => {
          if (el) bubblesRef.current[idx] = el
        }}
        className={`flex select-none items-center justify-center rounded-full border-[3px] border-outline whitespace-nowrap sticker-shadow will-change-transform ${keyword.size}`}
        style={{ background: keyword.bg, color: keyword.color }}
      >
        <span
          ref={(el) => {
            if (el) labelsRef.current[idx] = el
          }}
          className="font-display leading-none will-change-[transform,opacity]"
        >
          {keyword.label}
        </span>
      </div>
    </li>
  )

  return (
    <div
      aria-label="What makes Inkboard fun"
      className="mx-auto flex max-w-md flex-col items-center gap-4"
    >
      <ul className="m-0 flex list-none flex-nowrap justify-center gap-4 p-0">
        {TOP_ROW.map((keyword, i) => renderPill(keyword, i))}
      </ul>
      <ul className="m-0 flex list-none flex-nowrap justify-center gap-4 p-0">
        {BOTTOM_ROW.map((keyword, i) => renderPill(keyword, i + TOP_ROW.length))}
      </ul>
    </div>
  )
}
