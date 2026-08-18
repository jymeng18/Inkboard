import { useEffect, useRef, useState } from 'react'
import { HexColorPicker } from 'react-colorful'

import { useCanvasUiStore } from '@/stores/canvasUiStore'

const SWATCHES = ['#2d2926', '#ff7070', '#ff9f43', '#ffd23f', '#7bc86c', '#00c1fd', '#a06cd5']

export default function ColorPicker() {
  const color = useCanvasUiStore((s) => s.color)
  const setColor = useCanvasUiStore((s) => s.setColor)
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: MouseEvent) => {
      if (ref.current && !ref.current.contains(event.target as Node)) setOpen(false)
    }
    const onKeyDown = (event: KeyboardEvent) => event.key === 'Escape' && setOpen(false)
    window.addEventListener('mousedown', onPointerDown)
    window.addEventListener('keydown', onKeyDown)
    return () => {
      window.removeEventListener('mousedown', onPointerDown)
      window.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  return (
    <div ref={ref} className="flex items-center gap-1.5">
      <div
        className="size-8 rounded-lg border-[3px] border-outline"
        style={{ backgroundColor: color }}
        title={`Selected: ${color}`}
        aria-label={`Selected color ${color}`}
      />
      <div className="hidden items-center gap-1.5 sm:flex">
        <span className="mx-0.5 h-6 w-0.5 rounded bg-outline/15" aria-hidden />

        {SWATCHES.map((swatch) => (
          <button
            key={swatch}
            type="button"
            onClick={() => setColor(swatch)}
            title={swatch}
            aria-label={`Use ${swatch}`}
            className={`size-6 rounded-full border-2 transition-transform hover:scale-110 ${
              color.toLowerCase() === swatch.toLowerCase()
                ? 'border-outline ring-2 ring-outline ring-offset-1'
                : 'border-outline/40'
            }`}
            style={{ backgroundColor: swatch }}
          />
        ))}
      </div>

      <span className="mx-0.5 hidden h-6 w-0.5 rounded bg-outline/15 sm:block" aria-hidden />

      <div className="relative flex items-center">
        <button
          type="button"
          onClick={() => setOpen((prev) => !prev)}
          aria-label="Custom color"
          aria-expanded={open}
          title="Custom color"
          className="size-8 rounded-full border-2 border-outline transition-transform hover:scale-110"
          style={{
            background:
              'radial-gradient(circle, #fff 0%, rgba(255,255,255,0) 58%), conic-gradient(from 90deg, red, orange, yellow, lime, cyan, blue, magenta, red)',
          }}
        />

        {open && (
          <div className="absolute top-11 left-1/2 z-50 -translate-x-1/2 rounded-2xl border-[3px] border-outline bg-surface p-3 sticker-shadow">
            <HexColorPicker color={color} onChange={setColor} />
            <div className="mt-2 flex items-center gap-2">
              <span className="font-label text-xs font-bold">HEX</span>
              <input
                value={color}
                onChange={(event) => setColor(event.target.value)}
                className="w-24 rounded-full border-2 border-outline bg-background px-2 py-1 font-label text-xs outline-none focus:border-primary"
                aria-label="Hex color value"
              />
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
