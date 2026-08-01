import { useEffect, useState } from 'react'
import { toast } from 'sonner'

import { statusToast } from '@/lib/notify'
import { Check, Search, UserMinus, UserPlus, Users, X } from 'lucide-react'

import { truncateId, type FriendDto, type FriendRequestDto } from '@/api/friends'
import { extractErrorMessage, isGuid } from '@/api/party'
import { Button } from '@/components/ui/button'
import UserAvatar from '@/components/ui/UserAvatar'
import {
  useFriends,
  usePendingFriendRequests,
  useRespondToFriendRequest,
  useSendFriendRequest,
  useUnfriend,
} from '@/hooks/useFriends'
import { useAuthStore } from '@/stores/authStore'
import { useConnectionStore } from '@/stores/connectionStore'

interface FriendsPanelProps {
  open: boolean
  onClose: () => void
}

export default function FriendsPanel({ open, onClose }: FriendsPanelProps) {
  const [search, setSearch] = useState('')

  const { data: friends = [], refetch: refetchFriends } = useFriends()
  const { data: requests } = usePendingFriendRequests()
  const sendRequest = useSendFriendRequest()
  const currentUserId = useAuthStore((s) => s.userId)

  /*
   * The only presence signal that exists today is the PartyHub feed, which
   * covers party members and nobody else. Anyone we have no word on renders
   * offline rather than optimistically online. A real presence source lands
   * later and slots in right here.
   */
  const presence = useConnectionStore((s) => s.presence)

  useEffect(() => {
    if (!open) return
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [open, onClose])

  /*
   * Nothing tells the sender that their request was accepted: they can't see
   * their own outgoing requests, and the poll only covers incoming ones. So
   * opening the panel is the refresh, one request per open, which is as light
   * as this gets without a socket.
   */
  useEffect(() => {
    if (open) refetchFriends()
  }, [open, refetchFriends])

  const online = friends.filter((friend) => presence[friend.userId])
  const offline = friends.filter((friend) => !presence[friend.userId])
  const isEmpty = friends.length === 0 && requests.length === 0

  function handleAdd() {
    const targetUserId = search.trim()
    if (!targetUserId || sendRequest.isPending) return

    // Same gate the server applies before it will even parse the id.
    if (!isGuid(targetUserId)) {
      toast.error("That doesn't look like a user ID.")
      return
    }

    if (targetUserId === currentUserId) {
      toast.error('You cannot add yourself.')
      return
    }

    sendRequest.mutate(targetUserId, {
      onSuccess: (request) => {
        setSearch('')
        statusToast.success(`Request sent to ${request.userName}`)
      },
      onError: (err) => toast.error(extractErrorMessage(err)),
    })
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
              disabled={sendRequest.isPending}
              aria-label="Send friend request"
              className="absolute top-1/2 right-1.5 flex size-8 -translate-y-1/2 items-center justify-center rounded-full bg-primary text-white transition-transform hover:scale-105 disabled:opacity-50"
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
                Add someone by their user ID to send your first request.
              </p>
            </div>
          )}

          {requests.length > 0 && (
            <Section title="Requests" count={requests.length} tone="bg-primary text-white">
              {requests.map((request) => (
                <RequestRow key={request.id} request={request} />
              ))}
            </Section>
          )}

          {!isEmpty && (
            <>
              <Section title="Online" count={online.length} tone="bg-green-400">
                {online.length === 0 ? (
                  <EmptyRow>No friends online.</EmptyRow>
                ) : (
                  online.map((friend) => (
                    <FriendRow key={friend.userId} friend={friend} online />
                  ))
                )}
              </Section>

              <Section title="Offline" count={offline.length} tone="bg-surface-dim">
                {offline.map((friend) => (
                  <FriendRow key={friend.userId} friend={friend} online={false} />
                ))}
              </Section>
            </>
          )}
        </div>
      </aside>
    </>
  )
}

function Section({
  title,
  count,
  tone,
  children,
}: {
  title: string
  count: number
  tone: string
  children: React.ReactNode
}) {
  return (
    <div>
      <h3 className="mb-2 flex items-center gap-2 font-label text-xs font-extrabold tracking-widest text-on-background/60 uppercase">
        {title}
        <span
          className={`rounded-full border-2 border-outline px-2 py-px font-label text-[10px] leading-tight ${tone}`}
        >
          {count}
        </span>
      </h3>
      <ul className="flex flex-col gap-2">{children}</ul>
    </div>
  )
}

