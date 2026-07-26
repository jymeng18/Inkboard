import {
  blockUser,
  createParty,
  inviteUser,
  leaveParty,
  removeMember as removeMemberApi,
} from '@/api/party'
import { useAuthStore } from '@/stores/authStore'
import { useConnectionStore } from '@/stores/connectionStore'
import { usePartyStore } from '@/stores/partyStore'

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

/*
 * All party operations for the canvas, in one place. The party is born lazily:
 * there's no party until the first invite, so `invite` creates it (making the
 * inviter the leader) and then sends. Everything throws on failure so callers
 * can surface the backend message via extractErrorMessage.
 */
export function useCanvasParty(canvasId: string | undefined) {
  const partyId = usePartyStore((s) => s.partyId)
  const members = usePartyStore((s) => s.members)
  const setParty = usePartyStore((s) => s.setParty)
  const removeMemberFromStore = usePartyStore((s) => s.removeMember)
  const clearParty = usePartyStore((s) => s.clearParty)
  const currentUserId = useAuthStore((s) => s.userId)
  const presence = useConnectionStore((s) => s.presence)

  const isLeader = members.some((m) => m.userId === currentUserId && m.role === 'Leader')

  async function ensurePartyId(): Promise<string> {
    if (partyId) return partyId
    if (!canvasId || !GUID.test(canvasId)) {
      throw new Error('Save this canvas before inviting anyone.')
    }
    const party = await createParty(canvasId)
    setParty(party.id, party.leaderId)
    return party.id
  }

  async function invite(userId: string) {
    const id = await ensurePartyId()
    await inviteUser(id, userId)
  }

  async function kick(userId: string) {
    if (!partyId) return
    await removeMemberApi(partyId, userId)
    // Optimistic — the NotifyOnKick broadcast also removes it for everyone else.
    removeMemberFromStore(userId)
  }

  async function block(userId: string) {
    await blockUser(userId)
  }

  async function leave() {
    if (!partyId) return
    await leaveParty(partyId)
    clearParty()
  }

  return { partyId, members, presence, currentUserId, isLeader, invite, kick, block, leave }
}
