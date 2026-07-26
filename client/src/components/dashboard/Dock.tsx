/**
 * Adapted from the Dock component at https://reactbits.dev/
 *
 * Reworked for a top-anchored toolbar: labels hang below the icons rather than
 * above, the panel sits in normal flow instead of pinned to the bottom of the
 * viewport, and it reserves its magnified height so hovering never resizes the
 * bar it lives in. Cursor tracking moved from `pageX` to `clientX` — the
 * original compared page coordinates against `getBoundingClientRect`, which
 * returns viewport ones, so the two drifted apart on any scrolled page. Items
 * are real buttons, and everything collapses to a plain static row under
 * prefers-reduced-motion.
 */

import {
  AnimatePresence,
  motion,
  useMotionValue,
  useSpring,
  useTransform,
  type MotionValue,
  type SpringOptions,
} from "motion/react";
import React, { useRef, useState } from "react";

import { usePrefersReducedMotion } from "@/hooks/useMediaQuery";

export type DockItemData = {
  icon: React.ReactNode;
  label: string;
  onClick: () => void;
  /** Selected state, exposed as `aria-pressed` for toggle-style items. */
  active?: boolean;
  className?: string;
};

export type DockProps = {
  items: DockItemData[];
  className?: string;
  /** How far from an item the cursor starts pulling on it, in pixels. */
  distance?: number;
  baseItemSize?: number;
  magnification?: number;
  spring?: SpringOptions;
  /** Which side the hover label sits on. Defaults below, for a top bar. */
  labelPlacement?: "top" | "bottom";
};

type DockItemProps = DockItemData & {
  mouseX: MotionValue<number>;
  spring: SpringOptions;
  distance: number;
  baseItemSize: number;
  magnification: number;
  labelPlacement: "top" | "bottom";
  reduced: boolean;
};

function DockItem({
  icon,
  label,
  onClick,
  active,
  className = "",
  mouseX,
  spring,
  distance,
  magnification,
  baseItemSize,
  labelPlacement,
  reduced,
}: DockItemProps) {
  const ref = useRef<HTMLButtonElement>(null);
  const [labelVisible, setLabelVisible] = useState(false);

  /*
   * Distance from the cursor to this item's resting center. Measuring against
   * the resting half-size rather than the live one keeps an item from chasing
   * its own growth once neighbours start pushing the row around.
   */
  const mouseDistance = useTransform(mouseX, (value) => {
    const rect = ref.current?.getBoundingClientRect();
    if (!rect) return Number.POSITIVE_INFINITY;
    return value - rect.left - baseItemSize / 2;
  });

  const targetSize = useTransform(
    mouseDistance,
    [-distance, 0, distance],
    [baseItemSize, magnification, baseItemSize],
  );
  const size = useSpring(targetSize, spring);
  const iconScale = useTransform(size, (value) => value / baseItemSize);

  return (
    <motion.button
      ref={ref}
      type="button"
      onClick={onClick}
      onHoverStart={() => setLabelVisible(true)}
      onHoverEnd={() => setLabelVisible(false)}
      onFocus={() => setLabelVisible(true)}
      onBlur={() => setLabelVisible(false)}
      aria-label={label}
      aria-pressed={active}
      style={
        reduced
          ? { width: baseItemSize, height: baseItemSize }
          : { width: size, height: size }
      }
      className={`relative flex shrink-0 cursor-pointer items-center justify-center rounded-full border-[3px] outline-none transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary ${
        active
          ? "border-outline bg-primary text-white sticker-shadow-sm"
          : "border-transparent text-on-background/70 hover:bg-background hover:text-on-background"
      } ${className}`}
    >
      <motion.span
        aria-hidden
        className="flex items-center justify-center"
        style={reduced ? undefined : { scale: iconScale }}
      >
        {icon}
      </motion.span>

      <DockLabel
        visible={labelVisible}
        placement={labelPlacement}
        reduced={reduced}
      >
        {label}
      </DockLabel>
    </motion.button>
  );
}

type DockLabelProps = {
  children: React.ReactNode;
  visible: boolean;
  placement: "top" | "bottom";
  reduced: boolean;
};

/*
 * The label repeats the button's aria-label, so it stays hidden from assistive
 * tech. It must not take pointer events either — it hangs over the drawing
 * surface, and the toolbar around it is `pointer-events-auto`.
 *
 * Positioning lives on the static outer span: motion writes its own transform,
 * which would otherwise overwrite a Tailwind translate class.
 */
function DockLabel({ children, visible, placement, reduced }: DockLabelProps) {
  const below = placement === "bottom";
  const offset = reduced ? {} : { y: below ? -6 : 6 };

  return (
    <span
      aria-hidden
      className={`pointer-events-none absolute left-1/2 z-10 -translate-x-1/2 ${
        below ? "top-full mt-2" : "bottom-full mb-2"
      }`}
    >
      <AnimatePresence>
        {visible && (
          <motion.span
            initial={{ opacity: 0, ...offset }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            transition={{ duration: reduced ? 0 : 0.18 }}
            className="block w-fit rounded-full border-[3px] border-outline bg-surface px-3 py-1 font-label text-xs font-bold whitespace-pre text-on-background sticker-shadow-sm"
          >
            {children}
          </motion.span>
        )}
      </AnimatePresence>
    </span>
  );
}

export default function Dock({
  items,
  className = "",
  spring = { mass: 0.1, stiffness: 150, damping: 12 },
  magnification = 70,
  distance = 200,
  baseItemSize = 50,
  labelPlacement = "bottom",
}: DockProps) {
  const reduced = usePrefersReducedMotion();
  const mouseX = useMotionValue(Number.POSITIVE_INFINITY);

  return (
    <div
      /* Nothing responds to the cursor when motion is reduced, so skip tracking it. */
      onMouseMove={reduced ? undefined : ({ clientX }) => mouseX.set(clientX)}
      onMouseLeave={
        reduced ? undefined : () => mouseX.set(Number.POSITIVE_INFINITY)
      }
      /*
       * Holding the magnified height keeps a swelling icon from growing the
       * toolbar around it, which would shove the whole bar on every hover.
       */
      style={reduced ? undefined : { minHeight: magnification }}
      className={`flex items-center gap-1 ${className}`}
    >
      {items.map((item) => (
        <DockItem
          key={item.label}
          {...item}
          mouseX={mouseX}
          spring={spring}
          distance={distance}
          magnification={magnification}
          baseItemSize={baseItemSize}
          labelPlacement={labelPlacement}
          reduced={reduced}
        />
      ))}
    </div>
  );
}
