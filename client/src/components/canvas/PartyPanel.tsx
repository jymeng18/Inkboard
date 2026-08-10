import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'

import { statusToast } from '@/lib/notify'
import { ChevronRight, Crown, LogOut, ShieldBan, UserMinus, UserPlus, Users } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { extractErrorMessage } from '@/api/party'
import type { CaptureSnapshot } from '@/hooks/useCanvasSnapshot'
import type { useCanvasParty } from '@/hooks/useCanvasParty'
import type { PartyMember } from '@/stores/partyStore'
import { useCanvasUiStore } from '@/stores/canvasUiStore'
import InviteDialog from './InviteDialog'
import PanelMenuItem from './PanelMenuItem'

const MAX_PARTY_SIZE = 5

type Party = ReturnType<typeof useCanvasParty>

function shortId(id: string) {
  return `${id.slice(0, 8)}…`
}

export default function PartyPanel({
  party,
  onExit,
}: {
  party: Party
  onExit?: CaptureSnapshot
}) {
  const open = useCanvasUiStore((s) => s.panelOpen)
  const togglePanel = useCanvasUiStore((s) => s.togglePanel)
  const openFriends = useCanvasUiStore((s) => s.openFriends)
  const navigate = useNavigate()
  const [inviteOpen, setInviteOpen] = useState(false)
  const [openMenuId, setOpenMenuId] = useState<string | null>(null)

  const { members, presence, currentUserId, isLeader } = party

  // Before the first invite there's no party yet, so you're solo on the canvas.
  const displayMembers: PartyMember[] =
    members.length > 0 ? members : [{ userId: currentUserId, role: 'Leader' }]

  const isOnline = (userId: string) => userId === currentUserId || (presence[userId] ?? true)
  const activeCount = displayMembers.filter((m) => isOnline(m.userId)).length

  async function run(action: () => Promise<void>, successMessage: string, thenExit = false) {
    setOpenMenuId(null)
    try {
      await action()
      statusToast.success(successMessage)
      if (thenExit) navigate('/dashboard', { replace: true })
    } catch (err) {
      toast.error(extractErrorMessage(err))
    }
  }

  return (
    <>
      {!open && (
        <button
          type="button"
          onClick={togglePanel}
          aria-label="Open party lobby"
          className="absolute top-24 right-4 z-20 flex items-center gap-2 rounded-full border-[3px] border-outline bg-surface px-4 py-2.5 font-label text-sm font-bold sticker-shadow-sm transition-transform hover:-translate-y-0.5"
        >
          <Users className="size-5" aria-hidden />
          {activeCount}
        </button>
      )}

      <aside
        className={`absolute top-4 right-4 bottom-4 z-20 flex w-80 flex-col rounded-3xl border-4 border-outline bg-surface p-5 sticker-shadow-lg transition-transform duration-200 ${
          open ? 'translate-x-0' : 'translate-x-[calc(100%+1.5rem)]'
        }`}
      >
        <div className="mb-5 flex items-start justify-between">
          <div>
            <h2 className="font-display text-3xl leading-none">Party Lobby</h2>
            <p className="mt-1 flex items-center gap-1.5 font-body text-xs text-on-background/70">
              <span className="size-2 rounded-full bg-secondary" aria-hidden />
              {activeCount} active {activeCount === 1 ? 'creator' : 'creators'} · {displayMembers.length}/
              {MAX_PARTY_SIZE}
            </p>
          </div>
          <button
            type="button"
            onClick={togglePanel}
            aria-label="Collapse party panel"
            className="flex size-8 items-center justify-center rounded-full text-on-background/60 hover:bg-background hover:text-on-background"
          >
            <ChevronRight className="size-5" aria-hidden />
          </button>
        </div>

        <ul className="flex-1 space-y-2 overflow-y-auto pr-1.5 pb-1">
          {displayMembers.map((member) => {
            const isYou = member.userId === currentUserId
            const isMemberLeader = member.role === 'Leader'
            const online = isOnline(member.userId)
            const menuOpen = openMenuId === member.userId

            // What you can do to this member.
            const canKick = isLeader && !isYou
            const canBlock = !isYou
            const canLeave = isYou
            const hasActions = canKick || canBlock || canLeave

            return (
              <li key={member.userId} className="relative">
                <button
                  type="button"
                  disabled={!hasActions}
                  onClick={() => setOpenMenuId(menuOpen ? null : member.userId)}
                  className={`flex w-full items-center gap-3 rounded-2xl border-[3px] p-2.5 text-left transition-colors ${
                    isYou
                      ? 'border-outline bg-primary-container sticker-shadow-sm'
                      : 'border-transparent hover:border-outline/30 hover:bg-background'
                  } ${hasActions ? '' : 'cursor-default'}`}
                >
                  <span className="relative shrink-0">
                    <span
                      className={`flex size-10 items-center justify-center rounded-full border-[3px] border-outline font-display text-lg ${
                        isMemberLeader ? 'bg-primary-container' : 'bg-secondary-container'
                      } ${online ? '' : 'opacity-50'}`}
                    >
                      {(isYou ? 'Y' : member.userId.charAt(0)).toUpperCase()}
                    </span>
                    <span
                      aria-hidden
                      className={`absolute -right-0.5 -bottom-0.5 size-3 rounded-full border-2 border-surface ${
                        online ? 'bg-green-500' : 'bg-surface-dim'
                      }`}
                    />
                  </span>

                  <div className="min-w-0 flex-1">
                    <p className="truncate font-label text-sm font-bold">
                      {isYou ? 'You' : shortId(member.userId)}
                    </p>
                    <span className="flex items-center gap-1 font-label text-[11px] font-bold tracking-wide uppercase text-on-background/60">
                      {isMemberLeader && <Crown className="size-3 text-primary" aria-hidden />}
                      {isMemberLeader ? 'Leader' : 'Member'} · {online ? 'Active' : 'Offline'}
                    </span>
                  </div>
                </button>

                {menuOpen && (
                  <>
                    <div className="fixed inset-0 z-40" onClick={() => setOpenMenuId(null)} aria-hidden />
                    <div className="absolute top-full right-0 z-50 mt-1 w-44 overflow-hidden rounded-2xl border-[3px] border-outline bg-surface py-1 sticker-shadow">
                      {canKick && (
                        <PanelMenuItem
                          icon={UserMinus}
                          label="Kick from party"
                          onClick={() => run(() => party.kick(member.userId), 'Member removed')}
                        />
                      )}
                      {canBlock && (
                        <PanelMenuItem
                          icon={ShieldBan}
                          label="Block user"
                          onClick={() => run(() => party.block(member.userId), 'User blocked')}
                        />
                      )}
                      {canLeave && (
                        <PanelMenuItem
                          icon={LogOut}
                          label="Leave party"
                          destructive
                          onClick={() =>
                            run(
                              async () => {
                                // Owner-only capture no-ops for members. Fire and
                                // forget so leaving stays instant; the off-stage
                                // render survives the page unmount.
                                void onExit?.()
                                await party.leave()
                              },
                              'Left the party',
                              true,
                            )
                          }
                        />
                      )}
                    </div>
                  </>
                )}
              </li>
            )
          })}
        </ul>

        <div className="mt-4 border-t-2 border-outline/15 pt-4">
          <Button size="md" className="w-full" onClick={() => setInviteOpen(true)}>
            <UserPlus />
            Invite Friends
          </Button>
        </div>
      </aside>

      <InviteDialog
        open={inviteOpen}
        onClose={() => setInviteOpen(false)}
        onInvite={party.invite}
        onPickFromFriends={() => {
          setInviteOpen(false)
          openFriends()
        }}
      />
    </>
  )
}
