import type { LucideIcon } from 'lucide-react'

interface ToolButtonProps {
  icon: LucideIcon
  label: string
  active?: boolean
  onClick: () => void
}

export default function ToolButton({ icon: Icon, label, active, onClick }: ToolButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={label}
      aria-label={label}
      aria-pressed={active}
      className={`flex size-10 items-center justify-center rounded-full border-[3px] transition-colors ${
        active
          ? 'border-outline bg-primary text-white sticker-shadow-sm'
          : 'border-transparent text-on-background/70 hover:bg-background hover:text-on-background'
      }`}
    >
      <Icon className="size-5" aria-hidden />
    </button>
  )
}
