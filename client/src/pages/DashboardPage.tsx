import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";

import { statusToast } from "@/lib/notify";

import {
  canvasDisplayName,
  normalizeCanvasName,
  DEFAULT_CANVAS_NAME,
  type CanvasDto,
} from "@/api/canvas";
import {
  extractErrorMessage,
  removeMember as removeMemberApi,
  setPartyCanvas,
} from "@/api/party";

import {
  useCanvases,
  useCreateCanvas,
  useRenameCanvas,
} from "@/hooks/useCanvases";
import { usePendingFriendRequests } from "@/hooks/useFriends";
import { useInboxUnread } from "@/hooks/useInboxUnread";
import { useAuth } from "@/hooks/useAuth";
import { MOBILE_VIEWPORT } from "@/hooks/useMediaQuery";
import {
  clearMobileNoticePending,
  isMobileNoticePending,
} from "@/lib/firstRun";

import CanvasNameDialog from "@/components/dashboard/CanvasNameDialog";
import CanvasesView from "@/components/dashboard/CanvasesView";
import DashboardSidebar, {
  UnreadBadge,
} from "@/components/dashboard/DashboardSidebar";
import {
  NAV_ITEMS,
  type DashboardView,
} from "@/components/dashboard/dashboardNav";
import DashboardTopBar from "@/components/dashboard/DashboardTopBar";
import FriendsPanel from "@/components/dashboard/FriendsPanel";
import InboxView from "@/components/dashboard/InboxView";
import MobileExperienceNotice from "@/components/dashboard/MobileExperienceNotice";
import PartyView from "@/components/dashboard/PartyView";
import SettingsView from "@/components/dashboard/SettingsView";
import ConfirmDialog from "@/components/ui/ConfirmDialog";

import { useAuthStore } from "@/stores/authStore";
import { useConnectionStore } from "@/stores/connectionStore";
import { usePartyStore } from "@/stores/partyStore";

/**
 * useMutation, useQeury used for anything involving backend requests, 
 * useQuery: reading data from server
 * useMutation: editing/create/delete data from server
 */

/* Which naming dialog is open, and what it is naming. */
type NameDialogState =
  | { mode: "create" }
  | { mode: "rename"; canvas: CanvasDto }
  | null;

