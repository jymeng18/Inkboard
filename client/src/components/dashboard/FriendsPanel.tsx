import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { Check, Search, UserPlus, Users, X } from 'lucide-react'

import { Button } from '@/components/ui/button'
import type { Friend, FriendRequest, PresenceStatus } from '../../types/social'

const STATUS_STYLES: Record<PresenceStatus, { dot: string; label: string }> = {
  online: { dot: 'bg-green-500', label: 'Online' },
  'in-canvas': { dot: 'bg-secondary', label: 'In a canvas' },
  offline: { dot: 'bg-surface-dim', label: 'Offline' },
}

interface FriendsPanelProps {
  open: boolean
  onClose: () => void
  friends: Friend[]
  requests: FriendRequest[]
}

export default function FriendsPanel({ open, onClose, friends, requests }: FriendsPanelProps) {
  const [search, setSearch] = useState('')

  useEffect(() => {
    if (!open) return
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [open, onClose])

  const online = friends.filter((f) => f.status !== 'offline')
  const offline = friends.filter((f) => f.status === 'offline')
  const isEmpty = friends.length === 0 && requests.length === 0

  function handleAdd() {
    const id = search.trim()
    if (!id) return
    toast.info('Friend requests are coming soon.')
    setSearch('')
  }

  return (
    <>
      <div
        onClick={onClose}
        aria-hidden
        className={`fixed inset-0 z-40 bg-outline/30 transition-opacity duration-200 ${
          open ? 'opacity-100' : 'pointer-events-none opacity-0'
        }`}
      />

      <aside
        role="dialog"
        aria-label="Friends"
        aria-hidden={!open}
        className={`fixed top-0 right-0 z-50 flex h-full w-80 max-w-[85vw] flex-col border-l-4 border-outline bg-surface transition-transform duration-200 ${
          open ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        <div className="flex items-center justify-between border-b-4 border-outline px-5 py-4">
          <h2 className="font-display text-2xl">Friends</h2>
          <Button variant="ghost" size="icon" onClick={onClose} aria-label="Close friends panel">
            <X />
          </Button>
        </div>

        <div className="border-b-2 border-outline/15 p-4">
          <div className="relative">
            <Search
              className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-on-background/40"
              aria-hidden
            />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              onKeyDown={(event) => event.key === 'Enter' && handleAdd()}
              placeholder="Add by user ID"
              className="w-full rounded-full border-[3px] border-outline bg-background py-2 pr-11 pl-10 font-body text-sm outline-none placeholder:text-on-background/35 focus:border-primary"
            />
            <button
              type="button"
              onClick={handleAdd}
              aria-label="Send friend request"
              className="absolute top-1/2 right-1.5 flex size-8 -translate-y-1/2 items-center justify-center rounded-full bg-primary text-white transition-transform hover:scale-105"
            >
              <UserPlus className="size-4" aria-hidden />
            </button>
          </div>
        </div>

        <div className="flex-1 space-y-6 overflow-y-auto p-4">
          {isEmpty && (
            <div className="mt-10 px-4 text-center">
              <span className="mx-auto mb-4 flex size-14 items-center justify-center rounded-full border-[3px] border-outline bg-secondary-container sticker-shadow-sm">
                <Users className="size-7" aria-hidden />
              </span>
              <p className="font-display text-xl leading-none">No friends yet</p>
              <p className="mt-2 font-body text-xs text-on-background/60">
                Add someone by their user ID to get started once friends go live.
              </p>
            </div>
          )}

          {requests.length > 0 && (
            <Section title={`Requests · ${requests.length}`}>
              {requests.map((request) => (
                <li
                  key={request.id}
                  className="flex items-center gap-3 rounded-xl border-[3px] border-outline bg-primary-container/40 p-2"
                >
                  <Avatar name={request.userName} />
                  <span className="min-w-0 flex-1 truncate font-label text-sm font-bold">
                    {request.userName}
                  </span>
                  <button
                    type="button"
                    aria-label={`Accept ${request.userName}`}
                    className="flex size-8 items-center justify-center rounded-full border-2 border-outline bg-green-400 text-outline hover:scale-105"
                  >
                    <Check className="size-4" aria-hidden />
                  </button>
                  <button
                    type="button"
                    aria-label={`Decline ${request.userName}`}
                    className="flex size-8 items-center justify-center rounded-full border-2 border-outline bg-surface hover:scale-105"
                  >
                    <X className="size-4" aria-hidden />
                  </button>
                </li>
              ))}
            </Section>
          )}

          {!isEmpty && (
            <>
              <Section title={`Online · ${online.length}`}>
                {online.length === 0 ? (
                  <EmptyRow>No friends online.</EmptyRow>
                ) : (
                  online.map((friend) => <FriendRow key={friend.id} friend={friend} />)
                )}
              </Section>

              <Section title={`Offline · ${offline.length}`}>
                {offline.map((friend) => (
                  <FriendRow key={friend.id} friend={friend} />
                ))}
              </Section>
            </>
          )}
        </div>
      </aside>
    </>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <h3 className="mb-2 font-label text-xs font-extrabold tracking-widest text-on-background/50 uppercase">
        {title}
      </h3>
      <ul className="flex flex-col gap-2">{children}</ul>
    </div>
  )
}

function FriendRow({ friend }: { friend: Friend }) {
  const status = STATUS_STYLES[friend.status]
  return (
    <li className="flex items-center gap-3 rounded-xl p-2 hover:bg-background">
      <Avatar name={friend.userName} dimmed={friend.status === 'offline'} />
      <div className="min-w-0 flex-1">
        <p className="truncate font-label text-sm font-bold">{friend.userName}</p>
        <span className="flex items-center gap-1.5 font-body text-xs text-on-background/60">
          <span className={`size-2 rounded-full ${status.dot}`} aria-hidden />
          {status.label}
        </span>
      </div>
    </li>
  )
}

function Avatar({ name, dimmed }: { name: string; dimmed?: boolean }) {
  return (
    <span
      className={`flex size-9 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-secondary-container font-display ${
        dimmed ? 'opacity-50' : ''
      }`}
    >
      {name.charAt(0).toUpperCase()}
    </span>
  )
}

function EmptyRow({ children }: { children: React.ReactNode }) {
  return <li className="px-2 py-1 font-body text-xs text-on-background/50">{children}</li>
}
