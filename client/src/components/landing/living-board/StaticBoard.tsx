import { MousePointer2, Pencil } from "lucide-react";

/*
 * The no-WebGL board. Shown on touch devices, when the user prefers reduced
 * motion, and as the Suspense fallback while the 3D chunk loads, so the hero
 * always has a real Inkboard sitting there whether or not Three ever boots.
 */
export default function StaticBoard() {
  return (
    <div className="relative size-full">
      <div className="absolute inset-0 rotate-1">
        <div className="canvas-bg relative size-full overflow-hidden rounded-3xl border-[3px] border-outline bg-surface sticker-shadow-lg">
          <div className="flex items-center justify-between border-b-2 border-dashed border-outline/15 px-4 py-3">
            <div className="flex gap-1.5">
              <span className="size-3.5 rounded-full border-2 border-outline bg-primary" />
              <span className="size-3.5 rounded-full border-2 border-outline bg-secondary" />
              <span className="size-3.5 rounded-full border-2 border-outline bg-primary-container" />
            </div>
            <span className="rounded-lg border-2 border-outline bg-background px-2.5 py-0.5 font-label text-[10px] font-bold tracking-widest uppercase">
              Room #402
            </span>
          </div>

          <svg
            viewBox="0 0 400 220"
            aria-hidden
            className="absolute inset-0 top-6 size-full"
            fill="none"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            {/* star */}
            <path
              d="M92 58 L74 104 L120 74 L64 74 L110 104 Z"
              stroke="#ffd23f"
              strokeWidth={6}
            />
            {/* heart */}
            <path
              d="M214 74 C206 58 182 58 182 78 C182 96 214 112 214 112 C214 112 246 96 246 78 C246 58 222 58 214 74 Z"
              stroke="#ff7070"
              strokeWidth={6}
            />
            {/* squiggle */}
            <path
              d="M292 92 Q312 60 330 92 T368 92"
              stroke="#00c1fd"
              strokeWidth={6}
            />
            {/* baseline scribble */}
            <path
              d="M70 168 Q150 150 210 168 T350 162"
              stroke="#2d2926"
              strokeWidth={5}
              opacity={0.7}
            />
          </svg>

          <CursorTag
            className="top-[28%] left-[16%] -rotate-6 bg-secondary"
            label="Lina"
          />
          <CursorTag
            className="right-[14%] bottom-[26%] rotate-3 bg-primary"
            label="Tom"
          />
        </div>
      </div>
    </div>
  );
}

function CursorTag({ className, label }: { className: string; label: string }) {
  return (
    <div
      aria-hidden
      className={`absolute z-20 flex items-center gap-1 rounded-full border-2 border-outline px-2 py-0.5 text-white sticker-shadow-sm ${className}`}
    >
      {label === "Tom" ? (
        <Pencil className="size-3" aria-hidden />
      ) : (
        <MousePointer2 className="size-3" aria-hidden />
      )}
      <span className="font-label text-[10px] font-bold">{label}</span>
    </div>
  );
}
