import api from '@/api/client'

export type AccountDto = {
  id: string
  userName: string
  email: string
  createdAt: string
}

export async function getAccount() {
  const { data } = await api.get<AccountDto>('/users/me')
  return data
}

export async function deleteAccount(password: string) {
  await api.delete('/users/me', { data: { password } })
}
