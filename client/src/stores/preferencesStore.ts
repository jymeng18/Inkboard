import { create } from 'zustand'
import { persist } from 'zustand/middleware'


interface PreferencesState {
  /*
   * Ambient party and social activity toasts: members joining or leaving,
   * invites you send, kicks, and the like. Turning this off silences those.
   * Invites and friend requests sent *to* you (and every error) ignore this
   * and always show.
   */
  statusNotifications: boolean
  setStatusNotifications: (enabled: boolean) => void
}

export const usePreferencesStore = create<PreferencesState>()(
  persist(
    (set) => ({
      statusNotifications: true,
      setStatusNotifications: (statusNotifications) => set({ statusNotifications }),
    }),
    { name: 'inkboard:preferences' },
  ),
)
