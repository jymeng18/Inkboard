import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import {
  getFriends,
  getFriendRequests,
  respondToFriendRequest,
  sendFriendRequest,
  unfriend,
  RequestStatus,
  type FriendRequestDto,
} from '@/api/friends'
import { extractErrorMessage } from '@/api/party'
import { parseServerDate } from '@/lib/datetime'
import { friendRequestToastId } from '@/lib/friendRequestToast'
import { useAuthStore } from '@/stores/authStore'

export const friendKeys = {
  friends: ['friends'] as const,
  requests: ['friend-requests'] as const,
}

/*
 * Short poll for the inbox. React Query holds one timer per key no
 * matter how many components subscribe, and skips ticks while the window is
 * blurred (`refetchIntervalInBackground` defaults to false), so a backgrounded
 * tab costs nothing.
 */
export const FRIEND_REQUEST_POLL_MS = 20_000

/* Frozen so a default never hands consumers a fresh identity each render. */
const NO_REQUESTS: readonly FriendRequestDto[] = Object.freeze([])

/*
 * Module level so React Query can memoize the derived slice. An inline selector
 * would be a new function every render and re-run the filter each time.
 */
const selectPending = (requests: FriendRequestDto[]) =>
  requests.filter((request) => request.status === RequestStatus.Pending)

/* Sorts the filtered copy, never the cached array itself. */
const selectAnswered = (requests: FriendRequestDto[]) =>
  requests
    .filter((request) => request.status !== RequestStatus.Pending)
    .sort(
      (a, b) =>
        parseServerDate(b.createdAt).getTime() - parseServerDate(a.createdAt).getTime(),
    )

export function useFriends() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  return useQuery({
    queryKey: friendKeys.friends,
    queryFn: getFriends,
    enabled: isAuthenticated,
  })
}

/*
 * Every request sent to you, any status. All three views below share this one
 * cache entry, so only the caller that passes `pollMs` costs a request.
 */
function useRequestsQuery(
  select?: (requests: FriendRequestDto[]) => FriendRequestDto[],
  pollMs?: number,
) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  return useQuery({
    queryKey: friendKeys.requests,
    queryFn: getFriendRequests,
    enabled: isAuthenticated,
    select,
    refetchInterval: pollMs,
  })
}

/* Pass `pollMs` from exactly one long lived mount (RequireAuth). */
export function useFriendRequests(pollMs?: number) {
  return useRequestsQuery(undefined, pollMs)
}

/** Requests still waiting on an answer, the inbox's default view. */
export function usePendingFriendRequests() {
  const { data, ...rest } = useRequestsQuery(selectPending)
  return { data: data ?? NO_REQUESTS, ...rest }
}

/** Already answered requests, newest first, for the inbox's History view. */
export function useAnsweredFriendRequests() {
  const { data, ...rest } = useRequestsQuery(selectAnswered)
  return { data: data ?? NO_REQUESTS, ...rest }
}

export function useSendFriendRequest() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (targetUserId: string) => sendFriendRequest(targetUserId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: friendKeys.requests })
    },
  })
}

/*
 * Accepting or declining. The row is restatused in the cache up front so it
 * leaves the Requests list under the user's cursor rather than after a round
 * trip, and accepting lands a new friendship, hence the friends refetch.
 *
 * The toasts live here because the inbox, the friends panel and the arrival
 * toast all answer requests and all said the same thing.
 */
export function useRespondToFriendRequest() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      requestId,
      requesterId,
      accepted,
    }: {
      requestId: string
      requesterId: string
      userName: string
      accepted: boolean
    }) => respondToFriendRequest(requestId, requesterId, accepted),

    onMutate: async ({ requestId, accepted }) => {
      // Answered now, wherever from, so clear the arrival toast if it's still up.
      toast.dismiss(friendRequestToastId(requestId))

      await queryClient.cancelQueries({ queryKey: friendKeys.requests })
      const previous = queryClient.getQueryData<FriendRequestDto[]>(friendKeys.requests)

      queryClient.setQueryData<FriendRequestDto[]>(friendKeys.requests, (requests) =>
        requests?.map((request) =>
          request.id === requestId
            ? {
                ...request,
                status: accepted ? RequestStatus.Accepted : RequestStatus.Declined,
              }
            : request,
        ),
      )

      return { previous }
    },

    onError: (error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(friendKeys.requests, context.previous)
      }
      toast.error(extractErrorMessage(error))
    },

    onSuccess: (_data, { userName, accepted }) => {
      if (accepted) {
        queryClient.invalidateQueries({ queryKey: friendKeys.friends })
        toast.success(`You and ${userName} are now friends`)
      } else {
        toast('Request declined')
      }
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: friendKeys.requests })
    },
  })
}

export function useUnfriend() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (targetUserId: string) => unfriend(targetUserId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: friendKeys.friends })
    },
  })
}
