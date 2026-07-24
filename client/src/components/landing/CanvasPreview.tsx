import { useRef } from 'react'
import { MousePointer2, Pencil } from 'lucide-react'
import {
  motion,
  useAnimationFrame,
  useMotionValue,
  useMotionValueEvent,
  useReducedMotion,
  useTransform,
  type MotionValue,
} from 'motion/react'


export default function CanvasPreview() {
  return (
    <section id="canvas" className="relative mb-24 w-full max-w-4xl scroll-mt-24">
      {/* Doodled squiggle floating off the top-left corner */}
      <div className="absolute -top-10 -left-16 hidden size-32 animate-drift opacity-40 lg:block">
        <svg viewBox="0 0 100 100" fill="none" className="stroke-secondary stroke-[3px]" aria-hidden>
          <path d="M10,50 Q25,25 50,50 T90,50" />
          <path d="M10,60 Q25,35 50,60 T90,60" opacity="0.5" />
        </svg>
      </div>

      <div className="relative aspect-video overflow-hidden rounded-4xl border-[3px] border-outline bg-surface p-4 sticker-shadow sm:p-6">
        <div className="relative flex h-full flex-col">
          <div className="mb-4 flex items-center justify-between sm:mb-6">
            <div className="flex gap-2">
              <span className="size-4 rounded-full border-2 border-outline bg-primary sm:size-6" />
              <span className="size-4 rounded-full border-2 border-outline bg-secondary sm:size-6" />
              <span className="size-4 rounded-full border-2 border-outline bg-primary-container sm:size-6" />
            </div>
            <div className="rounded-xl border-2 border-outline bg-[#f0f0f0] px-3 py-1 font-label text-[10px] font-bold tracking-widest uppercase sm:px-4 sm:text-xs">
              Collaborative Session #402
            </div>
          </div>

          <div className="relative flex flex-1 items-center justify-center">
            <div className="relative flex size-full items-center justify-center overflow-hidden rounded-3xl border-4 border-dashed border-outline/20 bg-[#faf9f6] sm:h-2/3 sm:w-2/3">
              <span className="absolute z-0 -rotate-6 font-display text-3xl text-outline/10">
                Sketch Area
              </span>
              <SelfDrawingSketch />
            </div>

            <CursorTag
              className="top-1/4 left-[15%] -rotate-6 bg-secondary"
              icon={<MousePointer2 className="size-3" aria-hidden />}
              label="Lina sketching..."
            />
            <CursorTag
              className="right-[12%] bottom-1/4 rotate-3 bg-primary"
              icon={<Pencil className="size-3" aria-hidden />}
              label="Tom erasing..."
            />
          </div>
        </div>
      </div>
    </section>
  )
}

interface CursorTagProps {
  className: string
  icon: React.ReactNode
  label: string
}

function CursorTag({ className, icon, label }: CursorTagProps) {
  return (
    <div
      aria-hidden
      className={`absolute z-20 flex items-center gap-1.5 rounded-full border-2 border-outline px-2 py-1 text-white sticker-shadow-sm sm:px-3 ${className}`}
    >
      {icon}
      <span className="font-label text-[10px] font-bold sm:text-xs">{label}</span>
    </div>
  )
}

/*
 * The figure, in the order a person would actually draw it. Each stroke is
 * timed independently so the sketch builds up limb by limb rather than fading
 * in all at once.
 */
const PEN_LIFT = 0.12
const HOLD = 1.6

