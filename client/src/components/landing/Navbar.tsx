import { SquarePen } from 'lucide-react'
import { Button } from '@/components/ui/button'

const NAV_LINKS = [
  { label: 'Features', href: '#features' },
  { label: 'How it Works', href: '#canvas' },
  { label: 'AI Magic', href: '#features' },
]

interface NavbarProps {
  onStart: () => void
}

export default function Navbar({ onStart }: NavbarProps) {
  return (
    <header className="mx-auto max-w-3xl px-6 pt-7">
      <nav className="flex items-center justify-between gap-4 rounded-full border-[3px] border-outline bg-surface px-5 py-2.5 sticker-shadow-sm sm:px-6">
        <a href="#top" className="flex items-center gap-2">
          <SquarePen className="size-6 text-primary" aria-hidden />
          <span className="font-display text-2xl tracking-tight">Inkboard</span>
        </a>

        <div className="hidden gap-7 font-label text-sm font-bold md:flex">
          {NAV_LINKS.map((link) => (
            <a key={link.label} href={link.href} className="transition-colors hover:text-primary">
              {link.label}
            </a>
          ))}
        </div>

        <Button size="sm" onClick={onStart}>
          Start Drawing →
        </Button>
      </nav>
    </header>
  )
}
