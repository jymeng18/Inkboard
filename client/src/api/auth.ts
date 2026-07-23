import api from './client'

export async function registerUser(email: string, password: string, userName: string) {
  const { data } = await api.post('/auth/register', { email, password, userName })
  return data as { id: string; userName: string; email: string }
}

export async function loginUser(email: string, password: string) {
  const { data } = await api.post('/auth/login', { email, password })
  return data as { access_token: string; refresh_token: string }
}

export async function refreshToken(token: string) {
  const { data } = await api.post('/auth/refresh', { refreshToken: token })
  return data as { access_token: string }
}

export async function logoutUser() {
  const refreshToken = localStorage.getItem('refreshToken')
  if (refreshToken) {
    await api.post('/auth/logout', { refreshToken })
  }
}
