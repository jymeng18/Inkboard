import {
  ArrowRight,
  ArrowUpRight,
  Crown,
  Link as LinkIcon,
  MousePointer2,
  Star,
  Users,
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
            It&rsquo;s a toy. That was always the point.
          </p>
          <h2 className="font-headline text-4xl font-extrabold tracking-tight md:text-6xl">
            Why it&rsquo;s so <span className="strike-through">serious</span>{' '}
            <span className="inline-block -rotate-2 font-display text-primary italic">
              silly.
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
          {/* Draw together — the anchor card, largest and tilted left */}
          <Reveal className="md:col-span-5">
            <div className="group -rotate-2 transition-transform duration-300 hover:rotate-0">
              <div className={`${CARD} relative h-full rounded-2xl p-6 sticker-shadow`}>
                <span className="absolute -top-5 -right-5 rotate-12 rounded-lg border-2 border-outline bg-primary-fixed px-2.5 py-1 font-handwriting text-lg text-primary">
                  Chaos? Yes.
                </span>

                <div className="mb-5 flex size-11 items-center justify-center rounded-xl border-2 border-outline bg-primary">
                  <Users className="size-5 text-white" aria-hidden />
                </div>
                <h3 className="mb-3 font-headline text-2xl font-bold">Draw all at once</h3>
                <p className="mb-6 text-sm leading-relaxed text-on-background/70">
                  You and up to four friends on one canvas, scribbling right on top of each
                  other. Every marker moves live, so it feels like crowding around the same
                  sheet of paper.
                </p>

                <div className="mt-auto flex gap-2 border-t border-dashed border-outline/20 pt-5">
                  {['Live', 'Up to 5 people'].map((tag) => (
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
                  <LinkIcon className="size-4" aria-hidden />
                </div>
                <h3 className="mb-2 font-headline text-xl font-bold">Just share a link</h3>
                <p className="mb-4 text-sm leading-relaxed text-on-background/70">
                  No accounts, no installs, no setup screen. Send a friend the room link and
                  they&rsquo;re drawing next to you a few seconds later.
                </p>
                <p className="font-label text-[10px] font-black tracking-[0.2em] uppercase opacity-40">
                  Zero setup
                </p>
              </div>
            </div>
          </Reveal>

          {/* Live cursors */}
          <Reveal className="md:col-span-3 md:mt-10" delay={0.16}>
            <div className="group -rotate-1 transition-transform duration-300 hover:rotate-0">
              <div className={`${CARD} rounded-2xl p-5 sticker-shadow`}>
                <div className="mb-4 flex size-9 items-center justify-center rounded-lg border-2 border-outline bg-secondary-container">
                  <MousePointer2 className="size-4 text-on-secondary-container" aria-hidden />
                </div>
                <h3 className="mb-2 font-headline text-xl font-bold">See every cursor</h3>
                <p className="mb-5 text-sm leading-relaxed text-on-background/70">
                  Watch your friends&rsquo; markers zoom around the page. Half the fun is the
                  mess in progress, not the finished picture.
                </p>
                <span className="self-start rounded bg-on-background px-2.5 py-1 font-label text-[10px] font-bold text-white">
                  IT&rsquo;S ALIVE
                </span>
              </div>
            </div>
          </Reveal>

          {/* Infinite canvas; wide, offset right */}
          <Reveal className="md:col-span-8 md:-mt-2 md:ml-6" delay={0.24}>
            <div className="group rotate-1 transition-transform duration-300 hover:rotate-0">
              <div
                className={`${CARD} items-center gap-6 rounded-3xl p-6 sticker-shadow md:flex-row`}
              >
                <div className="flex-1">
                  <span className="mb-3 inline-block rounded border border-outline bg-tertiary-fixed px-2 py-0.5 font-label text-[10px] font-bold">
                    ROOM TO ROAM
                  </span>
                  <h3 className="mb-3 font-headline text-2xl font-bold">The canvas never ends</h3>
                  <p className="mb-5 text-sm leading-relaxed text-on-background/70">
                    Zoom out and it just keeps going. Fill a wall, wander off to a new corner,
                    sprawl as far as the doodle takes you. You will not run out of room.
                  </p>
                  <a
                    href="#top"
                    className="flex items-center gap-2 font-body text-sm font-bold text-primary transition-all hover:gap-4"
                  >
                    Grab a marker <ArrowRight className="size-4" aria-hidden />
                  </a>
                </div>

                <div className="relative flex h-36 w-full items-center justify-center overflow-hidden rounded-2xl border-2 border-outline bg-inverse-surface p-5 md:w-56">
                  <div className="absolute inset-0 bg-[radial-gradient(#ffffff_1px,transparent_1px)] bg-size-[10px_10px] opacity-10" />
                  <svg
                    viewBox="0 0 160 90"
                    fill="none"
                    aria-hidden
                    className="z-10 w-full"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <rect
                      x="38"
                      y="22"
                      width="84"
                      height="46"
                      rx="8"
                      stroke="#75d1ff"
                      strokeWidth="2"
                      strokeDasharray="6 7"
                    />
                    <path d="M56 52 Q72 30 88 50 T120 44" stroke="#ffd23f" strokeWidth="4" />
                    <path d="M20 45 h12 M128 45 h12 M80 8 v12 M80 70 v12" stroke="#ffffff" strokeWidth="2" opacity="0.5" />
                  </svg>
                </div>
              </div>
            </div>
          </Reveal>

          {/* Your room, your rules — compact, off-palette, tilted hardest */}
          <Reveal className="md:col-span-4 md:mt-6" delay={0.32}>
            <div className="group -rotate-4 transition-transform duration-300 hover:-rotate-2">
              <div
                className={`${CARD} relative items-center justify-center overflow-hidden rounded-3xl bg-secondary-fixed p-6 text-center sticker-shadow-sm`}
              >
                <Crown
                  className="pointer-events-none absolute top-1 right-1 size-24 opacity-10"
                  aria-hidden
                />
                <div className="mb-5 flex size-14 items-center justify-center rounded-full border-[3px] border-outline bg-surface sticker-shadow-sm transition-transform group-hover:scale-110">
                  <Crown className="size-6 text-outline" aria-hidden />
                </div>
                <h3 className="mb-2 font-headline text-xl font-bold">Your room, your rules</h3>
                <p className="mb-4 text-sm font-medium opacity-80">
                  Invite the whole group, boot the troll, wipe it clean and start over. The
                  room is yours to run.
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
