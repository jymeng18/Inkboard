import Konva from 'konva'

import type { SceneItem } from '@/stores/sceneStore'

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
      const points = item.outline
      for (let i = 0; i + 1 < points.length; i += 2) {
        bounds = expand(bounds, points[i], points[i + 1])
      }
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
        points: item.outline,
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
  } else {
    layer.add(
      new Konva.Rect({
        x: item.x + offsetX,
        y: item.y + offsetY,
        width: item.width,
        height: item.height,
        stroke: item.color,
        strokeWidth: item.strokeWidth,
        lineJoin: 'round',
        listening: false,
        perfectDrawEnabled: false,
      }),
    )
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
