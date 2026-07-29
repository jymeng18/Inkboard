const MOBILE_NOTICE = 'inkboard.mobileNoticePending'

/*
 * One-shot flag for the "you're on a phone" heads-up. Registration is the only
 * writer, so the notice can only ever fire on an account's first session.
 * localStorage rather than a cookie: this is throwaway UI state, not session
 * data, and it must survive the redirect from /login to /dashboard.
 *
 * Every access is guarded because storage throws outright in some private
 * browsing modes, and a missed notice must never break signing up.
 */
export function markMobileNoticePending() {
  try {
    localStorage.setItem(MOBILE_NOTICE, '1')
  } catch {
    // No storage, no notice. Not worth failing registration over.
  }
}

export function isMobileNoticePending() {
  try {
    return localStorage.getItem(MOBILE_NOTICE) === '1'
  } catch {
    return false
  }
}

export function clearMobileNoticePending() {
  try {
    localStorage.removeItem(MOBILE_NOTICE)
  } catch {
    // Nothing was stored in the first place.
  }
}
