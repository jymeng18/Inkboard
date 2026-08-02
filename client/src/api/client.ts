import axios from 'axios'

import { clearSession, getAccessToken, getRefreshToken, setTokens } from '@/lib/session'
import type { AuthTokens } from '@/types/auth'

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

/*
 * If user is unauthroized, try again only once with their
 * new up to date access_token before giving up
 */
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      const refreshToken = getRefreshToken()

      if (refreshToken) {
        try {
          const { data } = await axios.post<AuthTokens>('/api/auth/refresh', { refreshToken })
          setTokens(data)
          originalRequest.headers.Authorization = `Bearer ${data.access_token}`
          return api(originalRequest)
        } catch {
          clearSession()
          window.location.href = '/login'
        }
      }
    }

    return Promise.reject(error)
  },
)

export default api
