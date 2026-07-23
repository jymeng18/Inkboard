import api from './client'

export type PartyDto = {
  id: string
  leaderId: string
  canvasId: string | null
  createdAt: string
}

export type PartyInviteDto = {
  id: string
  partyId: string
  invitedByUserId: string
  invitedUserId: string
  inviteStatus: 'Pending' | 'Accepted' | 'Declined'
  expiresAt: string
  createdAt: string
}

export type PartyDetailDto = {
  id: string
  leaderId: string
  canvasId: string | null
  members: { userId: string; role: string; joinedAt: string }[]
}

export async function getParty(partyId: string) {
  const { data } = await api.get(`/parties/${partyId}`)
  return data as PartyDetailDto
}

export async function createParty(canvasId: string) {
  const { data } = await api.post('/parties', { canvasId })
  return data as PartyDto
}

export async function inviteUser(partyId: string, invitedUserId: string) {
  const { data } = await api.post(`/parties/${partyId}/invites`, { invitedUserId })
  return data as PartyInviteDto
}

export async function respondToInvite(inviteId: string, accepted: boolean) {
  const { data } = await api.post(`/invites/${inviteId}/respond`, { accepted })
  return data as PartyInviteDto
}

export async function leaveParty(partyId: string) {
  await api.delete(`/parties/${partyId}`)
}

export async function removeMember(partyId: string, targetUserId: string) {
  await api.delete(`/parties/${partyId}/members/${targetUserId}`)
}

export async function blockUser(targetUserId: string) {
  await api.post(`/users/${targetUserId}/block`)
}

export function extractErrorMessage(err: unknown): string {
  if (err && typeof err === 'object' && 'response' in err) {
    const resp = (err as { response?: { data?: unknown } }).response
    if (resp?.data) {
      if (typeof resp.data === 'string') return resp.data
      if (typeof resp.data === 'object' && resp.data !== null) {
        const d = resp.data as Record<string, unknown>
        if (typeof d.error === 'string') return d.error
        if (typeof d.title === 'string') return d.title
      }
    }
  }
  return String(err)
}
