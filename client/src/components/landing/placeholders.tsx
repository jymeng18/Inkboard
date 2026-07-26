/*
 * Self contained svgs, in 
 */

const AVATAR_COLORS = [
  ['#ffd23f', '#ff7070'],
  ['#a7dbfe', '#00c1fd'],
  ['#ffcbb8', '#a73a00'],
] as const

interface AvatarProps {
  index: number
  className?: string
}

export function Avatar({ index, className = '' }: AvatarProps) {
  const [bg, fg] = AVATAR_COLORS[index % AVATAR_COLORS.length]

  return (
    <svg viewBox="0 0 48 48" role="img" aria-label="Inkboard user" className={className}>
      <rect width="48" height="48" fill={bg} />
      <circle cx="24" cy="19" r="8" fill={fg} />
      <path d="M8 48c0-9 7-15 16-15s16 6 16 15z" fill={fg} />
    </svg>
  )
}

interface DoodleProps {
  index: number
  className?: string
}

const DOODLE_PALETTES = [
  { bg: '#fdf3d8', ink: '#a73a00', accent: '#ffd23f' },
  { bg: '#ffe4e4', ink: '#ff7070', accent: '#00c1fd' },
  { bg: '#e0f4ff', ink: '#00c1fd', accent: '#2d2926' },
  { bg: '#ffe9f2', ink: '#ff7070', accent: '#ffd23f' },
] as const

/*
 * Four distinct hand-drawn-looking sketches for the polaroid grid. Each is a
 * loose composition of strokes rather than a literal subject, matching the
 * "someone doodled this in a session" feel of the design.
 */
export function Doodle({ index, className = '' }: DoodleProps) {
  const { bg, ink, accent } = DOODLE_PALETTES[index % DOODLE_PALETTES.length]

  return (
    <svg viewBox="0 0 200 200" role="img" aria-label="Community doodle" className={className}>
      <rect width="200" height="200" fill={bg} />
      <g fill="none" stroke={ink} strokeWidth="4" strokeLinecap="round" strokeLinejoin="round">
        {index === 0 && (
          <>
            <rect x="70" y="80" width="60" height="60" rx="8" />
            <circle cx="88" cy="102" r="6" />
            <circle cx="112" cy="102" r="6" />
            <path d="M84 122q16 12 32 0" />
            <path d="M100 80V56" />
            <circle cx="100" cy="48" r="10" fill={accent} stroke="none" />
          </>
        )}
        {index === 1 && (
          <>
            <path d="M40 150q30-90 60-40t60-30" />
            <path d="M40 170q30-90 60-40t60-30" stroke={accent} />
            <circle cx="150" cy="60" r="18" />
          </>
        )}
        {index === 2 && (
          <>
            <rect x="55" y="60" width="90" height="70" rx="10" />
            <circle cx="100" cy="95" r="20" />
            <circle cx="100" cy="95" r="8" fill={accent} stroke="none" />
            <path d="M70 60V45h30v15" />
          </>
        )}
        {index === 3 && (
          <>
            <path d="M60 130q40-70 80 0z" />
            <path d="M60 130h80" />
            <circle cx="82" cy="104" r="5" fill={ink} />
            <circle cx="118" cy="104" r="5" fill={ink} />
            <path d="M75 70l-12-22M125 70l12-22" stroke={accent} />
          </>
        )}
      </g>
    </svg>
  )
}
