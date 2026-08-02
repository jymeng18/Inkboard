import RemoteImage from "./RemoteImage";
import { Button } from "@/components/ui/button";
import { Avatar } from "./placeholders";
import { AVATAR_IMAGES } from "./media";
import { LiveCounter } from "./CountUp";

interface HeroProps {
  onStart: () => void;
}

export default function Hero({ onStart }: HeroProps) {
  return (
    <section className="relative mb-20 text-center">
      <h1 className="relative mb-6 font-display text-5xl leading-[0.95] sm:text-6xl md:text-7xl">
        Unleash the{" "}
        <span className="inline-block rotate-2 text-primary italic">kaos.</span>
        <br />
        Sync <span className="highlight-blob">the Art.</span>
      </h1>

      <p className="mx-auto mb-9 max-w-xl font-body text-base text-on-background/70 md:text-lg">
        A collaborative infinity canvas powered by{" "}
        <span className="font-bold text-on-background">
          SignalR WebSockets & KonvaJS
        </span>
        . No conflicts, just pure creative flow.
      </p>

      <div className="flex flex-col items-center justify-center gap-5 sm:flex-row">
        <div className="relative">
          <Button size="lg" onClick={onStart}>
            Grab a Marker
          </Button>
          <span className="pointer-events-none absolute -top-3 -right-4 rotate-12 rounded-sm border-2 border-outline bg-primary-container px-1.5 py-0.5 font-label text-[10px] font-bold">
            FREE FOREVER
          </span>
        </div>

        <Button variant="surface" size="lg" onClick={scrollToCanvas}>
          View the Architecture ↓
        </Button>
      </div>

      <div className="mt-12 flex items-center justify-center gap-3">
        <div className="flex -space-x-3">
          {AVATAR_IMAGES.map((src, i) => (
            <RemoteImage
              key={src}
              src={src}
              alt=""
              className="size-10 rounded-full border-[3px] border-outline object-cover"
              fallback={
                <Avatar
                  index={i}
                  className="size-10 rounded-full border-[3px] border-outline"
                />
              }
            />
          ))}
        </div>
        <p className="font-handwriting text-base text-on-background/60">
          <span className="font-bold text-primary">Rin</span> and<LiveCounter />others are sketching now.
        </p>
      </div>
    </section>
  );
}

function scrollToCanvas() {
  document.getElementById("canvas")?.scrollIntoView({ behavior: "smooth" });
}
