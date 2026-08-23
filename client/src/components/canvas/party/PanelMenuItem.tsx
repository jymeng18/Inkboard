import type { LucideIcon } from 'lucide-react'

/* One row of a canvas side-panel dropdown, shared by the party and friends panels. */
export default function PanelMenuItem({
  icon: Icon,
  label,
  onClick,
  destructive,
}: {
  icon: LucideIcon
  label: string
  onClick: () => void
  destructive?: boolean
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex w-full items-center gap-2.5 px-3 py-2 text-left font-label text-sm font-bold transition-colors hover:bg-background ${
        destructive ? 'text-primary' : 'text-on-background'
      }`}
    >
      <Icon className="size-4" aria-hidden />
      {label}
    </button>
  )
}