function RequestRow({ request }: { request: FriendRequestDto }) {
  const respond = useRespondToFriendRequest()

  function handleRespond(accepted: boolean) {
    if (respond.isPending) return

    respond.mutate({
      requestId: request.id,
      requesterId: request.userId,
      userName: request.userName,
      accepted,
    })
  }

  return (
    <li className="flex items-center gap-3 rounded-xl border-[3px] border-outline bg-primary-container p-2 sticker-shadow-sm">
      <UserAvatar name={request.userName} userId={request.userId} />
      <div className="min-w-0 flex-1">
        <p className="truncate font-label text-sm font-bold">{request.userName}</p>
        <p className="truncate font-body text-[11px] text-on-background/60">
          {truncateId(request.userId)}
        </p>
      </div>
      <button
        type="button"
        disabled={respond.isPending}
        onClick={() => handleRespond(true)}
        aria-label={`Accept ${request.userName}`}
        className="flex size-8 items-center justify-center rounded-full border-2 border-outline bg-green-400 text-outline hover:scale-105 disabled:opacity-50"
      >
        <Check className="size-4" aria-hidden />
      </button>
      <button
        type="button"
        disabled={respond.isPending}
        onClick={() => handleRespond(false)}
        aria-label={`Decline ${request.userName}`}
        className="flex size-8 items-center justify-center rounded-full border-2 border-outline bg-surface hover:scale-105 disabled:opacity-50"
      >
        <X className="size-4" aria-hidden />
      </button>
    </li>
  )
}

function FriendRow({ friend, online }: { friend: FriendDto; online: boolean }) {
  // Removing a friend is one click away from irreversible, so the row asks
  // first rather than reaching for a modal.
  const [confirming, setConfirming] = useState(false)
  const removeFriend = useUnfriend()

  function handleRemove() {
    removeFriend.mutate(friend.userId, {
      onSuccess: () => statusToast.message(`Removed ${friend.userName}`),
      onError: (err) => {
        setConfirming(false)
        toast.error(extractErrorMessage(err))
      },
    })
  }

  return (
    <li
      className={`group flex items-center gap-3 rounded-xl border-[3px] p-2 transition-transform ${
        online
          ? 'border-outline bg-surface sticker-shadow-sm hover:-translate-y-0.5'
          : 'border-outline/25 bg-surface/50'
      }`}
    >
      <span className="relative shrink-0">
        <UserAvatar name={friend.userName} userId={friend.userId} dimmed={!online} />
        <span
          aria-hidden
          className={`absolute -right-0.5 -bottom-0.5 size-3 rounded-full border-2 border-surface ${
            online ? 'bg-green-500' : 'bg-surface-dim'
          }`}
        />
      </span>

      <div className="min-w-0 flex-1">
        <p
          className={`truncate font-label text-sm font-bold ${online ? '' : 'text-on-background/60'}`}
        >
          {friend.userName}
        </p>
        <p className="truncate font-body text-[11px] text-on-background/45">
          {truncateId(friend.userId)}
        </p>
        <span className="font-body text-xs text-on-background/60">
          {online ? 'Online' : 'Offline'}
        </span>
      </div>

      {confirming ? (
        <div className="flex shrink-0 items-center gap-1">
          <button
            type="button"
            disabled={removeFriend.isPending}
            onClick={handleRemove}
            aria-label={`Confirm removing ${friend.userName}`}
            className="flex size-8 items-center justify-center rounded-full border-2 border-outline bg-primary text-white hover:scale-105 disabled:opacity-50"
          >
            <Check className="size-4" aria-hidden />
          </button>
          <button
            type="button"
            onClick={() => setConfirming(false)}
            aria-label="Keep friend"
            className="flex size-8 items-center justify-center rounded-full border-2 border-outline bg-surface hover:scale-105"
          >
            <X className="size-4" aria-hidden />
          </button>
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setConfirming(true)}
          aria-label={`Remove ${friend.userName}`}
          className="flex size-8 shrink-0 items-center justify-center rounded-full text-on-background/40 opacity-0 transition-opacity hover:text-primary focus-visible:opacity-100 group-hover:opacity-100"
        >
          <UserMinus className="size-4" aria-hidden />
        </button>
      )}
    </li>
  )
}

function EmptyRow({ children }: { children: React.ReactNode }) {
  return <li className="px-2 py-1 font-body text-xs text-on-background/50">{children}</li>
}
