import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";

export default function NotFoundPage() {
  return (
    <div className="canvas-bg relative grid min-h-dvh place-items-center overflow-hidden bg-background px-6 font-body text-on-background">

      <div className="relative z-10 flex max-w-xl flex-col items-center text-center">
        <div className="mb-8 -rotate-3 rounded-4xl border-[3px] border-outline bg-primary-container px-10 py-4 sticker-shadow-lg">
          <span className="font-display text-8xl leading-none sm:text-9xl">404</span>
        </div>

        <h1 className="mb-4 font-display text-4xl leading-[0.95] sm:text-5xl">
          You drew outside <span className="highlight-blob">the lines.</span>
        </h1>

        <p className="mb-8 max-w-md text-base text-on-background/70 md:text-lg">
          This page isn't on any of our canvases. Maybe it got erased, or maybe
          it was never scribbled in the first place.
        </p>

        <div className="flex flex-col items-center gap-4 sm:flex-row">
          <Button asChild size="lg">
            <Link to="/">Back to safety</Link>
          </Button>
          <Button asChild variant="surface" size="lg">
            <Link to="/dashboard">Go to my boards</Link>
          </Button>
        </div>

        <p className="mt-8 font-handwriting text-lg font-bold text-on-background/50">
          Error 404 — nothing to see here (yet).
        </p>
      </div>
    </div>
  );
}
