import { Button } from "@/components/ui/button";
import LivingBoard from "./living-board/LivingBoard";

interface HeroProps {
  onStart: () => void;
}

export default function Hero({ onStart }: HeroProps) {
  return (
    <section className="relative mb-24 w-full max-w-7xl pt-6 lg:pt-10">
      <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-6">
        {/* Copy */}
        <div className="text-center lg:col-span-5 lg:text-left">
          <h1 className="mb-6 font-display text-6xl leading-[0.9] sm:text-7xl xl:text-8xl">
            Draw dumb{" "}
            <span className="highlight-blob">stuff,</span>
            <br />
            <span className="inline-block rotate-3 text-primary italic">
              together.
            </span>
          </h1>

          <p className="mx-auto mb-8 max-w-md font-body text-base text-on-background/70 md:text-lg lg:mx-0">
            A shared canvas where you and your friends scribble at the same time.
            No sign-ups, no lag, no rules. Just grab a marker and make a
            beautiful mess.
          </p>

          <div className="flex flex-col items-center gap-4 sm:flex-row sm:justify-center lg:justify-start">
            <Button size="lg" onClick={onStart}>
              Grab a Marker
            </Button>
            <Button variant="surface" size="lg" onClick={scrollToCanvas}>
              Peek at the mess ↓
            </Button>
          </div>
        </div>

        {/* Live board */}
        <div className="relative lg:col-span-7">
          <LivingBoard className="relative aspect-video w-full lg:aspect-3/2" />
        </div>
      </div>
    </section>
  );
}

function scrollToCanvas() {
  document.getElementById("canvas")?.scrollIntoView({ behavior: "smooth" });
}