const STROKES = [
  { d: 'M 200 42 C 226 42 244 60 244 84 C 244 108 224 124 200 124 C 176 124 156 106 156 84 C 156 60 174 42 202 43', color: '#2d2926', width: 4.5, dur: 1.1 },
  { d: 'M 185 79 q 3.5 -9 7 0', color: '#2d2926', width: 3.5, dur: 0.22 },
  { d: 'M 211 79 q 3.5 -9 7 0', color: '#2d2926', width: 3.5, dur: 0.22 },
  { d: 'M 184 99 Q 200 113 217 97', color: '#2d2926', width: 3.5, dur: 0.38 },
  { d: 'M 161 68 Q 172 32 196 47 Q 214 28 237 63', color: '#ff7070', width: 4, dur: 0.6 },
  { d: 'M 200 124 L 200 212', color: '#2d2926', width: 4.5, dur: 0.45 },
  { d: 'M 200 152 C 180 158 164 175 154 199', color: '#2d2926', width: 4.5, dur: 0.5 },
  { d: 'M 200 152 C 221 158 239 172 251 196', color: '#2d2926', width: 4.5, dur: 0.5 },
  { d: 'M 200 212 C 194 235 186 249 176 269', color: '#2d2926', width: 4.5, dur: 0.5 },
  { d: 'M 200 212 C 206 235 214 249 225 269', color: '#2d2926', width: 4.5, dur: 0.5 },
  { d: 'M 122 279 Q 200 289 279 275', color: '#00c1fd', width: 4, dur: 0.55 },
].map((stroke, i, all) => ({
  ...stroke,
  begin: all.slice(0, i).reduce((t, s) => t + s.dur + PEN_LIFT, 0),
}))

const LAST = STROKES[STROKES.length - 1]
const DRAW_END = LAST.begin + LAST.dur
const CYCLE = DRAW_END + HOLD

/**
 * useAnimationFrame is from motion, no React rerenders on animation
 */
function SelfDrawingSketch() {
  const reducedMotion = useReducedMotion()
  const clock = useMotionValue(0)
  const penX = useMotionValue(200)
  const penY = useMotionValue(42)

  useAnimationFrame((elapsed) => {
    if (reducedMotion) return
    clock.set((elapsed / 1000) % CYCLE)
  })

  // Pen rides along only while ink is being laid down, then lifts off the page.
  const penOpacity = useTransform(clock, [DRAW_END - 0.3, DRAW_END], [1, 0])

  return (
    <svg
      viewBox="0 0 400 300"
      aria-hidden
      className="pointer-events-none absolute inset-0 z-10 size-full p-6 opacity-90"
    >
      {STROKES.map((stroke) =>
        reducedMotion ? (
          <path
            key={stroke.d}
            d={stroke.d}
            fill="none"
            stroke={stroke.color}
            strokeWidth={stroke.width}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        ) : (
          <Stroke key={stroke.d} stroke={stroke} clock={clock} penX={penX} penY={penY} />
        ),
      )}

      {!reducedMotion && (
        <motion.g style={{ x: penX, y: penY, opacity: penOpacity }}>
          <path d="M 0 0 L 10 25 L 18 18 L 28 28 L 32 24 L 22 14 L 30 10 Z" fill="#2d2926" />
        </motion.g>
      )}
    </svg>
  )
}

interface StrokeProps {
  stroke: (typeof STROKES)[number]
  clock: MotionValue<number>
  penX: MotionValue<number>
  penY: MotionValue<number>
}

function Stroke({ stroke, clock, penX, penY }: StrokeProps) {
  const ref = useRef<SVGPathElement>(null)
  const length = useRef(0)

  const progress = useTransform(clock, [stroke.begin, stroke.begin + stroke.dur], [0, 1])

  const opacity = useTransform(progress, (value) => (value > 0 ? 1 : 0))

  useMotionValueEvent(progress, 'change', (value) => {
    const path = ref.current
    if (!path || value <= 0 || value >= 1) return
    if (!length.current) length.current = path.getTotalLength()
    const point = path.getPointAtLength(value * length.current)
    penX.set(point.x)
    penY.set(point.y)
  })

  return (
    <motion.path
      ref={ref}
      d={stroke.d}
      fill="none"
      stroke={stroke.color}
      strokeWidth={stroke.width}
      strokeLinecap="round"
      strokeLinejoin="round"
      style={{ pathLength: progress, opacity }}
    />
  )
}
