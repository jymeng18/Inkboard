import { useEffect, useRef } from 'react'
import { useShallow } from 'zustand/react/shallow'
import { MousePointer2 } from 'lucide-react'

import { useAuthStore } from '@/stores/authStore'
import { useCursorStore } from '@/stores/cursorStore'
import { useViewportStore } from '@/stores/viewportStore'

// Distinct, saturated colours so a few cursors read as different people.
const CURSOR_COLORS = ['#ff7070', '#00c1fd', '#f4b400', '#9c27b0', '#00b894', '#e84393'] as const

// Fraction of the remaining gap closed each frame. Higher snaps faster with less
// smoothing; lower trails more. ~0.3 smooths the throttled feed without feeling laggy.
const LERP = 0.3

// The MousePointer2 arrow tip sits a couple px in from the icon's box corner, so
// nudge the anchor to land the tip on the actual point.
const TIP_OFFSET = 2

function cursorColor(userId: string): string {
  let hash = 0
  for (let i = 0; i < userId.length; i++) {
    hash = (hash + userId.charCodeAt(i)) % CURSOR_COLORS.length
  }
  return CURSOR_COLORS[hash]
}

/*
 * Remote collaborators' cursors, drawn as an HTML overlay (crisp labels) rather
 * than inside the Konva stage. Positions live in world coordinates; a single rAF
 * loop lerps each cursor toward its target in world space and reprojects to screen
 * every frame, so movement is smooth and stays glued to the world point through
 * pan/zoom. React only re-renders when the roster changes — never per position or
 * per pan frame — so the high-frequency updates never touch reconciliation.
 */
export default function CursorLayer() {
  const currentUserId = useAuthStore((s) => s.userId)

  // Roster only: re-render on join/leave, not on movement.
  const userIds = useCursorStore(
    useShallow((s) => Object.keys(s.cursors).filter((id) => id !== currentUserId)),
  )

  const nodes = useRef(new Map<string, HTMLDivElement>())
  const smoothed = useRef(new Map<string, { x: number; y: number }>())

  useEffect(() => {
    let frame = requestAnimationFrame(function tick() {
      const { cursors } = useCursorStore.getState()
      const { x: vx, y: vy, scale } = useViewportStore.getState()

      for (const [userId, node] of nodes.current) {
        const target = cursors[userId]
        if (!target) continue

        let cur = smoothed.current.get(userId)
        if (!cur) {
          // Spawn at the target instead of flying in from the origin.
          cur = { x: target.x, y: target.y }
          smoothed.current.set(userId, cur)
        }
        cur.x += (target.x - cur.x) * LERP
        cur.y += (target.y - cur.y) * LERP

        const screenX = cur.x * scale + vx - TIP_OFFSET
        const screenY = cur.y * scale + vy - TIP_OFFSET
        node.style.transform = `translate(${screenX}px, ${screenY}px)`
      }

      frame = requestAnimationFrame(tick)
    })
    return () => cancelAnimationFrame(frame)
  }, [])

  // Forget interpolation state for cursors that left.
  useEffect(() => {
    const alive = new Set(userIds)
    for (const id of smoothed.current.keys()) {
      if (!alive.has(id)) smoothed.current.delete(id)
    }
  }, [userIds])

  return (
    <div className="pointer-events-none absolute inset-0 z-10 overflow-hidden">
      {userIds.map((userId) => {
        const color = cursorColor(userId)
        return (
          <div
            key={userId}
            ref={(el) => {
              if (el) nodes.current.set(userId, el)
              else nodes.current.delete(userId)
            }}
            className="absolute top-0 left-0 flex flex-col items-start will-change-transform"
          >
            <MousePointer2
              className="size-5 drop-shadow-[1px_1px_0_var(--color-outline)]"
              style={{ color, fill: color }}
              aria-hidden
            />
            <span
              className="mt-0.5 ml-3 rounded-full border-2 border-outline px-2 py-0.5 font-label text-[10px] font-bold text-white sticker-shadow-sm"
              style={{ backgroundColor: color }}
            >
              {userId.slice(0, 6)}
            </span>
          </div>
        )
      })}
    </div>
  )
}
