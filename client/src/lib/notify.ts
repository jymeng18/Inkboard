import { toast } from 'sonner'

import { usePreferencesStore } from '@/stores/preferencesStore'


function enabled() {
  return usePreferencesStore.getState().statusNotifications
}

export const statusToast = {
  success: (message: string) => {
    if (enabled()) toast.success(message)
  },
  info: (message: string) => {
    if (enabled()) toast.info(message)
  },
  message: (message: string) => {
    if (enabled()) toast(message)
  },
}
