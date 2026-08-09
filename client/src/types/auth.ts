/** The login and refresh endpoints return only the access token; the refresh token lives in an httpOnly cookie. */
export interface AuthTokens {
  access_token: string
}

/** Claims embedded in the access token by the server. */
export interface JwtClaims {
  sub: string
  email: string
  name: string
  exp: number
}

/** The signed-in user. */
export interface SessionUser {
  userId: string
  userName: string
  accessToken: string
}
