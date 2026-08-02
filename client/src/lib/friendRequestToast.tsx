import { toast } from 'sonner'

import type { FriendRequestDto } from '@/api/friends'
import FriendRequestToast from '@/components/dashboard/FriendRequestToast'

/*
 * A stable, per-request toast id. Giving the arrival toast a deterministic id
 * lets whoever answers the request — the Inbox, the friends panel, or the toast
 * itself — dismiss this exact toast, so an already-answered request never
 * lingers on screen.
 */
export const friendRequestToastId = (requestId: string) => `friend-request:${requestId}`

/*
 * Announces a request the poll just picked up. Friend requests never expire, so
 * this only has to be up long enough to answer in passing. The Inbox holds it
 * either way.
 */
export function showFriendRequestToast(request: FriendRequestDto) {
  toast.custom((id) => <FriendRequestToast toastId={id} request={request} />, {
    id: friendRequestToastId(request.id),
    duration: 8_000,
  })
}
