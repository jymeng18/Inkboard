import { PartyPopper } from 'lucide-react'
import Reveal from './Reveal'
import { Button } from '@/components/ui/button'

interface FinalCtaProps {
  onStart: () => void
}

export default function FinalCta({ onStart }: FinalCtaProps) {
  return (
    <section className="mb-24 w-full max-w-3xl">
      <Reveal>
        <div className="relative overflow-hidden rounded-[44px] border-[3px] border-outline bg-primary-container p-8 text-center sticker-shadow sm:p-12">
          <PartyPopper
            className="absolute top-4 right-4 size-20 rotate-45 opacity-20"
            aria-hidden
          />

          <h2 className="mb-6 font-display text-4xl leading-none md:text-5xl">
            Ready to unleash <br /> the kaos?
          </h2>

          <Button size="lg" onClick={onStart}>
            Join the Inkboard
          </Button>

          <p className="mt-6 font-handwriting text-lg font-bold opacity-60">
            Over 1,200 boards created today!
          </p>
        </div>
      </Reveal>
    </section>
  )
}
