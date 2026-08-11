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

  /*
   * Owner-only background fallback: periodically snapshot the canvas while it is
   * open, so a crash or closed tab doesn't lose work. Off by default because
   * saving is otherwise the owner's explicit choice on exit; this is opt-in for
   * people who want the safety net.
   */
  autosaveSnapshots: boolean
  setAutosaveSnapshots: (enabled: boolean) => void
}

export const usePreferencesStore = create<PreferencesState>()(
  persist(
    (set) => ({
      statusNotifications: true,
      setStatusNotifications: (statusNotifications) => set({ statusNotifications }),

      autosaveSnapshots: false,
      setAutosaveSnapshots: (autosaveSnapshots) => set({ autosaveSnapshots }),
    }),
    { name: 'inkboard:preferences' },
  ),
)
