import { useCallback, useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'

import { subscribeLocalOps } from '@/lib/opChannel'
import { useAuthStore } from '@/stores/authStore'
import { useCursorStore } from '@/stores/cursorStore'
import { usePartyStore } from '@/stores/partyStore'
import { useSceneStore, type SceneItem } from '@/stores/sceneStore'
import { useViewportStore } from '@/stores/viewportStore'

// Cap outbound cursor frames. Raw pointermove fires ~120/sec; this keeps the
// wire (and the receivers) sane while staying well under the threshold of
// perceptible lag. Matches the ~30ms-50ms guidance in the cursor-tracking doc.
const SEND_INTERVAL_MS = 45

// A cursor we haven't heard from in this long is treated as gone.
const CURSOR_TTL_MS = 8000
const PRUNE_INTERVAL_MS = 2000

interface CursorPacket {
  userId?: string
  x: number
  y: number
}

// The server relays the op envelope; we only read the opaque payload it never parses.
interface OperationPacket {
  operationData: string
}

/*
 * Live sync for one canvas over the CanvasHub. Connects, joins the canvas group
 * (the hub's single auth checkpoint), streams the local cursor (throttled, in world
 * coordinates), and broadcasts / applies finished drawing operations. Operations
 * are commit-only for now: one op per finished stroke, not live in-progress points.
 */
export function useCanvasHub(canvasId: string | undefined) {
  const accessToken = useAuthStore((s) => s.accessToken)
  const partyId = usePartyStore((s) => s.partyId)

  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const joinedRef = useRef(false)
  const [connected, setConnected] = useState(false)

  // JoinCanvas is the hub's authorization gate, so a failure (e.g. no party bound
  // to this canvas yet, as when you're solo) is expected and just means no group.
  const attemptJoin = useCallback(async () => {
    const conn = connectionRef.current
    if (!conn || conn.state !== signalR.HubConnectionState.Connected || !canvasId) return
    if (!usePartyStore.getState().partyId) return

    try {
      await conn.invoke('JoinCanvas', canvasId)
      joinedRef.current = true
    } catch {
      joinedRef.current = false
    }
  }, [canvasId])

  // One connection per canvas.
  useEffect(() => {
    if (!accessToken || !canvasId) return

    const conn = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/canvas', {
        accessTokenFactory: () => useAuthStore.getState().accessToken,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    conn.on('NotifyOnCursorPos', (cursor: CursorPacket) => {
      if (!cursor.userId) return
      useCursorStore.getState().upsert(cursor.userId, cursor.x, cursor.y)
    })

    // Remote drawing ops apply straight to the scene (deduped by id) and never
    // loop back out through the op channel, so there's no rebroadcast.
    conn.on('NotifyOnOperation', (op: OperationPacket) => {
      try {
        useSceneStore.getState().addRemote(JSON.parse(op.operationData) as SceneItem)
      } catch {
        // Ignore a malformed op payload rather than tearing anything down.
      }
    })

    // Broadcast this client's finished ops. Fire-and-forget (the local render already
    // happened); dropped when not joined (e.g. solo), matching the cursor path.
    const unsubscribeOps = subscribeLocalOps((op) => {
      if (!joinedRef.current || conn.state !== signalR.HubConnectionState.Connected) return
      const type = op.kind === 'stroke' && op.tool === 'eraser' ? 1 : 0
      void conn.send('SendOperation', { type, operationData: JSON.stringify(op) })
    })

    conn.onreconnected(() => {
      setConnected(true)
      joinedRef.current = false // group membership is dropped on reconnect
      void attemptJoin()
    })
    conn.onclose(() => {
      setConnected(false)
      joinedRef.current = false
    })

    connectionRef.current = conn
    conn
      .start()
      .then(() => {
        setConnected(true)
        void attemptJoin()
      })
      .catch(() => setConnected(false))

    return () => {
      joinedRef.current = false
      setConnected(false)
      unsubscribeOps()
      useCursorStore.getState().clear()
      void conn.stop()
      connectionRef.current = null
    }
  }, [accessToken, canvasId, attemptJoin])

  // Re-join once the party forms (a solo owner who invites someone only becomes
  // bound to the canvas after the party exists).
  useEffect(() => {
    if (connected && partyId) void attemptJoin()
  }, [connected, partyId, attemptJoin])

  // Stream the local cursor. World coordinates so every receiver re-projects it
  // through their own pan/zoom; integers to match the int DTO and shrink payload.
  useEffect(() => {
    if (!canvasId) return
    let lastSent = 0

    const onMove = (event: PointerEvent) => {
      const conn = connectionRef.current
      if (!joinedRef.current || conn?.state !== signalR.HubConnectionState.Connected) return

      const now = performance.now()
      if (now - lastSent < SEND_INTERVAL_MS) return
      lastSent = now

      const { x, y, scale } = useViewportStore.getState()
      const worldX = Math.round((event.clientX - x) / scale)
      const worldY = Math.round((event.clientY - y) / scale)

      // send() over invoke(): a cursor frame is fire-and-forget, no server ack.
      void conn.send('SendCursorPos', { x: worldX, y: worldY })
    }

    window.addEventListener('pointermove', onMove)
    return () => window.removeEventListener('pointermove', onMove)
  }, [canvasId])

  // Age out cursors that stopped reporting.
  useEffect(() => {
    const id = window.setInterval(
      () => useCursorStore.getState().pruneOlderThan(CURSOR_TTL_MS),
      PRUNE_INTERVAL_MS,
    )
    return () => window.clearInterval(id)
  }, [])
}
