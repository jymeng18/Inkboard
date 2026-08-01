/*
 * When the inbox was last opened, per account. Opening the tab is what marks
 * everything in it read, so the unread badge is just "arrived after this
 * timestamp", with no server side read state and no growing set of seen ids.
 *
 * Keyed by user so signing into a second account on the same machine doesn't
 * inherit the first one's cutoff.
 */
const storageKey = (userId: string) => `inbox:lastSeen:${userId}`

/** Epoch millis of the last visit, or 0 so a first-ever visit shows everything waiting. */
export function readLastSeen(userId: string): number {
  if (!userId) return 0

  try {
    const stored = window.localStorage.getItem(storageKey(userId))
    const parsed = stored === null ? Number.NaN : Number(stored)
    return Number.isFinite(parsed) ? parsed : 0
  } catch {
    // Storage can be unavailable (private mode, blocked cookies); the badge
    // just falls back to counting everything.
    return 0
  }
}

export function writeLastSeen(userId: string, timestamp: number): void {
  if (!userId) return

  try {
    window.localStorage.setItem(storageKey(userId), String(timestamp))
  } catch {
    /* Non-fatal: the badge stops persisting, nothing else breaks. */
  }
}
