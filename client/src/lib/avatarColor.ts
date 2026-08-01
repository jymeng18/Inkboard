/*
 * Avatar colours, straight off the theme's container roles. All of them are
 * light enough to carry the outline coloured initial on top.
 */
const AVATAR_TONES = [
  'bg-primary-container',
  'bg-secondary-container',
  'bg-tertiary-fixed',
  'bg-secondary-fixed-dim',
  'bg-primary-fixed',
] as const

/* Stable per user, so a roster reads as distinct people rather than a column
 * of identical circles. */
export function avatarTone(userId: string): string {
  let hash = 0
  for (let i = 0; i < userId.length; i++) {
    hash = (hash + userId.charCodeAt(i)) % AVATAR_TONES.length
  }
  return AVATAR_TONES[hash]
}
