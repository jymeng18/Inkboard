import { useCallback, useSyncExternalStore } from "react";

/* Subscribes to a CSS media query from JS, staying in sync if it flips. */
export function useMediaQuery(query: string): boolean {
  const subscribe = useCallback(
    (onChange: () => void) => {
      const list = window.matchMedia(query);
      list.addEventListener("change", onChange);
      return () => list.removeEventListener("change", onChange);
    },
    [query],
  );

  return useSyncExternalStore(
    subscribe,
    () => window.matchMedia(query).matches,
    () => false,
  );
}

/*
 * The ribbons trail the cursor, so they are pointless on touch devices and the
 * WebGL loop is exactly the sort of thing that drains a phone battery.
 */
export const useHasFinePointer = () =>
  useMediaQuery("(hover: hover) and (pointer: fine)");

export const usePrefersReducedMotion = () =>
  useMediaQuery("(prefers-reduced-motion: reduce)");
