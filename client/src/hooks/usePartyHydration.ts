import { useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'

import { getParty } from '@/api/party'
import { useAuthStore } from '@/stores/authStore'
import { usePartyStore } from '@/stores/partyStore'

/*
 * Restores the party after a full page reload. The partyId survives in
 * localStorage, but everything else (members, roles, the canvas link) is
 * in-memory and gone, so this re-reads the party from the server and repopulates
 * the store.
 *
 * One-shot: it only acts while the store has a persisted id but no members yet.
 * Once hydrated, live hub events own the membership. It also self-heals a stale
 * id — a party that was dissolved (404) or one we were removed from while away
 * is dropped rather than left haunting the UI.
 */
export function usePartyHydration() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const currentUserId = useAuthStore((s) => s.userId)
  const partyId = usePartyStore((s) => s.partyId)
  const hydrateParty = usePartyStore((s) => s.hydrateParty)
  const clearParty = usePartyStore((s) => s.clearParty)

  const { data, error } = useQuery({
    queryKey: ['party', partyId],
    queryFn: () => getParty(partyId as string),
    enabled: isAuthenticated && !!partyId,
  })

  useEffect(() => {
    // Nothing to do once the store is populated; the hub keeps it live from here.
    if (!partyId || usePartyStore.getState().members.length > 0) return

    const status = (error as { response?: { status?: number } } | null)?.response?.status
    if (status === 404) {
      clearParty() // party no longer exists → forget the stale id
      return
    }
    // Still loading, or a transient error we shouldn't act on: leave the id be.
    if (!data) return

    const stillMember = data.members.some((m) => m.userId === currentUserId)
    if (!stillMember) {
      clearParty() // removed from the party while we were away
      return
    }

    hydrateParty(
      data.id,
      data.members.map((m) => ({
        userId: m.userId,
        role: m.role === 'Leader' ? 'Leader' : 'Member',
      })),
    )
  }, [data, error, partyId, currentUserId, hydrateParty, clearParty])
}
