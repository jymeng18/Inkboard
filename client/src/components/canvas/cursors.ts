import type { Tool } from '@/stores/canvasUiStore'

const cursorValue = (svg: string, hotspot: number) =>
  `url("data:image/svg+xml,${encodeURIComponent(svg)}") ${hotspot} ${hotspot}, crosshair`

// Browsers ignore custom cursors past ~128px, and a sub-pixel circle vanishes,
// so keep the drawn diameter within these screen-pixel bounds.
const MIN_DIAMETER = 4
const MAX_DIAMETER = 120

/*
 * A circle matching the on-screen stroke width, used as the drawing cursor so the
 * pointer previews how thick the next stroke will be. Pencil/brush leave it
 * hollow with a black dot marking the exact point; the eraser fills it white to
 * read as "removing". `diameter` is in screen pixels (stroke size scaled by zoom).
 */
function circleCursor(diameter: number, fill: string, dot: boolean): string {
  const d = Math.max(MIN_DIAMETER, Math.min(diameter, MAX_DIAMETER))
  const pad = 4
  const dim = d + pad * 2
  const center = dim / 2
  const dotMark = dot ? `<circle cx="${center}" cy="${center}" r="1" fill="black"/>` : ''
  const svg =
    `<svg xmlns="http://www.w3.org/2000/svg" width="${dim}" height="${dim}" viewBox="0 0 ${dim} ${dim}">` +
    `<circle cx="${center}" cy="${center}" r="${d / 2}" fill="${fill}" stroke="black" stroke-width="1.5"/>` +
    dotMark +
    `</svg>`
  return cursorValue(svg, Math.round(center))
}

export function cursorForTool(tool: Tool, diameter: number): string {
  if (tool === 'hand') return 'grab'
  if (tool === 'eraser') return circleCursor(diameter, 'white', false)
  if (tool === 'pencil' || tool === 'brush') return circleCursor(diameter, 'none', true)
  return 'crosshair'
}
