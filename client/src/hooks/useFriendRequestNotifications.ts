import { useEffect, useRef } from 'react'

import { RequestStatus } from '@/api/friends'
import { showFriendRequestToast } from '@/lib/friendRequestToast'
import { useAuthStore } from '@/stores/authStore'
import { FRIEND_REQUEST_POLL_MS, useFriendRequests } from './useFriends'

/*
 * Owns the one short poll for friend requests and toasts whatever is new.
 * Mounted once, high in the protected tree, so a request announces itself
 * wherever the user is, including mid canvas.
 *
 * A websocket would be the heavier answer to a problem this small; a 20s poll
 * that idles while the tab is blurred is enough for an invite that has no
 * deadline attached to it.
 */
export function useFriendRequestNotifications() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const { data } = useFriendRequests(FRIEND_REQUEST_POLL_MS)

  // null until the first response lands, which is what distinguishes "nothing
  // announced yet" from "nothing pending".
  const announced = useRef<Set<string> | null>(null)

  useEffect(() => {
    if (!isAuthenticated) {
      // Re-seed on the next sign-in rather than inheriting this user's ids.
      announced.current = null
      return
    }

    if (!data) return

    const pendingIds = data
      .filter((request) => request.status === RequestStatus.Pending)
      .map((request) => request.id)

    /*
     * The first fetch establishes the baseline silently. A backlog already
     * sitting in the inbox at sign in isn't news worth five toasts.
     */
    if (announced.current !== null) {
      for (const request of data) {
        if (request.status !== RequestStatus.Pending) continue
        if (!announced.current.has(request.id)) showFriendRequestToast(request)
      }
    }

    // Rebuilt, not appended to: answered requests fall out instead of piling up
    // for the lifetime of the session.
    announced.current = new Set(pendingIds)
  }, [data, isAuthenticated])
}
