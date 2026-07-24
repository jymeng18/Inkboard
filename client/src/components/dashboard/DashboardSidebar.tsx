import type { LucideIcon } from 'lucide-react'
import { HelpCircle, Palette, Settings, Shield, Users } from 'lucide-react'

export type DashboardView = 'canvases' | 'party' | 'settings'

const NAV_ITEMS: { view: DashboardView; label: string; icon: LucideIcon }[] = [
  { view: 'canvases', label: 'Canvases', icon: Palette },
  { view: 'party', label: 'Party', icon: Users },
  { view: 'settings', label: 'Settings', icon: Settings },
]

const MAX_PARTY_SIZE = 5

interface DashboardSidebarProps {
  active: DashboardView
  onSelect: (view: DashboardView) => void
  partySize: number
}

export default function DashboardSidebar({ active, onSelect, partySize }: DashboardSidebarProps) {
  const inParty = partySize > 0

  return (
    <aside className="hidden w-64 shrink-0 flex-col border-r-4 border-outline bg-surface p-4 lg:flex">
      <nav className="flex flex-col gap-2">
        {NAV_ITEMS.map(({ view, label, icon: Icon }) => {
          const isActive = view === active
          return (
            <button
              key={view}
              type="button"
              onClick={() => onSelect(view)}
              aria-current={isActive ? 'page' : undefined}
              className={`flex items-center gap-3 rounded-full border-[3px] px-4 py-2.5 font-label text-sm font-bold transition-colors ${
                isActive
                  ? 'border-outline bg-primary text-white sticker-shadow-sm'
                  : 'border-transparent text-on-background/70 hover:bg-background hover:text-on-background'
              }`}
            >
              <Icon className="size-5" aria-hidden />
              {label}
            </button>
          )
        })}
      </nav>

      <button
        type="button"
        onClick={() => onSelect('party')}
        className="mt-auto rounded-2xl border-[3px] border-outline bg-primary-container p-4 text-left canvas-bg sticker-shadow-sm transition-transform hover:-translate-y-0.5"
      >
        <div className="flex items-center gap-2">
          <span
            className={`size-2.5 rounded-full border border-outline ${inParty ? 'bg-secondary' : 'bg-surface-dim'}`}
            aria-hidden
          />
          <span className="font-label text-xs font-extrabold tracking-widest uppercase">Party</span>
        </div>
        <p className="mt-1 font-display text-2xl leading-none">
          {inParty ? `${partySize} / ${MAX_PARTY_SIZE}` : 'Empty'}
        </p>
        <p className="mt-1 font-body text-xs text-on-background/70">
          {inParty ? 'active now' : 'no active party'}
        </p>
      </button>

      <div className="mt-4 flex items-center justify-around border-t-2 border-outline/15 pt-4">
        <a
          href="#"
          className="flex items-center gap-1.5 font-label text-xs font-bold text-on-background/60 hover:text-on-background"
        >
          <HelpCircle className="size-4" aria-hidden />
          Help
        </a>
        <a
          href="#"
          className="flex items-center gap-1.5 font-label text-xs font-bold text-on-background/60 hover:text-on-background"
        >
          <Shield className="size-4" aria-hidden />
          Privacy
        </a>
      </div>
    </aside>
  )
}
