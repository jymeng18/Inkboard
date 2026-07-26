import { Sparkles } from 'lucide-react'
import Reveal from './Reveal'
import RemoteImage from './RemoteImage'
import { Doodle } from './placeholders'
import { DOODLE_IMAGES } from './media'

const DOODLES = [
  { handle: '@bot_artist', age: '2h ago', tilt: '-rotate-2' },
  { handle: '@ink_splatter', age: '5h ago', tilt: 'rotate-3' },
  { handle: '@schema_queen', age: '10h ago', tilt: '-rotate-1' },
  { handle: '@cat_sketcher', age: '1d ago', tilt: 'rotate-2' },
]

export default function Doodles() {
  return (
    <section className="mx-auto mb-24 w-full max-w-5xl">
      <Reveal className="mb-10 flex items-center gap-3">
        <Sparkles className="size-7 fill-primary text-primary" aria-hidden />
        <h2 className="highlight-blob font-display text-4xl md:text-5xl">Doodles of the Day</h2>
      </Reveal>

      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-4">
        {DOODLES.map((doodle, i) => (
          <Reveal key={doodle.handle} delay={i * 0.08}>
            {/* Polaroid: extra bottom padding leaves room for the caption. */}
            <figure
              className={`group rounded-xl border-[3px] border-outline bg-surface p-3 pb-6 sticker-shadow-sm transition-transform hover:rotate-0 motion-reduce:transition-none ${doodle.tilt}`}
            >
              <div className="mb-3 aspect-square overflow-hidden rounded-lg border border-outline/10">
                <RemoteImage
                  src={DOODLE_IMAGES[i]}
                  alt={`Doodle by ${doodle.handle}`}
                  className="size-full object-cover transition-transform duration-500 group-hover:scale-105 motion-reduce:transition-none"
                  fallback={<Doodle index={i} className="size-full" />}
                />
              </div>
              <figcaption className="text-center font-handwriting text-base text-on-background/70">
                {doodle.handle} • {doodle.age}
              </figcaption>
            </figure>
          </Reveal>
        ))}
      </div>
    </section>
  )
}
