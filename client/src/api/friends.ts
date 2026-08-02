import api from './client'

/*
 * No JsonStringEnumConverter is registered on the server, so RequestStatus
 * arrives over HTTP as its underlying number rather than as a name.
 */
export const RequestStatus = {
  Pending: 0,
  Accepted: 1,
  Declined: 2,
  Revoked: 3,
} as const

export type RequestStatus = (typeof RequestStatus)[keyof typeof RequestStatus]

export type FriendDto = {
  userId: string
  userName: string
}

/* userId / userName is the sender, since the server only returns your inbox. */
export type FriendRequestDto = {
  id: string
  userId: string
  userName: string
  createdAt: string
  status: RequestStatus
}

/** First segment of a uuid, which is what the UI shows under a username. */
export function truncateId(userId: string): string {
  return `${userId.slice(0, 8)}…`
}

export async function getFriends() {
  const { data } = await api.get('/friends')
  return data as FriendDto[]
}

export async function unfriend(targetUserId: string) {
  await api.delete(`/friends/${targetUserId}`)
}

export async function sendFriendRequest(targetUserId: string) {
  const { data } = await api.post(`/friends/${targetUserId}`)
  return data as FriendRequestDto
}

export async function getFriendRequests() {
  const { data } = await api.get('/friend-requests')
  return data as FriendRequestDto[]
}

export async function respondToFriendRequest(
  requestId: string,
  requesterId: string,
  accepted: boolean,
) {
  const { data } = await api.patch(`/friend-requests/${requestId}`, {
    requesterId,
    accepted,
  })
  return data as FriendRequestDto
}
