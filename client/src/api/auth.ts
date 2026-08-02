import api from './client'
import type { AuthTokens } from '@/types/auth'

/* Mirrors RegisterRequestValidator and LoginRequestValidator. */
export const AUTH_LIMITS = {
  userName: { min: 3, max: 30 },
  email: { max: 256 },
  password: { min: 6, max: 128 },
} as const

export async function registerUser(userName: string, email: string, password: string) {
  const res = await api
    .post<{ id: string} >('/auth/register', { userName, email, password })
  return res.data
}

export async function loginUser(email: string, password: string) {
  const res = await api.post<AuthTokens>('/auth/login', { email, password })
  return res.data
}

export function logoutUser(refreshToken: string) {
  return api.post('/auth/logout', { refreshToken })
}