export default function DashboardPage() {
  const navigate = useNavigate();
  const { logout } = useAuth();
  const userName = useAuthStore((s) => s.userName);
  const currentUserId = useAuthStore((s) => s.userId);

  const partyId = usePartyStore((s) => s.partyId);
  const members = usePartyStore((s) => s.members);
  const removeMemberFromStore = usePartyStore((s) => s.removeMember);
  const presence = useConnectionStore((s) => s.presence);

  const { data: pendingRequests } = usePendingFriendRequests();
  const { unreadCount, markAllRead } = useInboxUnread();

  const [view, setView] = useState<DashboardView>("canvases");
  const [friendsOpen, setFriendsOpen] = useState(false);
  const [nameDialog, setNameDialog] = useState<NameDialogState>(null);

  /* Landing on the Inbox is what counts as reading it, so the badge clears here. */
  const selectView = useCallback(
    (next: DashboardView) => {
      setView(next);
      if (next === "inbox") markAllRead();
    },
    [markAllRead],
  );

  /*
   * The pending flag is only ever set by registering, so this resolves on the
   * first dashboard load of a brand new account. Read once rather than through
   * a live media query: the rule is "was this a phone at first sign-in", so
   * later rotating or resizing shouldn't change the answer.
   */
  const [mobileNoticeOpen, setMobileNoticeOpen] = useState(
    () => isMobileNoticePending() && window.matchMedia(MOBILE_VIEWPORT).matches,
  );

  /*
   * Burn the flag the moment the notice isn't showing: either it was just
   * dismissed, or this was a desktop signup and it never had cause to appear.
   * Clearing on dismissal rather than on display is what lets a refresh
   * mid-notice still bring it back.
   */
  useEffect(() => {
    if (!mobileNoticeOpen) clearMobileNoticePending();
  }, [mobileNoticeOpen]);

  /*
   * A request that arrives from the poll while the Inbox is already open counts
   * as read on arrival, since the user is looking straight at it.
   */
  useEffect(() => {
    if (view === "inbox" && unreadCount > 0) markAllRead();
  }, [view, unreadCount, markAllRead]);

  // * useQuery rets all properties, not explicitly defined in the hook
  const { data: canvases = [], isLoading, isError, refetch } = useCanvases();

  async function handleLogout() {
    await logout();
    navigate("/login", { replace: true });
  }

  async function handleKick(targetUserId: string) {
    if (!partyId) return;
    try {
      await removeMemberApi(partyId, targetUserId);
      removeMemberFromStore(targetUserId);
      statusToast.success("Removed from party");
    } catch (err) {
      toast.error(extractErrorMessage(err));
    }
  }

  /*
   * A leader opening a canvas moves the whole party into it, which is a big
   * enough side effect to ask about first. Only the leader of a party that has
   * someone else in it gets the prompt; everyone else just opens the canvas.
   */
  const isPartyLeader = members.some(
    (m) => m.userId === currentUserId && m.role === "Leader",
  );
  const [partyMoveCanvas, setPartyMoveCanvas] = useState<CanvasDto | null>(null);
  const [movingParty, setMovingParty] = useState(false);

  function openCanvas(canvas: CanvasDto) {
    if (partyId && isPartyLeader && members.length > 1) {
      setPartyMoveCanvas(canvas);
      return;
    }
    void enterCanvas(canvas);
  }

  /*
   * Re-points the party at whatever canvas the leader is entering. Worth doing
   * even when they're the only member: the party's CanvasId is what an invitee
   * is sent to, so leaving it stale drops the next person onto a canvas the
   * party has already moved off.
   */
  async function enterCanvas(canvas: CanvasDto) {
    if (partyId && isPartyLeader) {
      try {
        await setPartyCanvas(partyId, canvas.id);
        if (members.length > 1) statusToast.success("Your party is coming with you");
      } catch (err) {
        // Non-fatal: opening your own canvas shouldn't hinge on the party link.
        toast.error(extractErrorMessage(err));
      }
    }
    navigate(`/canvas/${canvas.id}`);
  }

  /*
   * Links the party to the canvas before navigating, so the hub broadcast that
   * pulls members in has already gone out by the time we land.
   */
  async function confirmPartyMove(canvas: CanvasDto) {
    if (!partyId) return;
    setMovingParty(true);
    try {
      await setPartyCanvas(partyId, canvas.id);
      navigate(`/canvas/${canvas.id}`);
    } catch (err) {
      toast.error(extractErrorMessage(err));
      setMovingParty(false);
      setPartyMoveCanvas(null);
    }
  }

  const createCanvasMutation = useCreateCanvas();
  const renameCanvasMutation = useRenameCanvas();

  /* The dialog names the canvas first; creating it drops us straight into it. */
  function handleCreate(rawName: string) {
    if (createCanvasMutation.isPending) return;
    createCanvasMutation.mutate(normalizeCanvasName(rawName), {
      onSuccess: (canvas) => {
        setNameDialog(null);
        /*
         * Goes through enterCanvas so a leader's new board becomes the party's
         * board too. No second confirmation here: naming and creating it is
         * already deliberate enough, and the toast says what happened.
         */
        void enterCanvas(canvas);
      },
      onError: (err) => toast.error(extractErrorMessage(err)),
    });
  }

  function handleRename(canvas: CanvasDto, rawName: string) {
    if (renameCanvasMutation.isPending) return;
    const name = normalizeCanvasName(rawName);

    // Nothing changed, so skip the request and just close.
    if (name === canvasDisplayName(canvas)) {
      setNameDialog(null);
      return;
    }

    renameCanvasMutation.mutate(
      { canvasId: canvas.id, name },
      {
        onSuccess: () => {
          setNameDialog(null);
          toast.success(`Renamed to "${name}"`);
        },
        onError: (err) => toast.error(extractErrorMessage(err)),
      },
    );
  }

  return (
    <div className="flex min-h-screen flex-col bg-background font-body text-on-background">
      <DashboardTopBar
        userName={userName}
        friendsOpen={friendsOpen}
        requestCount={pendingRequests.length}
        onToggleFriends={() => setFriendsOpen((prev) => !prev)}
        onLogout={handleLogout}
      />

      <div className="flex flex-1">
        <DashboardSidebar
          active={view}
          onSelect={selectView}
          partySize={members.length}
          unreadCount={unreadCount}
        />

        <main className="flex-1 overflow-x-hidden p-5 sm:p-8">
          <nav className="mb-6 flex gap-2 overflow-x-auto lg:hidden">
            {NAV_ITEMS.map(({ view: tab, label, icon: Icon }) => (
              <button
                key={tab}
                type="button"
                onClick={() => selectView(tab)}
                className={`flex items-center gap-2 rounded-full border-[3px] px-4 py-2 font-label text-sm font-bold whitespace-nowrap transition-colors ${
                  view === tab
                    ? "border-outline bg-primary text-white sticker-shadow-sm"
                    : "border-outline/30 text-on-background/70"
                }`}
              >
                <Icon className="size-4" aria-hidden />
                {label}
                {tab === "inbox" && unreadCount > 0 && (
                  <UnreadBadge count={unreadCount} />
                )}
              </button>
            ))}
          </nav>

          {view === "canvases" && (
            <CanvasesView
              canvases={canvases}
              isLoading={isLoading}
              isError={isError}
              onRetry={() => refetch()}
              onNewCanvas={() => setNameDialog({ mode: "create" })}
              onOpenCanvas={openCanvas}
              onRenameCanvas={(canvas) =>
                setNameDialog({ mode: "rename", canvas })
              }
            />
          )}

          {view === "inbox" && <InboxView />}

          {view === "party" && (
            <PartyView
              members={members}
              currentUserId={currentUserId}
              presence={presence}
              onKick={handleKick}
              onGoToCanvases={() => selectView("canvases")}
            />
          )}

          {view === "settings" && (
            <SettingsView
              userId={currentUserId}
              userName={userName}
              onLogout={handleLogout}
            />
          )}
        </main>
      </div>

      <FriendsPanel open={friendsOpen} onClose={() => setFriendsOpen(false)} />

      {mobileNoticeOpen && (
        <MobileExperienceNotice onDismiss={() => setMobileNoticeOpen(false)} />
      )}

      {partyMoveCanvas && (
        <ConfirmDialog
          title="You're the party leader"
          description={`Opening "${canvasDisplayName(partyMoveCanvas)}" pulls your party in with you — ${
            members.length - 1
          } other ${members.length - 1 === 1 ? "member" : "members"} will be redirected to this canvas.`}
          confirmLabel="Open for everyone"
          cancelLabel="Not now"
          pending={movingParty}
          onConfirm={() => confirmPartyMove(partyMoveCanvas)}
          onClose={() => setPartyMoveCanvas(null)}
        />
      )}

      {nameDialog?.mode === "create" && (
        <CanvasNameDialog
          title="Name your canvas"
          description="Give it something you'll recognize later. You can rename it any time."
          submitLabel="Create"
          initialName={DEFAULT_CANVAS_NAME}
          pending={createCanvasMutation.isPending}
          onSubmit={handleCreate}
          onClose={() => setNameDialog(null)}
        />
      )}

      {nameDialog?.mode === "rename" && (
        <CanvasNameDialog
          key={nameDialog.canvas.id}
          title="Rename canvas"
          description="Pick a new name for this board."
          submitLabel="Save"
          initialName={canvasDisplayName(nameDialog.canvas)}
          pending={renameCanvasMutation.isPending}
          onSubmit={(name) => handleRename(nameDialog.canvas, name)}
          onClose={() => setNameDialog(null)}
        />
      )}
    </div>
  );
}
