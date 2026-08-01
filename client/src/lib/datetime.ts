/*
 * The API stamps timestamps in UTC, but a DateTime that loses its Kind on the
 * way out of EF serializes with no trailing Z. `new Date` reads that as local
 * time, which would skew both the "x ago" labels and the unread cutoff by the
 * viewer's offset. Anything without an explicit zone is pinned to UTC here.
 */
export function parseServerDate(value: string): Date {
  const hasZone = /(?:Z|[+-]\d{2}:?\d{2})$/.test(value)
  return new Date(hasZone ? value : `${value}Z`)
}

const MINUTE = 60_000
const HOUR = 60 * MINUTE
const DAY = 24 * HOUR

/*
 * Coarse relative time, rendered once per render rather than ticked by a timer:
 * an inbox row that says "3h ago" has no reason to re-render every second.
 */
export function timeAgo(value: string): string {
  const elapsed = Date.now() - parseServerDate(value).getTime()

  if (elapsed < MINUTE) return 'just now'
  if (elapsed < HOUR) return `${Math.floor(elapsed / MINUTE)}m ago`
  if (elapsed < DAY) return `${Math.floor(elapsed / HOUR)}h ago`
  if (elapsed < 7 * DAY) return `${Math.floor(elapsed / DAY)}d ago`

  return parseServerDate(value).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
  })
}
