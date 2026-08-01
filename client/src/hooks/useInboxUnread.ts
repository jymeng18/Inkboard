import { useCallback, useState } from 'react'

import { parseServerDate } from '@/lib/datetime'
import { readLastSeen, writeLastSeen } from '@/lib/inboxRead'
import { useAuthStore } from '@/stores/authStore'
import { usePendingFriendRequests } from './useFriends'

/*
 * The "+3" next to the Inbox tab. Counts pending requests that landed after the
 * last visit; opening the tab stamps the cutoff to now and empties the badge.
 *
 * Reads off the polled requests cache, so this adds no request of its own.
 */
export function useInboxUnread() {
  const userId = useAuthStore((s) => s.userId)
  const { data: pending } = usePendingFriendRequests()

  /*
   * The cutoff is stamped with the account it was read for. Signing into a
   * second account on the same machine leaves it stale, so that case falls back
   * to a fresh read of the new user's own key rather than inheriting a cutoff
   * that was never theirs.
   */
  const [seen, setSeen] = useState(() => ({ userId, at: readLastSeen(userId) }))
  const lastSeen = seen.userId === userId ? seen.at : readLastSeen(userId)

  const unreadCount = pending.reduce(
    (count, request) =>
      parseServerDate(request.createdAt).getTime() > lastSeen ? count + 1 : count,
    0,
  )

  const markAllRead = useCallback(() => {
    const now = Date.now()
    writeLastSeen(userId, now)
    setSeen({ userId, at: now })
  }, [userId])

  return { unreadCount, markAllRead }
}
