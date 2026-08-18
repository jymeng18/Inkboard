import Konva from 'konva'

import {
  boxMetrics,
  diamondPoints,
  starRadii,
  trianglePoints,
} from '@/components/canvas/shapeGeometry'
import { strokeOutline } from '@/components/canvas/strokeGeometry'
import type { LineItem, RectItem, SceneItem, ShapeItem } from '@/stores/sceneStore'

/*
 * Snapshot rasterisation. The stage is an infinite pan/zoom surface, so instead
 * of exporting a viewport we compute the box around everything that has been
 * drawn and render only that region through a detached Konva stage. This keeps
 * the output independent of the current zoom and of the on-screen canvas, and
 * matches the two-layer model where the committed scene is the source of truth.
 */

// Transparent margin baked around the content, in world units.
const PADDING = 24

// Longest edge of the exported image. Well under the server's hard limit while
// keeping the payload small.
const MAX_DIMENSION = 2048

// Floor for either edge so a tiny drawing still clears the server minimum.
const MIN_DIMENSION = 64

type Bounds = { minX: number; minY: number; maxX: number; maxY: number }

function expand(bounds: Bounds | null, x: number, y: number): Bounds {
  if (!bounds) return { minX: x, minY: y, maxX: x, maxY: y }
  return {
    minX: Math.min(bounds.minX, x),
    minY: Math.min(bounds.minY, y),
    maxX: Math.max(bounds.maxX, x),
    maxY: Math.max(bounds.maxY, y),
  }
}

/*
 * Tight world-space box around every visible mark. Eraser strokes only remove
 * ink, so they never grow the box. Returns null when nothing has been drawn,
 * which the caller treats as "no snapshot to take".
 */
export function contentBounds(items: SceneItem[]): Bounds | null {
  let bounds: Bounds | null = null

  for (const item of items) {
    if (item.kind === 'stroke') {
      if (item.tool === 'eraser') continue
      const outline = strokeOutline(item.points, item.tool, item.size, true)
      for (let i = 0; i + 1 < outline.length; i += 2) {
        bounds = expand(bounds, outline[i], outline[i + 1])
      }
    } else if (item.kind === 'line') {
      const half = item.strokeWidth / 2
      bounds = expand(bounds, Math.min(item.x1, item.x2) - half, Math.min(item.y1, item.y2) - half)
      bounds = expand(bounds, Math.max(item.x1, item.x2) + half, Math.max(item.y1, item.y2) + half)
    } else {
      const half = item.strokeWidth / 2
      bounds = expand(bounds, item.x - half, item.y - half)
      bounds = expand(bounds, item.x + item.width + half, item.y + item.height + half)
    }
  }

  return bounds
}

function addItem(layer: Konva.Layer, item: SceneItem, offsetX: number, offsetY: number) {
  if (item.kind === 'stroke') {
    layer.add(
      new Konva.Line({
        points: strokeOutline(item.points, item.tool, item.size, true),
        x: offsetX,
        y: offsetY,
        closed: true,
        fill: item.tool === 'eraser' ? '#000' : item.color,
        globalCompositeOperation: item.tool === 'eraser' ? 'destination-out' : undefined,
        lineJoin: 'round',
        listening: false,
        perfectDrawEnabled: false,
      }),
    )
  } else if (item.kind === 'line') {
    addLine(layer, item, offsetX, offsetY)
  } else {
    addShape(layer, item, offsetX, offsetY)
  }
}

function addLine(layer: Konva.Layer, item: LineItem, offsetX: number, offsetY: number) {
  layer.add(
    new Konva.Line({
      x: offsetX,
      y: offsetY,
      points: [item.x1, item.y1, item.x2, item.y2],
      stroke: item.color,
      strokeWidth: item.strokeWidth,
      lineCap: 'round',
      lineJoin: 'round',
      listening: false,
      perfectDrawEnabled: false,
    }),
  )
}

/* Mirrors ShapeNode's geometry (shared via shapeGeometry) for the detached export stage. */
function addShape(
  layer: Konva.Layer,
  item: ShapeItem | RectItem,
  offsetX: number,
  offsetY: number,
) {
  const shape = item.kind === 'rect' ? 'rectangle' : item.shape
  const style = {
    x: offsetX,
    y: offsetY,
    stroke: item.color,
    strokeWidth: item.strokeWidth,
    lineJoin: 'round' as const,
    listening: false,
    perfectDrawEnabled: false,
  }
  const m = boxMetrics(item.x, item.y, item.x + item.width, item.y + item.height)

  switch (shape) {
    case 'rectangle':
      layer.add(new Konva.Rect({ ...style, x: m.minX + offsetX, y: m.minY + offsetY, width: m.width, height: m.height }))
      break
    case 'ellipse':
      layer.add(
        new Konva.Ellipse({
          ...style,
          x: m.cx + offsetX,
          y: m.cy + offsetY,
          radiusX: m.width / 2,
          radiusY: m.height / 2,
        }),
      )
      break
    case 'triangle':
      layer.add(new Konva.Line({ ...style, points: trianglePoints(m), closed: true }))
      break
    case 'diamond':
      layer.add(new Konva.Line({ ...style, points: diamondPoints(m), closed: true }))
      break
    case 'star': {
      const { inner, outer } = starRadii(m)
      layer.add(
        new Konva.Star({
          ...style,
          x: m.cx + offsetX,
          y: m.cy + offsetY,
          numPoints: 5,
          innerRadius: inner,
          outerRadius: outer,
        }),
      )
      break
    }
  }
}

/*
 * Renders the committed scene into a PNG blob covering only the drawn region.
 * Reads straight from the passed items, so it is decoupled from the live stage
 * and safe to run while the canvas page is unmounting. Returns null when there
 * is nothing to capture.
 */
export async function renderSnapshotBlob(items: SceneItem[]): Promise<Blob | null> {
  const bounds = contentBounds(items)
  if (!bounds) return null

  const worldWidth = bounds.maxX - bounds.minX + PADDING * 2
  const worldHeight = bounds.maxY - bounds.minY + PADDING * 2

  const longestEdge = Math.max(worldWidth, worldHeight)
  const ratio = Math.min(1, MAX_DIMENSION / longestEdge)

  const width = Math.max(MIN_DIMENSION, Math.round(worldWidth * ratio))
  const height = Math.max(MIN_DIMENSION, Math.round(worldHeight * ratio))

  // Detached container: Konva renders to its own canvases, never the document.
  const container = document.createElement('div')
  const stage = new Konva.Stage({ container, width, height })

  // Scale the whole scene down on the layer so exported pixels stay resolution
  // independent regardless of how far the user was zoomed.
  const layer = new Konva.Layer({ listening: false, scaleX: ratio, scaleY: ratio })
  stage.add(layer)

  const offsetX = PADDING - bounds.minX
  const offsetY = PADDING - bounds.minY
  for (const item of items) {
    addItem(layer, item, offsetX, offsetY)
  }

  try {
    const blob = await stage.toBlob({
      x: 0,
      y: 0,
      width,
      height,
      pixelRatio: 1,
      mimeType: 'image/png',
    })
    return (blob as Blob | null) ?? null
  } finally {
    stage.destroy()
  }
}
