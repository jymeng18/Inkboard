import { useRef, useEffect, useCallback } from "react";
import * as signalR from "@microsoft/signalr";
import { toast } from "sonner";
import { showPartyInviteToast } from "@/lib/partyInviteToast";
import { statusToast } from "@/lib/notify";
import { useAuthStore } from "@/stores/authStore";
import { useInviteStore } from "@/stores/inviteStore";
import { useConnectionStore } from "@/stores/connectionStore";
import { usePartyStore } from "@/stores/partyStore";

export function usePartyHub() {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const accessToken = useAuthStore((s) => s.accessToken);
  const userId = useAuthStore((s) => s.userId);
  const addInvite = useInviteStore((s) => s.addInvite);
  const setConnected = useConnectionStore((s) => s.setConnected);
  const setLastEvent = useConnectionStore((s) => s.setLastEvent);
  const setPresence = useConnectionStore((s) => s.setPresence);
  const resetPresence = useConnectionStore((s) => s.resetPresence);
  const triggerNavToDashboard = useConnectionStore(
    (s) => s.triggerNavToDashboard,
  );
  const triggerNavToCanvas = useConnectionStore((s) => s.triggerNavToCanvas);
  const addMember = usePartyStore((s) => s.addMember);
  const removeMember = usePartyStore((s) => s.removeMember);
  const setLeader = usePartyStore((s) => s.setLeader);
  const clearParty = usePartyStore((s) => s.clearParty);

  const connect = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected)
      return;
    if (!accessToken) return;

    if (connectionRef.current) {
      try {
        await connectionRef.current.stop();
      } catch {
        /* ignore */
      }
      connectionRef.current = null;
    }

    const conn = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/party", {
        accessTokenFactory: () => accessToken,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    conn.on("NotifyOnConnection", (uid: string) => {
      setPresence(uid, true);
      setLastEvent({ event: "connected", userId: uid });
    });

    conn.on("NotifyOnDisconnect", (uid: string) => {
      setPresence(uid, false);
      setLastEvent({ event: "disconnected", userId: uid });
    });

    conn.on("NotifyOnMemberJoined", (uid: string) => {
      addMember(uid);
      setPresence(uid, true);
      statusToast.success(`Member joined: ${uid.slice(0, 8)}...`);
      setLastEvent({ event: "joined", userId: uid });
    });

    conn.on("NotifyOnMemberLeft", (uid: string) => {
      removeMember(uid);
      statusToast.info(`Member left: ${uid.slice(0, 8)}...`);
      setLastEvent({ event: "left", userId: uid });
    });

    conn.on("NotifyOnKick", (uid: string) => {
      removeMember(uid);
      if (uid === userId) {
        // Kicked = out of the party entirely, and off the canvas.
        clearParty();
        triggerNavToDashboard();
        toast.error("You were removed from the party");
      } else {
        statusToast.info(`Removed: ${uid.slice(0, 8)}...`);
      }
      setLastEvent({ event: "kicked", userId: uid });
    });

    conn.on("ReceiveInvite", (invite: unknown) => {
      const i = invite as {
        id: string;
        partyId: string;
        invitedByUserId: string;
        expiresAt: string;
        inviteStatus: number;
      };
      addInvite({
        id: i.id,
        partyId: i.partyId,
        invitedByUserId: i.invitedByUserId,
        invitedUserId: "",
        inviteStatus:
          i.inviteStatus === 0
            ? "Pending"
            : i.inviteStatus === 1
              ? "Accepted"
              : "Declined",
        expiresAt: i.expiresAt,
        createdAt: new Date().toISOString(),
      });
      showPartyInviteToast({
        id: i.id,
        partyId: i.partyId,
        invitedByUserId: i.invitedByUserId,
        expiresAt: i.expiresAt,
      });
      setLastEvent({ event: "invited", userId: i.partyId });
    });

    // The leader tore the session down: the party is gone for everyone, so the
    // wording stays neutral. The leader hears this back about their own action.
    conn.on("PartyEnded", () => {
      clearParty();
      triggerNavToDashboard();
      toast.info("Session ended — the party has been disbanded.");
      setLastEvent({ event: "party-ended", userId: "" });
    });

    /*
     * The leader opened a canvas for the party; everyone follows them in.
     * usePartyCanvasNavigation announces it, since it's the one that knows
     * whether this member actually had to move.
     */
    conn.on("PartyCanvasOpened", (canvasId: string) => {
      triggerNavToCanvas(canvasId);
      setLastEvent({ event: "canvas-opened", userId: canvasId });
    });

    conn.on("LeadershipTransferred", (newLeaderId: string) => {
      // The old leader left: leadership passes on and the canvas link breaks, so
      // everyone exits to the dashboard, the party itself stays intact.
      setLeader(newLeaderId);
      triggerNavToDashboard();
      toast.info("Leadership changed — the canvas closed. You're still in the party.");
      setLastEvent({ event: "leadership", userId: newLeaderId });
    });

    conn.onreconnecting((err) => {
      setConnected(false);
      console.log("[PartyHub] Reconnecting...", err?.message);
    });

    conn.onreconnected(() => {
      setConnected(true);
    });

    conn.onclose((err) => {
      setConnected(false);
      console.log("[PartyHub] Closed:", err?.message);
    });

    // Connection lifecycle stays in the console. Only real party activity
    // (joins, kicks, invites) surfaces as a toast.
    try {
      await conn.start();
      connectionRef.current = conn;
      setConnected(true);
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      console.log("[PartyHub] Connect failed:", msg);
    }
  }, [
    accessToken,
    userId,
    addInvite,
    setConnected,
    setLastEvent,
    setPresence,
    triggerNavToDashboard,
    triggerNavToCanvas,
    addMember,
    removeMember,
    setLeader,
    clearParty,
  ]);

  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      await connectionRef.current.stop();
      connectionRef.current = null;
      setConnected(false);
      resetPresence();
    }
  }, [setConnected, resetPresence]);

  useEffect(() => {
    if (accessToken) {
      connect();
    }
    return () => {
      disconnect();
    };
  }, [connect, disconnect, accessToken]);

  return { connect, disconnect };
}
