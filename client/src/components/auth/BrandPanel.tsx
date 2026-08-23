import BubbleKeywords from './BubbleKeywords'

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

        <BubbleKeywords />
      </div>
    </div>
  )
}
