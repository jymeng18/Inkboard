import { useQuery } from '@tanstack/react-query'

import { getParty } from '@/api/party'
import { useAuthStore } from '@/stores/authStore'
import { usePartyStore } from '@/stores/partyStore'

/*
 * Authoritative view of the current party for the dashboard: does the party have
 * a live canvas, and are we its leader? getParty is the source of truth for the
 * canvas link — the field that severs on a leadership transfer — which the
 * in-memory stores don't track.
 *
 * A member of a party that's mid-session is `locked`: opening or creating a
 * canvas from here would strand them in a board the party isn't on. Leaders are
 * never locked — a canvas they open just re-links the party and pulls everyone
 * in — but they still get the banner so they can jump back.
 */
export function usePartySession() {
  const partyId = usePartyStore((s) => s.partyId)
  const currentUserId = useAuthStore((s) => s.userId)

  const { data: party } = useQuery({
    queryKey: ['party', partyId],
    queryFn: () => getParty(partyId as string),
    enabled: !!partyId,
  })

  // Derive from our own membership row, not just the id: a stale or foreign
  // partyId (e.g. left over across accounts) must never lock this user out.
  const me = party?.members.find((m) => m.userId === currentUserId)
  const activeCanvasId = party?.canvasId ?? null
  const isLeader = me?.role === 'Leader'
  const inActiveSession = !!me && activeCanvasId !== null
  const locked = inActiveSession && !isLeader

  return { partyId, activeCanvasId, isLeader, inActiveSession, locked }
}
