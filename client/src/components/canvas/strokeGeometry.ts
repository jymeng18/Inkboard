import { getStroke } from 'perfect-freehand'

export type StrokeTool = 'pencil' | 'brush' | 'eraser'

/*
 * perfect-freehand tuning per tool. Pencil is thin and firm; brush is fat and
 * pressure-tapered; eraser is a wide firm nib (rendered with destination-out).
 */
const OPTIONS: Record<StrokeTool, Parameters<typeof getStroke>[1]> = {
  pencil: { size: 5, thinning: 0.5, smoothing: 0.5, streamline: 0.5, simulatePressure: false },
  brush: { size: 18, thinning: 0.7, smoothing: 0.55, streamline: 0.4, simulatePressure: false },
  eraser: { size: 30, thinning: 0, smoothing: 0.5, streamline: 0.5, simulatePressure: false },
}

/** Builds the closed outline polygon for a stroke, flattened to [x, y, x, y, …]. */
export function strokeOutline(points: number[][], tool: StrokeTool, done: boolean): number[] {
  const outline = getStroke(points, { ...OPTIONS[tool], last: done })
  const flat: number[] = []
  for (const point of outline) {
    flat.push(point[0], point[1])
  }
  return flat
}
