import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { BookUser, ChevronRight, Copy, UserMinus, UserPlus } from 'lucide-react'

import { truncateId } from '@/api/friends'
import { extractErrorMessage } from '@/api/party'
import UserAvatar from '@/components/ui/UserAvatar'
import { useFriends, useUnfriend } from '@/hooks/useFriends'
import type { useCanvasParty } from '@/hooks/useCanvasParty'
import { useCanvasUiStore } from '@/stores/canvasUiStore'
import { useConnectionStore } from '@/stores/connectionStore'
import PanelMenuItem from './PanelMenuItem'

type Party = ReturnType<typeof useCanvasParty>

export default function CanvasFriendsPanel({ party }: { party: Party }) {
  const open = useCanvasUiStore((s) => s.friendsOpen)
  const partyOpen = useCanvasUiStore((s) => s.panelOpen)
  const toggleFriends = useCanvasUiStore((s) => s.toggleFriends)

  const { data: friends = [], refetch: refetchFriends } = useFriends()
  const removeFriend = useUnfriend()
  const presence = useConnectionStore((s) => s.presence)

  const [openMenuId, setOpenMenuId] = useState<string | null>(null)

  /*
   * Same refresh point as the dashboard panel: nothing pushes "your request was
   * accepted" to the sender, so opening the list is when it re-reads.
   */
  useEffect(() => {
    if (open) refetchFriends()
  }, [open, refetchFriends])

  // Collapsing shouldn't leave a menu hanging open behind the sheet, or waiting
  // to reappear on the next open.
  function togglePanel() {
    setOpenMenuId(null)
    toggleFriends()
  }

  const onlineCount = friends.filter((friend) => presence[friend.userId]).length

  async function inviteToParty(userId: string, userName: string) {
    setOpenMenuId(null)
    try {
      await party.invite(userId)
      toast.success(`Invited ${userName}`)
    } catch (err) {
      toast.error(extractErrorMessage(err))
    }
  }

  async function copyId(userId: string) {
    setOpenMenuId(null)
    try {
      await navigator.clipboard.writeText(userId)
      toast.success('User ID copied')
    } catch {
      // Clipboard access needs a secure context, and it isn't worth failing silently.
      toast.error("Couldn't copy — copy it from the dashboard instead.")
    }
  }

  function unfriend(userId: string, userName: string) {
    setOpenMenuId(null)
    removeFriend.mutate(userId, {
      onSuccess: () => toast(`Removed ${userName}`),
      onError: (err) => toast.error(extractErrorMessage(err)),
    })
  }

  return (
    <>
      {/*
       * Sits directly under the party lobby pill in the collapsed rail. Both
       * pills hide while a panel is docked, since the sheet covers that corner.
       */}
      {!open && !partyOpen && (
        <button
          type="button"
          onClick={togglePanel}
          aria-label="Open friends list"
          className="absolute top-40 right-4 z-20 flex items-center gap-2 rounded-full border-[3px] border-outline bg-surface px-4 py-2.5 font-label text-sm font-bold sticker-shadow-sm transition-transform hover:-translate-y-0.5"
        >
          <BookUser className="size-5" aria-hidden />
          {friends.length}
        </button>
      )}

      <aside
        aria-label="Friends"
        className={`absolute top-4 right-4 bottom-4 z-20 flex w-80 flex-col rounded-3xl border-4 border-outline bg-surface p-5 sticker-shadow-lg transition-transform duration-200 ${
          open ? 'translate-x-0' : 'translate-x-[calc(100%+1.5rem)]'
        }`}
      >
        <div className="mb-5 flex items-start justify-between">
          <div>
            <h2 className="font-display text-3xl leading-none">Friends</h2>
            <p className="mt-1 flex items-center gap-1.5 font-body text-xs text-on-background/70">
              <span className="size-2 rounded-full bg-green-500" aria-hidden />
              {onlineCount} online · {friends.length} total
            </p>
          </div>
          <button
            type="button"
            onClick={togglePanel}
            aria-label="Collapse friends panel"
            className="flex size-8 items-center justify-center rounded-full text-on-background/60 hover:bg-background hover:text-on-background"
          >
            <ChevronRight className="size-5" aria-hidden />
          </button>
        </div>

        {friends.length === 0 ? (
          <div className="flex flex-1 flex-col items-center justify-center px-4 text-center">
            <span className="mb-4 flex size-14 items-center justify-center rounded-full border-[3px] border-outline bg-secondary-container sticker-shadow-sm">
              <BookUser className="size-7" aria-hidden />
            </span>
            <p className="font-display text-xl leading-none">No friends yet</p>
            <p className="mt-2 font-body text-xs text-on-background/60">
              Add someone from the dashboard to invite them straight from here.
            </p>
          </div>
        ) : (
          <ul className="flex-1 space-y-2 overflow-y-auto">
            {friends.map((friend) => {
              const online = presence[friend.userId] ?? false
              const menuOpen = open && openMenuId === friend.userId
              const inParty = party.members.some((m) => m.userId === friend.userId)

              return (
                <li key={friend.userId} className="relative">
                  <button
                    type="button"
                    onClick={() => setOpenMenuId(menuOpen ? null : friend.userId)}
                    aria-expanded={menuOpen}
                    className={`flex w-full items-center gap-3 rounded-2xl border-[3px] p-2.5 text-left transition-colors ${
                      menuOpen
                        ? 'border-outline bg-background sticker-shadow-sm'
                        : 'border-transparent hover:border-outline/30 hover:bg-background'
                    }`}
                  >
                    <span className="relative shrink-0">
                      <UserAvatar
                        name={friend.userName}
                        userId={friend.userId}
                        size="size-10"
                        dimmed={!online}
                      />
                      <span
                        aria-hidden
                        className={`absolute -right-0.5 -bottom-0.5 size-3 rounded-full border-2 border-surface ${
                          online ? 'bg-green-500' : 'bg-surface-dim'
                        }`}
                      />
                    </span>

                    <div className="min-w-0 flex-1">
                      <p className="truncate font-label text-sm font-bold">{friend.userName}</p>
                      <span className="flex items-center gap-1 font-body text-[11px] text-on-background/60">
                        {truncateId(friend.userId)} · {online ? 'Online' : 'Offline'}
                      </span>
                    </div>
                  </button>

                  {menuOpen && (
                    <>
                      <div
                        className="fixed inset-0 z-40"
                        onClick={() => setOpenMenuId(null)}
                        aria-hidden
                      />
                      <div className="absolute top-full right-0 z-50 mt-1 w-48 overflow-hidden rounded-2xl border-[3px] border-outline bg-surface py-1 sticker-shadow">
                        {!inParty && (
                          <PanelMenuItem
                            icon={UserPlus}
                            label="Invite to party"
                            onClick={() => inviteToParty(friend.userId, friend.userName)}
                          />
                        )}
                        <PanelMenuItem
                          icon={Copy}
                          label="Copy ID"
                          onClick={() => copyId(friend.userId)}
                        />
                        <PanelMenuItem
                          icon={UserMinus}
                          label="Unfriend"
                          destructive
                          onClick={() => unfriend(friend.userId, friend.userName)}
                        />
                      </div>
                    </>
                  )}
                </li>
              )
            })}
          </ul>
        )}
      </aside>
    </>
  )
}
