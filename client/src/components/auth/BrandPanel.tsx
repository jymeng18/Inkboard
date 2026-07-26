import { Brush, Users } from 'lucide-react'

const FEATURES = [
  { icon: Brush, label: 'Real-time Art' },
  { icon: Users, label: 'Global Parties' },
]

export default function BrandPanel() {
  return (
    <div className="relative hidden flex-col justify-center overflow-hidden border-r-4 border-outline bg-background px-12 canvas-bg lg:flex">
      <div className="relative z-10 mx-auto max-w-md text-center">
        <img
          src="/pen.svg"
          alt=""
          className="mx-auto mb-10 h-32 rotate-[-30deg] animate-float"
        />

        <h1 className="mb-5 font-display text-5xl leading-none xl:text-6xl">
          Unleash the <span className="text-primary highlight-blob">kaos</span>
        </h1>

        <p className="mb-10 font-body text-on-background/75">
          The collaborative digital playground where every stroke matters. Join the party, pick up
          your brush, and let your creativity run wild with friends.
        </p>

        <div className="grid grid-cols-2 gap-4">
          {FEATURES.map(({ icon: Icon, label }) => (
            <div
              key={label}
              className="rounded-2xl border-[3px] border-outline bg-surface p-5 sticker-shadow-sm"
            >
              <Icon className="mx-auto mb-2 size-6 text-primary" aria-hidden />
              <p className="font-label text-sm font-bold">{label}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
