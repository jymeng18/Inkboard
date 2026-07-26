import { create } from 'zustand'
import type { PartyInviteDto } from '@/api/party'

type InviteState = {
  invites: PartyInviteDto[]
  addInvite: (invite: PartyInviteDto) => void
  removeInvite: (inviteId: string) => void
  clearInvites: () => void
}

export const useInviteStore = create<InviteState>((set) => ({
  invites: [],
  addInvite: (invite) => set((s) => ({ invites: [...s.invites, invite] })),
  removeInvite: (inviteId) => set((s) => ({ invites: s.invites.filter((i) => i.id !== inviteId) })),
  clearInvites: () => set({ invites: [] }),
}))
