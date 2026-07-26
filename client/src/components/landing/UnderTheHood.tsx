import {
  ArrowRight,
  ArrowUpRight,
  MousePointer2,
  Network,
  ShieldCheck,
  Star,
  Zap,
} from 'lucide-react'
import Reveal from './Reveal'

/* Every card in the collage shares this shell; only tilt and payload differ. */
const CARD = 'flex flex-col border-[3px] border-outline bg-surface'

export default function UnderTheHood() {
  return (
    <>
      <section className="relative mb-14 max-w-3xl scroll-mt-24 text-center" id="features">
        <Reveal>
          <p className="mb-3 font-body text-base text-on-background/60 md:text-lg">
            This isn't just a toy. It's a distributed systems problem disguised as a doodle pad.
          </p>
          <h2 className="font-headline text-4xl font-extrabold tracking-tight md:text-6xl">
            Under the <span className="strike-through">hood</span>{' '}
            <span className="inline-block -rotate-2 font-display text-primary italic">
              Scribble.
            </span>
          </h2>
        </Reveal>
      </section>

      <section className="canvas-bg relative mb-24 w-full max-w-5xl rounded-[40px] border-4 border-dashed border-outline-variant/40 px-6 py-16 sm:px-8">
        {/* Decorative doodles scattered behind the cards */}
        <ArrowUpRight
          className="pointer-events-none absolute top-8 left-[40%] size-12 -rotate-45 text-primary/40"
          aria-hidden
        />
        <Star
          className="pointer-events-none absolute right-[5%] bottom-16 size-14 rotate-12 fill-primary-container text-primary-container"
          aria-hidden
        />

        <div className="grid grid-cols-1 items-start gap-6 md:grid-cols-12">
          {/* Operational Transforms — the anchor card, largest and tilted left */}
          <Reveal className="md:col-span-5">
            <div className="group -rotate-2 transition-transform duration-300 hover:rotate-0">
              <div className={`${CARD} relative h-full rounded-2xl p-6 sticker-shadow`}>
                <span className="absolute -top-5 -right-5 rotate-12 rounded-lg border-2 border-outline bg-primary-fixed px-2.5 py-1 font-handwriting text-lg text-primary">
                  Conflicts? 0.
                </span>

                <div className="mb-5 flex size-11 items-center justify-center rounded-xl border-2 border-outline bg-primary">
                  <Network className="size-5 text-white" aria-hidden />
                </div>
                <h3 className="mb-3 font-headline text-2xl font-bold">Operational Transforms</h3>
                <p className="mb-6 text-sm leading-relaxed text-on-background/70">
                  Two people drawing on the same pixel? Operations carry a timestamp and a user ID,
                  and resolve in a deterministic order before you blink.
                </p>

                <div className="mt-auto flex gap-2 border-t border-dashed border-outline/20 pt-5">
                  {['SignalR', 'Last-Write-Wins'].map((tag) => (
                    <span
                      key={tag}
                      className="rounded-lg border-2 border-outline bg-background px-2.5 py-1 font-label text-[10px] font-bold tracking-tight uppercase"
                    >
                      {tag}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </Reveal>

          <Reveal className="md:col-span-4 md:-mt-10" delay={0.08}>
            <div className="group rotate-3 transition-transform duration-300 hover:rotate-0">
              <div className={`${CARD} rounded-2xl p-5 sticker-shadow-sm`}>
                <div className="mb-4 flex size-9 items-center justify-center rounded-lg border-2 border-outline bg-primary-container">
                  <Zap className="size-4" aria-hidden />
                </div>
                <h3 className="mb-2 font-headline text-xl font-bold">Socket Blast</h3>
                <p className="mb-4 text-sm leading-relaxed text-on-background/70">
                  State streams over WebSockets. We broadcast delta operations, never the full
                  canvas, keeping bandwidth lower than a 90s modem.
                </p>
                <p className="font-label text-[10px] font-black tracking-[0.2em] uppercase opacity-40">
                  Real-time Sync
                </p>
              </div>
            </div>
          </Reveal>

          {/* Live Cursors; replaces the AI card from the comp */}
          <Reveal className="md:col-span-3 md:mt-10" delay={0.16}>
            <div className="group -rotate-1 transition-transform duration-300 hover:rotate-0">
              <div className={`${CARD} rounded-2xl p-5 sticker-shadow`}>
                <div className="mb-4 flex size-9 items-center justify-center rounded-lg border-2 border-outline bg-secondary-container">
                  <MousePointer2 className="size-4 text-on-secondary-container" aria-hidden />
                </div>
                <h3 className="mb-2 font-headline text-xl font-bold">Live Cursors</h3>
                <p className="mb-5 text-sm leading-relaxed text-on-background/70">
                  Every pointer in the room, throttled to 30 updates a second and interpolated
                  client-side so the motion stays smooth.
                </p>
                <span className="self-start rounded bg-on-background px-2.5 py-1 font-label text-[10px] font-bold text-white">
                  FIRE-AND-FORGET
                </span>
              </div>
            </div>
          </Reveal>

          {/* Binary Blob Handling; wide, offset right */}
          <Reveal className="md:col-span-8 md:-mt-2 md:ml-6" delay={0.24}>
            <div className="group rotate-1 transition-transform duration-300 hover:rotate-0">
              <div
                className={`${CARD} items-center gap-6 rounded-3xl p-6 sticker-shadow md:flex-row`}
              >
                <div className="flex-1">
                  <span className="mb-3 inline-block rounded border border-outline bg-tertiary-fixed px-2 py-0.5 font-label text-[10px] font-bold">
                    PERFORMANCE
                  </span>
                  <h3 className="mb-3 font-headline text-2xl font-bold">Binary Blob Handling</h3>
                  <p className="mb-5 text-sm leading-relaxed text-on-background/70">
                    We don't base64 encode everything. Snapshots leave the canvas as binary buffers
                    and land in blob storage, keyed by canvas ID.
                  </p>
                  <a
                    href="#"
                    className="flex items-center gap-2 font-body text-sm font-bold text-primary transition-all hover:gap-4"
                  >
                    Read the architecture notes <ArrowRight className="size-4" aria-hidden />
                  </a>
                </div>

                <div className="relative flex h-36 w-full items-center justify-center overflow-hidden rounded-2xl border-2 border-outline bg-inverse-surface p-5 md:w-56">
                  <div className="absolute inset-0 bg-[radial-gradient(#ffffff_1px,transparent_1px)] bg-size-[10px_10px] opacity-10" />
                  <pre className="z-10 font-mono text-[11px] leading-loose text-secondary-fixed-dim">
                    01101001 01101110{'\n'}
                    <span className="text-primary-container">canvas_stream:</span> 0x88f2{'\n'}
                    <span className="text-green-400">buffer_alloc:</span> 2048kb
                  </pre>
                </div>
              </div>
            </div>
          </Reveal>

          {/* Room Authority — compact, off-palette, tilted hardest */}
          <Reveal className="md:col-span-4 md:mt-6" delay={0.32}>
            <div className="group -rotate-4 transition-transform duration-300 hover:-rotate-2">
              <div
                className={`${CARD} relative items-center justify-center overflow-hidden rounded-3xl bg-secondary-fixed p-6 text-center sticker-shadow-sm`}
              >
                <ShieldCheck
                  className="pointer-events-none absolute top-1 right-1 size-24 opacity-10"
                  aria-hidden
                />
                <div className="mb-5 flex size-14 items-center justify-center rounded-full border-[3px] border-outline bg-surface sticker-shadow-sm transition-transform group-hover:scale-110">
                  <ShieldCheck className="size-6 text-outline" aria-hidden />
                </div>
                <h3 className="mb-2 font-headline text-xl font-bold">Room Authority</h3>
                <p className="mb-4 text-sm font-medium opacity-80">
                  One leader, up to five seats. Invites, kicks, and blocks are enforced server-side.
                </p>
                <div className="h-1 w-full rounded-full bg-outline/20" />
              </div>
            </div>
          </Reveal>
        </div>
      </section>
    </>
  )
}
