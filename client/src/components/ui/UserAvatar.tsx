import { avatarTone } from '@/lib/avatarColor'

/*
 * The initial sticker used for a person anywhere they appear: friends panel,
 * inbox, request toast. The tone comes off their id, so the same person keeps
 * the same colour across all of them.
 */
export default function UserAvatar({
  name,
  userId,
  size = 'size-9',
  dimmed,
}: {
  name: string
  userId: string
  size?: string
  dimmed?: boolean
}) {
  return (
    <span
      className={`flex shrink-0 items-center justify-center rounded-full border-[3px] border-outline font-display text-lg ${size} ${avatarTone(
        userId,
      )} ${dimmed ? 'opacity-55' : ''}`}
    >
      {name.charAt(0).toUpperCase()}
    </span>
  )
}
