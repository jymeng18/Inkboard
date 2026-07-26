import { Link } from 'react-router-dom'
import { toast } from 'sonner'
import {
  Brush,
  Eraser,
  Hand,
  History,
  Pencil,
  Redo2,
  Share2,
  Shapes,
  Undo2,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/authStore'
import { useCanvasUiStore, type Tool } from '@/stores/canvasUiStore'
import { useSceneStore } from '@/stores/sceneStore'
import ColorPicker from './ColorPicker'
import ToolButton from './ToolButton'

const TOOLS: { tool: Tool; icon: LucideIcon; label: string }[] = [
  { tool: 'pencil', icon: Pencil, label: 'Pencil (P)' },
  { tool: 'brush', icon: Brush, label: 'Brush (B)' },
  { tool: 'eraser', icon: Eraser, label: 'Eraser (E)' },
  { tool: 'shapes', icon: Shapes, label: 'Shapes (U)' },
  { tool: 'hand', icon: Hand, label: 'Hand (H)' },
]

const soon = () => toast.info('This action is coming soon.')

export default function CanvasToolbar() {
  const tool = useCanvasUiStore((s) => s.tool)
  const setTool = useCanvasUiStore((s) => s.setTool)
  const undo = useSceneStore((s) => s.undo)
  const redo = useSceneStore((s) => s.redo)
  const canUndo = useSceneStore((s) => s.items.length > 0)
  const canRedo = useSceneStore((s) => s.redoStack.length > 0)
  const userName = useAuthStore((s) => s.userName)
  const initial = userName.trim().charAt(0).toUpperCase() || '?'

  return (
    <div className="pointer-events-none absolute inset-x-0 top-0 z-20 flex items-start justify-between gap-3 p-4">
      <Link
        to="/dashboard"
        className="pointer-events-auto flex items-center gap-2 rounded-full border-[3px] border-outline bg-surface px-4 py-2 sticker-shadow-sm transition-transform hover:-translate-y-0.5"
        title="Back to dashboard"
      >
        <img src="/pen.svg" alt="" className="h-6 -rotate-30" />
        <span className="hidden font-display text-xl sm:inline">Inkboard</span>
      </Link>

      <div className="pointer-events-auto flex items-center gap-2 rounded-full border-[3px] border-outline bg-surface px-3 py-2 sticker-shadow">
        <div className="flex items-center gap-1">
          {TOOLS.map(({ tool: value, icon, label }) => (
            <ToolButton
              key={value}
              icon={icon}
              label={label}
              active={tool === value}
              onClick={() => setTool(value)}
            />
          ))}
        </div>

        <span className="mx-1 h-7 w-0.5 rounded bg-outline/15" aria-hidden />

        <ColorPicker />
      </div>

      <div className="pointer-events-auto flex items-center gap-2">
        <div className="flex items-center gap-1 rounded-full border-[3px] border-outline bg-surface px-2 py-1.5 sticker-shadow-sm">
          <IconButton icon={Undo2} label="Undo (Ctrl+Z)" onClick={undo} disabled={!canUndo} />
          <IconButton icon={Redo2} label="Redo (Ctrl+Shift+Z)" onClick={redo} disabled={!canRedo} />
          <IconButton icon={History} label="Version history" onClick={soon} />
        </div>

        <Button size="sm" onClick={soon}>
          <Share2 />
          <span className="hidden sm:inline">Export</span>
        </Button>

        <span
          className="flex size-10 items-center justify-center rounded-full border-[3px] border-outline bg-secondary-container font-display text-lg sticker-shadow-sm"
          title={userName || 'You'}
        >
          {initial}
        </span>
      </div>
    </div>
  )
}

function IconButton({
  icon: Icon,
  label,
  onClick,
  disabled,
}: {
  icon: LucideIcon
  label: string
  onClick: () => void
  disabled?: boolean
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      title={label}
      aria-label={label}
      className="flex size-8 items-center justify-center rounded-full text-on-background/70 transition-colors hover:bg-background hover:text-on-background disabled:pointer-events-none disabled:opacity-30"
    >
      <Icon className="size-4" aria-hidden />
    </button>
  )
}
