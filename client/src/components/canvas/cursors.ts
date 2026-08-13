import type { Tool } from '@/stores/canvasUiStore'

const cursorValue = (svg: string, hotspot: number) =>
  `url("data:image/svg+xml,${encodeURIComponent(svg)}") ${hotspot} ${hotspot}, crosshair`

/*
 * A circle the width of the current stroke, used as the drawing cursor so the
 * pointer previews how thick the next stroke will be. Pencil/brush leave it
 * hollow with a black dot marking the exact point; the eraser fills it white to
 * read as "removing". `size` is the stroke width, matching the size slider.
 */
function circleCursor(size: number, fill: string, dot: boolean): string {
  const pad = 4
  const dim = size + pad * 2
  const center = dim / 2
  const dotMark = dot ? `<circle cx="${center}" cy="${center}" r="1" fill="black"/>` : ''
  const svg =
    `<svg xmlns="http://www.w3.org/2000/svg" width="${dim}" height="${dim}" viewBox="0 0 ${dim} ${dim}">` +
    `<circle cx="${center}" cy="${center}" r="${size / 2}" fill="${fill}" stroke="black" stroke-width="1.5"/>` +
    dotMark +
    `</svg>`
  return cursorValue(svg, Math.round(center))
}

export function cursorForTool(tool: Tool, size: number): string {
  if (tool === 'hand') return 'grab'
  if (tool === 'eraser') return circleCursor(size, 'white', false)
  if (tool === 'pencil' || tool === 'brush') return circleCursor(size, 'none', true)
  return 'crosshair'
}
