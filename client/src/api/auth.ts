import api from './client'
import type { AuthTokens } from '../types/auth'

export function registerUser(userName: string, email: string, password: string) {
  return api
    .post<{ id: string }>('/auth/register', { userName, email, password })
    .then((res) => res.data)
}

export function loginUser(email: string, password: string) {
  return api.post<AuthTokens>('/auth/login', { email, password }).then((res) => res.data)
}

export function logoutUser(refreshToken: string) {
  return api.post('/auth/logout', { refreshToken })
}
