import { MousePointer2, Pencil } from 'lucide-react'

/*
 * A mock collaborative session: the browser chrome, a dashed sketch area with
 * strokes that draw themselves on a loop, and two other people's cursors.
 */
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

function SelfDrawingSketch() {
  return (
    <svg
      viewBox="0 0 400 300"
      aria-hidden
      className="pointer-events-none absolute inset-0 z-10 size-full p-8 opacity-80"
    >
      <path
        d="M 50 150 Q 100 50 150 150 T 250 250"
        fill="none"
        stroke="#ffb347"
        strokeWidth="4"
        strokeLinecap="round"
        strokeDasharray="600"
        strokeDashoffset="600"
      >
        <animate
          attributeName="stroke-dashoffset"
          from="600"
          to="0"
          begin="0s"
          dur="3s"
          repeatCount="indefinite"
        />
      </path>

      <path
        d="M 280 80 A 40 40 0 1 1 279.9 80"
        fill="none"
        stroke="#7b61ff"
        strokeWidth="4"
        strokeLinecap="round"
        strokeDasharray="300"
        strokeDashoffset="300"
      >
        <animate
          attributeName="stroke-dashoffset"
          from="300"
          to="0"
          begin="1s"
          dur="2s"
          repeatCount="indefinite"
        />
      </path>

      <path
        d="M 320 200 l 10 20 l 20 5 l -15 15 l 5 20 l -20 -10 l -20 10 l 5 -20 l -15 -15 l 20 -5 z"
        fill="none"
        stroke="#ff6b6b"
        strokeWidth="3"
        strokeLinecap="round"
        strokeDasharray="200"
        strokeDashoffset="200"
      >
        <animate
          attributeName="stroke-dashoffset"
          from="200"
          to="0"
          begin="0.5s"
          dur="3s"
          repeatCount="indefinite"
        />
      </path>

      <g>
        <path d="M 0 0 L 10 25 L 18 18 L 28 28 L 32 24 L 22 14 L 30 10 Z" fill="#2d2926" />
        <animateMotion
          path="M 50 150 Q 100 50 150 150 T 250 250"
          begin="0s"
          dur="3s"
          repeatCount="indefinite"
        />
      </g>
    </svg>
  )
}
