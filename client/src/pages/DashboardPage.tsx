import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { Palette, Settings, Users } from "lucide-react";

import {
  canvasDisplayName,
  normalizeCanvasName,
  DEFAULT_CANVAS_NAME,
  type CanvasDto,
} from "@/api/canvas";
import {
  extractErrorMessage,
  removeMember as removeMemberApi,
} from "@/api/party";

import {
  useCanvases,
  useCreateCanvas,
  useRenameCanvas,
} from "@/hooks/useCanvases";
import { useAuth } from "@/hooks/useAuth";
import { MOBILE_VIEWPORT } from "@/hooks/useMediaQuery";
import {
  clearMobileNoticePending,
  isMobileNoticePending,
} from "@/lib/firstRun";

import CanvasNameDialog from "@/components/dashboard/CanvasNameDialog";
import CanvasesView from "@/components/dashboard/CanvasesView";
import DashboardSidebar, {
  type DashboardView,
} from "@/components/dashboard/DashboardSidebar";
import DashboardTopBar from "@/components/dashboard/DashboardTopBar";
import FriendsPanel from "@/components/dashboard/FriendsPanel";
import MobileExperienceNotice from "@/components/dashboard/MobileExperienceNotice";
import PartyView from "@/components/dashboard/PartyView";
import SettingsView from "@/components/dashboard/SettingsView";

import { useAuthStore } from "@/stores/authStore";
import { useConnectionStore } from "@/stores/connectionStore";
import { usePartyStore } from "@/stores/partyStore";
import type { Friend, FriendRequest } from "@/types/social";

/**
 * useMutation, useQeury used for anything involving backend requests, 
 * useQuery: reading data from server
 * useMutation: editing/create/delete data from server
 */

const MOBILE_TABS: {
  view: DashboardView;
  label: string;
  icon: typeof Palette;
}[] = [
  { view: "canvases", label: "Canvases", icon: Palette },
  { view: "party", label: "Party", icon: Users },
  { view: "settings", label: "Settings", icon: Settings },
];

// TODO: Backend needs to create our friends list features
const FRIENDS: Friend[] = [];
const FRIEND_REQUESTS: FriendRequest[] = [];

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

  const [view, setView] = useState<DashboardView>("canvases");
  const [friendsOpen, setFriendsOpen] = useState(false);
  const [nameDialog, setNameDialog] = useState<NameDialogState>(null);
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
      toast.success("Removed from party");
    } catch (err) {
      toast.error(extractErrorMessage(err));
    }
  }

  const openCanvas = (canvas: CanvasDto) => navigate(`/canvas/${canvas.id}`);

  const createCanvasMutation = useCreateCanvas();
  const renameCanvasMutation = useRenameCanvas();

  /* The dialog names the canvas first; creating it drops us straight into it. */
  function handleCreate(rawName: string) {
    if (createCanvasMutation.isPending) return;
    createCanvasMutation.mutate(normalizeCanvasName(rawName), {
      onSuccess: (canvas) => {
        setNameDialog(null);
        navigate(`/canvas/${canvas.id}`);
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
        requestCount={FRIEND_REQUESTS.length}
        onToggleFriends={() => setFriendsOpen((prev) => !prev)}
        onLogout={handleLogout}
      />

      <div className="flex flex-1">
        <DashboardSidebar
          active={view}
          onSelect={setView}
          partySize={members.length}
        />

        <main className="flex-1 overflow-x-hidden p-5 sm:p-8">
          <nav className="mb-6 flex gap-2 overflow-x-auto lg:hidden">
            {MOBILE_TABS.map(({ view: tab, label, icon: Icon }) => (
              <button
                key={tab}
                type="button"
                onClick={() => setView(tab)}
                className={`flex items-center gap-2 rounded-full border-[3px] px-4 py-2 font-label text-sm font-bold whitespace-nowrap transition-colors ${
                  view === tab
                    ? "border-outline bg-primary text-white sticker-shadow-sm"
                    : "border-outline/30 text-on-background/70"
                }`}
              >
                <Icon className="size-4" aria-hidden />
                {label}
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

          {view === "party" && (
            <PartyView
              members={members}
              currentUserId={currentUserId}
              presence={presence}
              onKick={handleKick}
              onGoToCanvases={() => setView("canvases")}
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

      <FriendsPanel
        open={friendsOpen}
        onClose={() => setFriendsOpen(false)}
        friends={FRIENDS}
        requests={FRIEND_REQUESTS}
      />

      {mobileNoticeOpen && (
        <MobileExperienceNotice onDismiss={() => setMobileNoticeOpen(false)} />
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
