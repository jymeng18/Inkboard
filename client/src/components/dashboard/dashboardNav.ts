import type { LucideIcon } from 'lucide-react'
import { Inbox, Palette, Settings, Users } from 'lucide-react'

export type DashboardView = 'canvases' | 'inbox' | 'party' | 'settings'

/*
 * One source of truth for the dashboard's sections, shared by the desktop
 * sidebar and the mobile tab strip so the two can't drift apart. Lives outside
 * the component files to keep fast refresh working on them.
 */
export const NAV_ITEMS: { view: DashboardView; label: string; icon: LucideIcon }[] = [
  { view: 'canvases', label: 'Canvases', icon: Palette },
  { view: 'inbox', label: 'Inbox', icon: Inbox },
  { view: 'party', label: 'Party', icon: Users },
  { view: 'settings', label: 'Settings', icon: Settings },
]
