import { Crown, Palette, UserMinus, Users } from 'lucide-react'

import { Button } from '@/components/ui/button'
import type { PartyMember } from '../../stores/partyStore'

const MAX_PARTY_SIZE = 5

interface PartyViewProps {
  members: PartyMember[]
  currentUserId: string
  presence: Record<string, boolean>
  onKick: (userId: string) => void
  onGoToCanvases: () => void
}

export default function PartyView({
  members,
  currentUserId,
  presence,
  onKick,
  onGoToCanvases,
}: PartyViewProps) {
  if (members.length === 0) {
    return <EmptyParty onGoToCanvases={onGoToCanvases} />
  }

  const youAreLeader = members.some((m) => m.userId === currentUserId && m.role === 'Leader')

  return (
    <section>
      <div className="mb-6">
        <h1 className="font-display text-3xl leading-none sm:text-4xl">
          Your Party{' '}
          <span className="text-primary">
            {members.length} / {MAX_PARTY_SIZE}
          </span>
        </h1>
        <p className="mt-1 font-body text-sm text-on-background/70">
          Everyone currently drawing alongside you.
        </p>
      </div>

      <ul className="flex flex-col gap-3">
        {members.map((member) => {
          const isLeader = member.role === 'Leader'
          const isYou = member.userId === currentUserId
          // Absent presence defaults to online: members stay online until the hub
          // reports a disconnect. Self is always online while viewing.
          const isOnline = isYou || (presence[member.userId] ?? true)
          return (
            <li
              key={member.userId}
              className="flex items-center gap-4 rounded-2xl border-[3px] border-outline bg-surface p-3 sticker-shadow-sm"
            >
              <span className="relative shrink-0">
                <span
                  className={`flex size-11 items-center justify-center rounded-full border-[3px] border-outline font-display text-lg ${
                    isLeader ? 'bg-primary-container' : 'bg-secondary-container'
                  } ${isOnline ? '' : 'opacity-50'}`}
                >
                  {member.userId.charAt(0).toUpperCase()}
                </span>
                <span
                  aria-hidden
                  className={`absolute -right-0.5 -bottom-0.5 size-3.5 rounded-full border-2 border-surface ${
                    isOnline ? 'bg-green-500' : 'bg-surface-dim'
                  }`}
                />
              </span>

              <div className="min-w-0 flex-1">
                <p className="truncate font-label font-bold">
                  {member.userId}
                  {isYou && <span className="text-on-background/50"> (you)</span>}
                </p>
                <span className="flex items-center gap-1 font-body text-xs text-on-background/60">
                  {isLeader ? (
                    <>
                      <Crown className="size-3.5 text-primary" aria-hidden />
                      Leader
                    </>
                  ) : (
                    'Member'
                  )}
                  <span aria-hidden>·</span>
                  {isOnline ? 'Online' : 'Offline'}
                </span>
              </div>

              {youAreLeader && !isYou && (
                <Button
                  variant="surface"
                  size="sm"
                  onClick={() => onKick(member.userId)}
                  aria-label={`Remove ${member.userId} from the party`}
                >
                  <UserMinus />
                  Kick
                </Button>
              )}
            </li>
          )
        })}
      </ul>
    </section>
  )
}

function EmptyParty({ onGoToCanvases }: { onGoToCanvases: () => void }) {
  return (
    <section className="mx-auto max-w-md py-10 text-center">
      <span className="mx-auto mb-5 flex size-16 items-center justify-center rounded-full border-[3px] border-outline bg-secondary-container sticker-shadow-sm">
        <Users className="size-8" aria-hidden />
      </span>
      <h1 className="font-display text-3xl leading-none">No active party</h1>
      <p className="mx-auto mt-3 max-w-sm font-body text-sm text-on-background/70">
        Parties form inside a canvas — open one and invite friends to start drawing together. If you
        were just dropped here, the party lost its link to its canvas.
      </p>
      <Button size="md" className="mt-6" onClick={onGoToCanvases}>
        <Palette />
        Go to Canvases
      </Button>
    </section>
  )
}
