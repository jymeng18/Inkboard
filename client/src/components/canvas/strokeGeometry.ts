import { getStroke } from 'perfect-freehand'

export type StrokeTool = 'pencil' | 'brush' | 'marker' | 'calligraphy' | 'eraser'

/*
 * perfect-freehand tuning per tool. The width comes from the size slider and is
 * passed in per stroke, so what stays here is each tool's character:
 *  - pencil: firm, lightly tapered.
 *  - brush: pressure-tapered round nib.
 *  - marker: uniform width with flat, uncapped ends, for bold highlighter strokes.
 *  - calligraphy: speed-driven taper (simulated pressure) with drawn-out tips.
 *  - eraser: firm even nib, rendered destination-out.
 */
const OPTIONS: Record<StrokeTool, Parameters<typeof getStroke>[1]> = {
  pencil: { thinning: 0.5, smoothing: 0.5, streamline: 0.5, simulatePressure: false },
  brush: { thinning: 0.7, smoothing: 0.55, streamline: 0.4, simulatePressure: false },
  marker: {
    thinning: 0,
    smoothing: 0.4,
    streamline: 0.35,
    simulatePressure: false,
    start: { cap: false },
    end: { cap: false },
  },
  calligraphy: {
    thinning: 0.85,
    smoothing: 0.5,
    streamline: 0.3,
    simulatePressure: true,
    start: { taper: 12 },
    end: { taper: 12 },
  },
  eraser: { thinning: 0, smoothing: 0.5, streamline: 0.5, simulatePressure: false },
}

/** Builds the closed outline polygon for a stroke, flattened to [x, y, x, y, …]. */
export function strokeOutline(
  points: number[][],
  tool: StrokeTool,
  size: number,
  done: boolean,
): number[] {
  const outline = getStroke(points, { ...OPTIONS[tool], size, last: done })
  const flat: number[] = []
  for (const point of outline) {
    flat.push(point[0], point[1])
  }
  return flat
}
