import { toast } from 'sonner'
import { Check, X } from 'lucide-react'

import { truncateId, type FriendRequestDto } from '@/api/friends'
import UserAvatar from '@/components/ui/UserAvatar'
import { useRespondToFriendRequest } from '@/hooks/useFriends'

/*
 * The card body of the friend request toast, shaped like the party invite one
 * so both arrivals read as the same kind of event. Answering here writes
 * through the same mutation the Inbox uses, so the two stay in sync.
 */
export default function FriendRequestToast({
  toastId,
  request,
}: {
  toastId: string | number
  request: FriendRequestDto
}) {
  const respond = useRespondToFriendRequest()

  function handleRespond(accepted: boolean) {
    respond.mutate(
      {
        requestId: request.id,
        requesterId: request.userId,
        userName: request.userName,
        accepted,
      },
      // The mutation reports the outcome; this card just gets out of the way.
      { onSuccess: () => toast.dismiss(toastId) },
    )
  }

  return (
    <div className="flex w-full items-center gap-3 rounded-2xl border-[3px] border-outline bg-surface p-4 font-body text-on-background sticker-shadow">
      <UserAvatar name={request.userName} userId={request.userId} size="size-10" />

      <div className="min-w-0 flex-1">
        <p className="font-display text-lg leading-none">Friend request</p>
        <p className="truncate font-body text-xs text-on-background/70">
          from {request.userName} · {truncateId(request.userId)}
        </p>
      </div>

      <button
        type="button"
        disabled={respond.isPending}
        onClick={() => handleRespond(true)}
        aria-label={`Accept ${request.userName}`}
        className="flex size-9 items-center justify-center rounded-full border-[3px] border-outline bg-primary text-white transition-transform hover:-translate-y-0.5 disabled:opacity-50"
      >
        <Check className="size-4" aria-hidden />
      </button>
      <button
        type="button"
        disabled={respond.isPending}
        onClick={() => handleRespond(false)}
        aria-label={`Decline ${request.userName}`}
        className="flex size-9 items-center justify-center rounded-full border-[3px] border-outline bg-surface transition-transform hover:-translate-y-0.5 disabled:opacity-50"
      >
        <X className="size-4" aria-hidden />
      </button>
    </div>
  )
}
