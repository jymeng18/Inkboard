import { toast } from 'sonner'

import type { FriendRequestDto } from '@/api/friends'
import FriendRequestToast from '@/components/dashboard/FriendRequestToast'

/*
 * Announces a request the poll just picked up. Friend requests never expire, so
 * this only has to be up long enough to answer in passing. The Inbox holds it
 * either way.
 */
export function showFriendRequestToast(request: FriendRequestDto) {
  toast.custom((id) => <FriendRequestToast toastId={id} request={request} />, {
    duration: 8_000,
  })
}
