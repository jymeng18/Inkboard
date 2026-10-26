import { queryClient } from '@/lib/queryClient'
import { useAuthStore } from '@/stores/authStore'
import { useConnectionStore } from '@/stores/connectionStore'
import { useCursorStore } from '@/stores/cursorStore'
import { usePartyStore } from '@/stores/partyStore'
import { useSceneStore } from '@/stores/sceneStore'

export function clearLocalSession() {
  void queryClient.cancelQueries()
  queryClient.clear()
  usePartyStore.getState().clearParty()
  useConnectionStore.getState().resetPresence()
  useConnectionStore.getState().setConnected(false)
  useCursorStore.getState().clear()
  useSceneStore.getState().clear()
  useAuthStore.getState().logout()
}
