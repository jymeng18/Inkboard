/*
 * Pure geometry for the outline shapes, shared by the live/committed renderer
 * (ShapeNode, react-konva) and the snapshot rasteriser (canvasSnapshot, imperative
 * Konva) so the two can never compute a shape differently. Given two points — a
 * bounding-box corner pair, or a line's endpoints — it derives the box metrics and
 * the polygon point lists each shape needs.
 */

export interface BoxMetrics {
  minX: number
  minY: number
  width: number
  height: number
  cx: number
  cy: number
}

export function boxMetrics(ax: number, ay: number, bx: number, by: number): BoxMetrics {
  const minX = Math.min(ax, bx)
  const minY = Math.min(ay, by)
  const width = Math.abs(bx - ax)
  const height = Math.abs(by - ay)
  return { minX, minY, width, height, cx: minX + width / 2, cy: minY + height / 2 }
}

export function trianglePoints({ minX, minY, width, height, cx }: BoxMetrics): number[] {
  return [cx, minY, minX, minY + height, minX + width, minY + height]
}

export function diamondPoints({ minX, minY, width, height, cx, cy }: BoxMetrics): number[] {
  return [cx, minY, minX + width, cy, cx, minY + height, minX, cy]
}

/* A star sized to the box: outer radius fills the smaller half-extent, inner half that. */
export function starRadii(m: BoxMetrics): { inner: number; outer: number } {
  const outer = Math.min(m.width, m.height) / 2
  return { inner: outer / 2, outer }
}

