/** Token pair returned by the login and refresh endpoints. */
export interface AuthTokens {
  access_token: string
  refresh_token: string
}

/** Claims embedded in the access token by the server. */
export interface JwtClaims {
  sub: string
  email: string
  exp: number
}

/** The signed-in user. */
export interface SessionUser {
  userId: string
  userName: string
  accessToken: string
}
