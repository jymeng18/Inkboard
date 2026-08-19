import { Check } from 'lucide-react'
import RemoteImage from './RemoteImage'
import { Doodle } from './placeholders'
import { DOODLE_IMAGES } from './media'

const PERKS = ['Up to 5 artists per room', 'Strokes sync in real time', 'Snapshots saved for you']


export default function PartyMode() {
  return (
    <section
      id="overview"
      className="mb-24 self-stretch scroll-mt-24 border-y-4 border-outline bg-primary-container py-20 mx-[calc(50%-50vw)]"
    >
      <div className="mx-auto flex max-w-5xl flex-col items-center gap-14 px-6 md:flex-row">
        <div className="order-2 flex-1 md:order-1">
          {/* Two canvases stacked at opposing angles, as if dropped on a desk */}
          <div className="relative mx-auto aspect-square w-full max-w-sm">
            <RemoteImage
              src={DOODLE_IMAGES[1]}
              alt=""
              className="absolute inset-0 size-full -rotate-3 rounded-xl border-4 border-outline object-cover opacity-40 grayscale sticker-shadow-sm"
              fallback={
                <div className="absolute inset-0 size-full -rotate-3 overflow-hidden rounded-xl border-4 border-outline opacity-40 grayscale sticker-shadow-sm">
                  <Doodle index={1} className="size-full" />
                </div>
              }
            />
            <RemoteImage
              src={DOODLE_IMAGES[3]}
              alt="A doodle drawn collaboratively in an Inkboard room"
              className="absolute top-8 right-0 z-10 h-4/5 w-4/5 rotate-3 rounded-xl border-4 border-outline object-cover sticker-shadow"
              fallback={
                <div className="absolute top-8 right-0 z-10 h-4/5 w-4/5 rotate-3 overflow-hidden rounded-xl border-4 border-outline sticker-shadow">
                  <Doodle index={3} className="size-full" />
                </div>
              }
            />
          </div>
        </div>

        <div className="order-1 flex-1 md:order-2">
          <span className="mb-6 inline-block rounded-full border-2 border-outline bg-surface px-4 py-1 font-label text-[10px] font-extrabold uppercase sticker-shadow-sm">
            Party Mode
          </span>

          <ul className="mb-7 space-y-3">
            {PERKS.map((perk) => (
              <li key={perk} className="flex items-center gap-3 font-body font-bold">
                <Check
                  className="size-5 shrink-0 rounded-full bg-on-background p-0.5 text-white"
                  aria-hidden
                />
                {perk}
              </li>
            ))}
          </ul>

          <h2 className="mb-6 font-display text-4xl leading-none md:text-6xl">
            You draw the stick figure. <br />
            <span className="text-white drop-shadow-[2px_2px_0_var(--color-outline)]">
              They draw the rest.
            </span>
          </h2>

          <p className="max-w-md font-body text-on-background/80">
            Stuck on a doodle? Hand the marker to someone else. Invite up to four others into the
            same room and watch the canvas fill in as you go — every stroke synced, nothing ever
            locked by another user.
          </p>
        </div>
      </div>
    </section>
  )
}
