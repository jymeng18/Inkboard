import { useQuery } from '@tanstack/react-query'

import { getAccount } from '@/api/account'
import { useAuthStore } from '@/stores/authStore'

export function useAccount() {
  const userId = useAuthStore((state) => state.userId)

  return useQuery({
    queryKey: ['account', userId],
    queryFn: getAccount,
    enabled: !!userId,
  })
}
