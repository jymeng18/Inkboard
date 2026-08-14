import { useCallback, useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'

import { subscribeLocalOps } from '@/lib/opChannel'
import { useAuthStore } from '@/stores/authStore'
import { useCursorStore } from '@/stores/cursorStore'
import { useDrawingStore } from '@/stores/drawingStore'
import { useLiveStrokeStore, type LiveStrokeFrame } from '@/stores/liveStrokeStore'
import { usePartyStore } from '@/stores/partyStore'
import { useSceneStore, type SceneItem } from '@/stores/sceneStore'
import { useViewportStore } from '@/stores/viewportStore'

// Cap outbound cursor frames. Raw pointermove fires ~120/sec; this keeps the
// wire (and the receivers) sane while staying well under the threshold of
// perceptible lag. Matches the ~30ms-50ms guidance in the cursor-tracking doc.
const SEND_INTERVAL_MS = 45

// Cap outbound in-progress stroke frames, same idea as cursors.
const LIVE_STROKE_SEND_MS = 40

// A cursor / live stroke we haven't heard from in this long is treated as gone.
const CURSOR_TTL_MS = 8000
const LIVE_STROKE_TTL_MS = 5000
const PRUNE_INTERVAL_MS = 2000

interface CursorPacket {
  userId?: string
  x: number
  y: number
}

// The server relays these envelopes; we only read the opaque payload it never parses.
interface OperationPacket {
  userId?: string
  operationData: string
}

interface LiveStrokePacket {
  userId?: string
  data: string
}

/*
 * Live sync for one canvas over the CanvasHub. Connects, joins the canvas group
 * (the hub's single auth checkpoint), and runs three feeds: the local cursor
 * (throttled), in-progress strokes streamed as they're drawn, and finished ops
 * broadcast/applied on commit.
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
    // loop back out through the op channel, so there's no rebroadcast. The commit
    // also finalizes: drop that user's live preview, now superseded by the op.
    conn.on('NotifyOnOperation', (op: OperationPacket) => {
      try {
        useSceneStore.getState().addRemote(JSON.parse(op.operationData) as SceneItem)
        if (op.userId) useLiveStrokeStore.getState().remove(op.userId)
      } catch {
        // Ignore a malformed op payload rather than tearing anything down.
      }
    })

    // A peer's in-progress stroke frame: begin a new one or append to theirs.
    conn.on('NotifyOnLiveStroke', (msg: LiveStrokePacket) => {
      if (!msg.userId) return
      try {
        useLiveStrokeStore.getState().apply(msg.userId, JSON.parse(msg.data) as LiveStrokeFrame)
      } catch {
        // Ignore a malformed live frame.
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
      useLiveStrokeStore.getState().clear()
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

  // Stream the local in-progress stroke as throttled, incremental point batches.
  // drawingStore already holds the live stroke; we just forward the new tail. The
  // committed op is authoritative, so any batch skipped by the throttle is covered.
  useEffect(() => {
    if (!canvasId) return
    let currentId: string | null = null
    let sentCount = 0
    let lastSent = 0

    return useDrawingStore.subscribe((ds) => {
      if (ds.mode !== 'stroke' || !ds.id) {
        currentId = null
        sentCount = 0
        return
      }
      const conn = connectionRef.current
      if (!joinedRef.current || conn?.state !== signalR.HubConnectionState.Connected) return

      if (ds.id !== currentId) {
        currentId = ds.id
        sentCount = 0
        lastSent = 0
      }
      const now = performance.now()
      if (now - lastSent < LIVE_STROKE_SEND_MS) return

      const batch = ds.points.slice(sentCount)
      if (batch.length === 0) return
      sentCount = ds.points.length
      lastSent = now

      void conn.send('SendLiveStroke', {
        data: JSON.stringify({ id: ds.id, tool: ds.tool, color: ds.color, size: ds.size, points: batch }),
      })
    })
  }, [canvasId])

  // Age out cursors and live strokes that stopped reporting.
  useEffect(() => {
    const id = window.setInterval(() => {
      useCursorStore.getState().pruneOlderThan(CURSOR_TTL_MS)
      useLiveStrokeStore.getState().pruneOlderThan(LIVE_STROKE_TTL_MS)
    }, PRUNE_INTERVAL_MS)
    return () => window.clearInterval(id)
  }, [])
}
