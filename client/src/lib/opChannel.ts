import type { SceneItem } from '@/stores/sceneStore'

/*
 * A tiny in-process bus for locally-committed canvas operations. It decouples the
 * drawing commit (the emit hook) from the transport: the Konva stage publishes a
 * finished op here and never learns about SignalR, while useCanvasHub subscribes
 * and broadcasts. Remote ops are applied straight to sceneStore and never come
 * back through here, so there's no rebroadcast loop.
 */
type OpListener = (op: SceneItem) => void

const listeners = new Set<OpListener>()

export function publishLocalOp(op: SceneItem): void {
  for (const listener of listeners) listener(op)
}

export function subscribeLocalOps(listener: OpListener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
