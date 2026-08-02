import { create } from 'zustand'

export type PartyMember = { userId: string; role: 'Leader' | 'Member' }

type PartyState = {
  partyId: string | null
  members: PartyMember[]
  setParty: (partyId: string, leaderUserId: string) => void
  hydrateParty: (partyId: string, members: PartyMember[]) => void
  addMember: (userId: string) => void
  removeMember: (userId: string) => void
  setLeader: (userId: string) => void
  clearParty: () => void
}

const STORAGE_KEY = 'activePartyId'

export const usePartyStore = create<PartyState>((set) => ({
  // Seeded from localStorage so a reload remembers which party we're in; the
  // member list is empty until usePartyHydration reconciles it with the server.
  partyId: localStorage.getItem(STORAGE_KEY),
  members: [],

  setParty: (partyId, leaderUserId) => {
    localStorage.setItem(STORAGE_KEY, partyId)
    set({ partyId, members: [{ userId: leaderUserId, role: 'Leader' }] })
  },

  /* Restores the full membership after a reload, from the server's snapshot. */
  hydrateParty: (partyId, members) => {
    localStorage.setItem(STORAGE_KEY, partyId)
    set({ partyId, members })
  },

  addMember: (userId) =>
    set((s) => {
      if (s.members.some((m) => m.userId === userId)) return s
      return { members: [...s.members, { userId, role: 'Member' }] }
    }),

  removeMember: (userId) =>
    set((s) => ({ members: s.members.filter((m) => m.userId !== userId) })),

  setLeader: (userId) =>
    set((s) => ({
      members: s.members.map((m) => ({
        ...m,
        role: m.userId === userId ? 'Leader' : 'Member',
      })),
    })),

  clearParty: () => {
    localStorage.removeItem(STORAGE_KEY)
    set({ partyId: null, members: [] })
  },
}))
