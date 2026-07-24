/** Presence shown next to a friend in the friends panel. */
export type PresenceStatus = 'online' | 'in-canvas' | 'offline'

export interface Friend {
  id: string
  userName: string
  status: PresenceStatus
}

export interface FriendRequest {
  id: string
  userName: string
}
