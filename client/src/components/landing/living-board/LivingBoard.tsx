import { Suspense, lazy, useEffect, useRef, useState } from "react";
import {
  useHasFinePointer,
  usePrefersReducedMotion,
} from "@/hooks/useMediaQuery";
import StaticBoard from "./StaticBoard";

/*
 * The Three scene is a separate chunk that only ever loads on devices that will
 * actually run it. Touch / reduced-motion visitors get the static board and
 * never download three at all.
 */
const BoardScene = lazy(() => import("./BoardScene"));

interface LivingBoardProps {
  /* Lets the hero size the board for its layout (centered vs. side-by-side). */
  className?: string;
}

export default function LivingBoard({
  className = "relative mx-auto aspect-[16/10] w-full max-w-4xl",
}: LivingBoardProps) {
  const finePointer = useHasFinePointer();
  const reducedMotion = usePrefersReducedMotion();
  const interactive = finePointer && !reducedMotion;

  const ref = useRef<HTMLDivElement>(null);
  const [inView, setInView] = useState(true);

  useEffect(() => {
    if (!interactive) return;
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      ([entry]) => setInView(entry.isIntersecting),
      { rootMargin: "200px" },
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, [interactive]);

  return (
    <div ref={ref} className={className}>
      {interactive ? (
        <Suspense fallback={<StaticBoard />}>
          <BoardScene active={inView} />
        </Suspense>
      ) : (
        <StaticBoard />
      )}
    </div>
  );
}
