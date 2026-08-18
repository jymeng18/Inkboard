import { DropdownMenu } from 'radix-ui'
import {
  Brush,
  Check,
  ChevronDown,
  Circle,
  Diamond,
  Feather,
  Highlighter,
  Square,
  Star,
  Triangle,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

import { useCanvasUiStore, type BrushVariant, type ShapeKind } from '@/stores/canvasUiStore'

interface Option<T extends string> {
  id: T
  icon: LucideIcon
  label: string
}

const BRUSH_OPTIONS: Option<BrushVariant>[] = [
  { id: 'brush', icon: Brush, label: 'Brush' },
  { id: 'marker', icon: Highlighter, label: 'Marker' },
  { id: 'calligraphy', icon: Feather, label: 'Calligraphy' },
]

const SHAPE_OPTIONS: Option<ShapeKind>[] = [
  { id: 'rectangle', icon: Square, label: 'Rectangle' },
  { id: 'ellipse', icon: Circle, label: 'Ellipse' },
  { id: 'triangle', icon: Triangle, label: 'Triangle' },
  { id: 'diamond', icon: Diamond, label: 'Diamond' },
  { id: 'star', icon: Star, label: 'Star' },
]

/*
 * The contextual variant picker in the toolbar: a dropdown for whichever active
 * tool has variants (brush styles, or the shape to draw). Renders nothing for
 * tools that don't, so it simply appears beside the dock when relevant.
 */
export default function ToolOptions() {
  const tool = useCanvasUiStore((s) => s.tool)
  const brushVariant = useCanvasUiStore((s) => s.brushVariant)
  const shapeKind = useCanvasUiStore((s) => s.shapeKind)
  const setBrushVariant = useCanvasUiStore((s) => s.setBrushVariant)
  const setShapeKind = useCanvasUiStore((s) => s.setShapeKind)

  if (tool === 'brush') {
    return (
      <VariantDropdown
        ariaLabel="Brush style"
        options={BRUSH_OPTIONS}
        value={brushVariant}
        onChange={setBrushVariant}
      />
    )
  }

  if (tool === 'shapes') {
    return (
      <VariantDropdown
        ariaLabel="Shape"
        options={SHAPE_OPTIONS}
        value={shapeKind}
        onChange={setShapeKind}
      />
    )
  }

  return null
}

function VariantDropdown<T extends string>({
  ariaLabel,
  options,
  value,
  onChange,
}: {
  ariaLabel: string
  options: Option<T>[]
  value: T
  onChange: (id: T) => void
}) {
  const active = options.find((o) => o.id === value) ?? options[0]
  const ActiveIcon = active.icon

  return (
    <>
      <span className="mx-1 h-7 w-0.5 rounded bg-outline/15" aria-hidden />

      <DropdownMenu.Root>
        <DropdownMenu.Trigger asChild>
          <button
            type="button"
            aria-label={`${ariaLabel}: ${active.label}`}
            title={`${ariaLabel}: ${active.label}`}
            className="flex items-center gap-1 rounded-full border-2 border-outline/40 px-2 py-1.5 font-label text-sm font-bold text-on-background transition-colors hover:border-outline hover:bg-background data-[state=open]:border-outline data-[state=open]:bg-background"
          >
            <ActiveIcon className="size-5" aria-hidden />
            <ChevronDown className="size-3.5 opacity-60" aria-hidden />
          </button>
        </DropdownMenu.Trigger>

        <DropdownMenu.Portal>
          <DropdownMenu.Content
            sideOffset={10}
            align="center"
            className="z-50 flex min-w-40 flex-col gap-1 rounded-2xl border-[3px] border-outline bg-surface p-1.5 font-label text-sm font-bold sticker-shadow"
          >
            {options.map(({ id, icon: Icon, label }) => (
              <DropdownMenu.Item
                key={id}
                onSelect={() => onChange(id)}
                className={`flex cursor-pointer items-center gap-2.5 rounded-full px-3 py-2 outline-none transition-colors ${
                  id === value
                    ? 'bg-primary text-white'
                    : 'text-on-background/80 hover:bg-background focus:bg-background'
                }`}
              >
                <Icon className="size-4" aria-hidden />
                <span className="flex-1">{label}</span>
                {id === value && <Check className="size-4" aria-hidden />}
              </DropdownMenu.Item>
            ))}
          </DropdownMenu.Content>
        </DropdownMenu.Portal>
      </DropdownMenu.Root>
    </>
  )
}
