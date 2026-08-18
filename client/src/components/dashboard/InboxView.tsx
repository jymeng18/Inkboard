import { useState } from 'react'
import { Check, Inbox, RotateCw, X } from 'lucide-react'

import { RequestStatus, truncateId, type FriendRequestDto } from '@/api/friends'
import { Button } from '@/components/ui/button'
import UserAvatar from '@/components/ui/UserAvatar'
import {
  useAnsweredFriendRequests,
  usePendingFriendRequests,
  useRespondToFriendRequest,
} from '@/hooks/useFriends'
import { usePartyInvites, useRespondToInvite } from '@/hooks/usePartyInvites'
import { formatClockTime, isFuture, timeAgo } from '@/lib/datetime'

type InboxTab = 'requests' | 'invites' | 'history'

const STATUS_CHIPS: Record<number, { label: string; className: string }> = {
  [RequestStatus.Accepted]: { label: 'Accepted', className: 'bg-green-400' },
  [RequestStatus.Declined]: { label: 'Declined', className: 'bg-surface-dim' },
  [RequestStatus.Revoked]: { label: 'Withdrawn', className: 'bg-surface-dim' },
}

export default function InboxView() {
  const [tab, setTab] = useState<InboxTab>('requests')

  const { data: pending, isLoading, isError, refetch } = usePendingFriendRequests()
  const { data: answered } = useAnsweredFriendRequests()

  /*
   * One mutation for the whole list rather than one per row: `variables` tells
   * us which row is mid-flight, so only that row disables.
   */
  const respond = useRespondToFriendRequest()

  const {
    data: invites,
    isLoading: invitesLoading,
    isError: invitesError,
    refetch: refetchInvites,
  } = usePartyInvites()
  const respondInvite = useRespondToInvite()

  /* Expired invites drop out here: the server filters by status, not by time. */
  const pendingInvites = invites.filter((invite) => isFuture(invite.expiresAt))

  function handleRespond(request: FriendRequestDto, accepted: boolean) {
    if (respond.isPending) return

    respond.mutate({
      requestId: request.id,
      requesterId: request.userId,
      userName: request.userName,
      accepted,
    })
  }

  return (
    <section>
      <div className="mb-6">
        <h1 className="font-display text-3xl leading-none sm:text-4xl">Inbox</h1>
        <p className="mt-1 font-body text-sm text-on-background/70">
          Friend requests waiting on you, and everything you've already answered.
        </p>
      </div>

      <div className="mb-5 flex gap-2 overflow-x-auto pb-1.5">
        <TabButton
          active={tab === 'requests'}
          count={pending.length}
          onClick={() => setTab('requests')}
        >
          Requests
        </TabButton>
        <TabButton
          active={tab === 'invites'}
          count={pendingInvites.length}
          onClick={() => setTab('invites')}
        >
          Party invites
        </TabButton>
        <TabButton
          active={tab === 'history'}
          count={answered.length}
          onClick={() => setTab('history')}
        >
          History
        </TabButton>
      </div>

      {tab !== 'invites' && isError && (
        <div className="flex flex-col items-center gap-3 py-8 text-center">
          <p className="font-body text-sm text-on-background/70">Couldn't load your inbox.</p>
          <Button variant="surface" size="sm" onClick={() => refetch()}>
            <RotateCw />
            Try again
          </Button>
        </div>
      )}

      {tab !== 'invites' && isLoading && !isError && (
        <ul className="grid gap-4 md:grid-cols-2 2xl:grid-cols-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <RequestSkeleton key={i} />
          ))}
        </ul>
      )}

      {!isLoading && !isError && tab === 'requests' && (
        pending.length === 0 ? (
          <EmptyState
            title="No pending requests"
            hint="When someone adds you by user ID, their request lands here."
          />
        ) : (
          <ul className="grid gap-4 md:grid-cols-2 2xl:grid-cols-3">
            {pending.map((request) => (
              <li
                key={request.id}
                className="flex items-center gap-4 rounded-2xl border-[3px] border-outline bg-surface p-3 sticker-shadow-sm"
              >
                <UserAvatar name={request.userName} userId={request.userId} size="size-11" />

                <div className="min-w-0 flex-1">
                  <p className="truncate font-label font-bold">{request.userName}</p>
                  <p className="truncate font-body text-xs text-on-background/50">
                    {truncateId(request.userId)} · {timeAgo(request.createdAt)}
                  </p>
                </div>

                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    size="sm"
                    disabled={respond.isPending}
                    onClick={() => handleRespond(request, true)}
                    aria-label={`Accept ${request.userName}`}
                  >
                    <Check />
                    <span className="hidden lg:inline">Accept</span>
                  </Button>
                  <Button
                    variant="surface"
                    size="icon"
                    disabled={respond.isPending}
                    onClick={() => handleRespond(request, false)}
                    aria-label={`Decline ${request.userName}`}
                  >
                    <X />
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        )
      )}

      {!isLoading && !isError && tab === 'history' && (
        answered.length === 0 ? (
          <EmptyState
            title="Nothing answered yet"
            hint="Requests you accept or decline are kept here."
          />
        ) : (
          <ul className="grid gap-4 md:grid-cols-2 2xl:grid-cols-3">
            {answered.map((request) => {
              const chip = STATUS_CHIPS[request.status]
              return (
                <li
                  key={request.id}
                  className="flex items-center gap-4 rounded-2xl border-[3px] border-outline/25 bg-surface/50 p-3"
                >
                  <UserAvatar
                    name={request.userName}
                    userId={request.userId}
                    size="size-11"
                    dimmed
                  />

                  <div className="min-w-0 flex-1">
                    <p className="truncate font-label font-bold text-on-background/70">
                      {request.userName}
                    </p>
                    <p className="truncate font-body text-xs text-on-background/40">
                      {truncateId(request.userId)} · {timeAgo(request.createdAt)}
                    </p>
                  </div>

                  {chip && (
                    <span
                      className={`shrink-0 rounded-full border-2 border-outline/40 px-3 py-1 font-label text-xs font-bold ${chip.className}`}
                    >
                      {chip.label}
                    </span>
                  )}
                </li>
              )
            })}
          </ul>
        )
      )}

      {tab === 'invites' && invitesError && (
        <div className="flex flex-col items-center gap-3 py-8 text-center">
          <p className="font-body text-sm text-on-background/70">
            Couldn't load your party invites.
          </p>
          <Button variant="surface" size="sm" onClick={() => refetchInvites()}>
            <RotateCw />
            Try again
          </Button>
        </div>
      )}

      {tab === 'invites' && !invitesError && invitesLoading && (
        <ul className="grid gap-4 md:grid-cols-2 2xl:grid-cols-3">
          {Array.from({ length: 2 }).map((_, i) => (
            <RequestSkeleton key={i} />
          ))}
        </ul>
      )}

      {tab === 'invites' && !invitesError && !invitesLoading && (
        pendingInvites.length === 0 ? (
          <EmptyState
            title="No party invites"
            hint="When someone invites you to their canvas, it lands here for 5 minutes."
          />
        ) : (
          <ul className="grid gap-4 md:grid-cols-2 2xl:grid-cols-3">
            {pendingInvites.map((invite) => (
              <li
                key={invite.id}
                className="flex items-center gap-4 rounded-2xl border-[3px] border-outline bg-surface p-3 sticker-shadow-sm"
              >
                <span className="flex size-11 shrink-0 items-center justify-center rounded-full border-[3px] border-outline bg-primary-container">
                  <img src="/pen.svg" alt="" className="h-6 rotate-[-30deg]" />
                </span>

                <div className="min-w-0 flex-1">
                  <p className="truncate font-label font-bold">Party invite</p>
                  <p className="truncate font-body text-xs text-on-background/50">
                    from {truncateId(invite.invitedByUserId)} · Due{' '}
                    {formatClockTime(invite.expiresAt)}
                  </p>
                </div>

                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    size="sm"
                    disabled={respondInvite.isPending}
                    onClick={() =>
                      respondInvite.mutate({
                        inviteId: invite.id,
                        partyId: invite.partyId,
                        accepted: true,
                      })
                    }
                    aria-label="Accept party invite"
                  >
                    <Check />
                    <span className="hidden lg:inline">Accept</span>
                  </Button>
                  <Button
                    variant="surface"
                    size="icon"
                    disabled={respondInvite.isPending}
                    onClick={() =>
                      respondInvite.mutate({
                        inviteId: invite.id,
                        partyId: invite.partyId,
                        accepted: false,
                      })
                    }
                    aria-label="Decline party invite"
                  >
                    <X />
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        )
      )}
    </section>
  )
}

function TabButton({
  active,
  count,
  onClick,
  children,
}: {
  active: boolean
  count: number
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={`flex items-center gap-2 rounded-full border-[3px] px-4 py-2 font-label text-sm font-bold whitespace-nowrap transition-colors ${
        active
          ? 'border-outline bg-primary text-white sticker-shadow-sm'
          : 'border-outline/30 text-on-background/70 hover:border-outline hover:text-on-background'
      }`}
    >
      {children}
      <span
        className={`rounded-full px-2 py-0.5 font-label text-[11px] ${
          active ? 'bg-white/25' : 'bg-outline/10'
        }`}
      >
        {count}
      </span>
    </button>
  )
}

/*
 * Centred against the full width of the content area, so an inbox with nothing
 * in it reads as deliberately empty rather than as a list that failed to start
 * in the top left corner.
 */
function EmptyState({ title, hint }: { title: string; hint: string }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-2xl border-[3px] border-dashed border-outline/30 px-6 py-16 text-center canvas-bg">
      <span className="mb-4 flex size-16 items-center justify-center rounded-full border-[3px] border-outline bg-primary-container sticker-shadow-sm">
        <Inbox className="size-8" aria-hidden />
      </span>
      <p className="font-display text-2xl leading-none">{title}</p>
      <p className="mt-2 max-w-xs font-body text-sm text-on-background/60">{hint}</p>
    </div>
  )
}

function RequestSkeleton() {
  return (
    <li className="flex animate-pulse items-center gap-4 rounded-2xl border-[3px] border-outline bg-surface p-3">
      <span className="size-11 shrink-0 rounded-full bg-surface-dim/50" />
      <div className="flex-1 space-y-2">
        <div className="h-4 w-1/3 rounded bg-surface-dim/50" />
        <div className="h-3 w-1/4 rounded bg-surface-dim/40" />
      </div>
    </li>
  )
}
