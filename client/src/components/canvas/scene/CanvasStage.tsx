import { useCallback, useEffect, useRef, useState } from 'react'
import { Layer, Stage } from 'react-konva'
import { useGesture } from '@use-gesture/react'
import type Konva from 'konva'

import { publishLocalOp } from '@/lib/opChannel'
import { useCanvasUiStore } from '@/stores/canvasUiStore'
import { useDrawingStore } from '@/stores/drawingStore'
import { useSceneStore, type SceneItem } from '@/stores/sceneStore'
import { useViewportStore } from '@/stores/viewportStore'
import CurrentStroke from './CurrentStroke'
import RemoteLiveShapes from './RemoteLiveShapes'
import RemoteLiveStrokes from './RemoteLiveStrokes'
import SceneShapes from './SceneShapes'
import { cursorForTool } from './cursors'
import type { StrokeTool } from './strokeGeometry'

const MIN_POINT_DISTANCE = 2.15
const SHAPE_STROKE_WIDTH = 3
const GRID = 24

// Snap a ruler line's head onto the nearest 0/45/90 axis from its start, keeping
// the drawn length. Used while Shift is held.
function snapToAngle(
  [sx, sy]: [number, number],
  [hx, hy]: [number, number],
): [number, number] {
  const dx = hx - sx
  const dy = hy - sy
  const length = Math.hypot(dx, dy)
  const step = Math.PI / 4
  const angle = Math.round(Math.atan2(dy, dx) / step) * step
  return [sx + Math.cos(angle) * length, sy + Math.sin(angle) * length]
}

/*
 * The live Konva stage. Drawing is driven imperatively through refs + a per-frame
 * rAF flush so pointer bursts never cause more than one render per frame, and the
 * committed layer stays untouched while you draw. Pan/zoom come from @use-gesture.
 */
export default function CanvasStage() {
  const containerRef = useRef<HTMLDivElement>(null)
  const [dims, setDims] = useState(() => ({ w: window.innerWidth, h: window.innerHeight }))

  const tool = useCanvasUiStore((s) => s.tool)
  const size = useCanvasUiStore((s) => s.size)
  const x = useViewportStore((s) => s.x)
  const y = useViewportStore((s) => s.y)
  const scale = useViewportStore((s) => s.scale)

  // Hot path scratch state, mutated without triggering renders.
  const drawing = useRef(false)
  const pointsRef = useRef<number[][]>([])
  const shapeStart = useRef<[number, number] | null>(null)
  const shapeHead = useRef<[number, number] | null>(null)
  const rafRef = useRef<number | null>(null)
  const panning = useRef(false)
  const lastPan = useRef<[number, number] | null>(null)

  useEffect(() => {
    const onResize = () => setDims({ w: window.innerWidth, h: window.innerHeight })
    window.addEventListener('resize', onResize)
    return () => window.removeEventListener('resize', onResize)
  }, [])

  const toWorld = (stage: Konva.Stage): [number, number] => {
    const pointer = stage.getPointerPosition()
    if (!pointer) return [0, 0]
    const transform = stage.getAbsoluteTransform().copy()
    transform.invert()
    const world = transform.point(pointer)
    return [world.x, world.y]
  }

  // Coalesce all moves within a frame into a single store update.
  const scheduleFlush = useCallback(() => {
    if (rafRef.current !== null) return
    rafRef.current = requestAnimationFrame(() => {
      rafRef.current = null
      const ds = useDrawingStore.getState()
      if (ds.mode === 'stroke') ds.setPoints(pointsRef.current.slice())
      else if (ds.mode === 'shape' && shapeHead.current) ds.setHead(shapeHead.current)
    })
  }, [])

  const handleDown = useCallback((e: Konva.KonvaEventObject<PointerEvent>) => {
    const ui = useCanvasUiStore.getState()

    // Middle mouse button pans, whatever tool is active.
    if (e.evt.button === 1) {
      e.evt.preventDefault()
      panning.current = true
      lastPan.current = [e.evt.clientX, e.evt.clientY]
      if (containerRef.current) containerRef.current.style.cursor = 'grabbing'
      return
    }

    if (ui.tool === 'hand') return
    const stage = e.target.getStage()
    if (!stage) return

    const [wx, wy] = toWorld(stage)
    drawing.current = true

    if (ui.tool === 'shapes' || ui.tool === 'ruler') {
      const shape = ui.tool === 'ruler' ? 'line' : ui.shapeKind
      shapeStart.current = [wx, wy]
      shapeHead.current = [wx, wy]
      useDrawingStore.getState().beginShape(shape, ui.color, [wx, wy])
    } else {
      // 'brush' is a family; the active variant is the concrete geometry stored
      // on the stroke so it replays identically everywhere.
      const geom: StrokeTool = ui.tool === 'brush' ? ui.brushVariant : (ui.tool as StrokeTool)
      const pressure = e.evt.pressure || 0.5

      const point: [number, number, number] = [
        Math.round(wx * 100) / 100,
        Math.round(wy * 100) / 100,
        Math.round(pressure * 1000) / 1000,
      ];

      pointsRef.current = [point]
      // Id assigned here so the live-stream frames and the final committed op
      // carry the same id.
      useDrawingStore.getState().beginStroke(crypto.randomUUID(), geom, ui.color, ui.size, point)
    }
  }, [])

  const handleMove = useCallback(
    (e: Konva.KonvaEventObject<PointerEvent>) => {
      if (panning.current) {
        const last = lastPan.current
        if (last) useViewportStore.getState().panBy(e.evt.clientX - last[0], e.evt.clientY - last[1])
        lastPan.current = [e.evt.clientX, e.evt.clientY]
        return
      }
      if (!drawing.current) return
      const stage = e.target.getStage()
      if (!stage) return

      const [wx, wy] = toWorld(stage)
      const ds = useDrawingStore.getState()
      if (ds.mode === 'shape') {
        // Shift constrains the ruler line to 45 increments.
        shapeHead.current =
          ds.shape === 'line' && e.evt.shiftKey && shapeStart.current
            ? snapToAngle(shapeStart.current, [wx, wy])
            : [wx, wy]
      } else {
          const pressure = e.evt.pressure || 0.5;
          const points = pointsRef.current;
          const last = points[points.length - 1];

          const dx = wx - last[0];
          const dy = wy - last[1];

          if (dx * dx + dy * dy >= MIN_POINT_DISTANCE * MIN_POINT_DISTANCE) {
            points.push([
              Math.round(wx * 100) / 100, 
              Math.round(wy * 100) / 100, 
              Math.round(pressure * 1000) / 1000
            ]);
          }
      }
      scheduleFlush()
    },
    [scheduleFlush],
  )

  const commit = useCallback(() => {
    const ds = useDrawingStore.getState()
    const scene = useSceneStore.getState()

    if (ds.mode === 'stroke' && pointsRef.current.length > 0) {
      // Store the raw input, not the outline: this is the operation we'll
      // broadcast and persist, and each client renders its own outline from it.
      const item: SceneItem = {
        id: ds.id ?? crypto.randomUUID(),
        kind: 'stroke',
        tool: ds.tool as StrokeTool,
        color: ds.color,
        size: ds.size,
        points: pointsRef.current.slice(),
      }
      scene.add(item)
      publishLocalOp(item)
    } else if (ds.mode === 'shape' && ds.shape && shapeStart.current && shapeHead.current) {
      const shape = ds.shape
      const [sx, sy] = shapeStart.current
      const [hx, hy] = shapeHead.current

      if (shape === 'line') {
        // A line only needs enough length to be a deliberate mark, not a stray click.
        if (Math.hypot(hx - sx, hy - sy) > 2) {
          const item: SceneItem = {
            id: crypto.randomUUID(),
            kind: "line",
            color: ds.color,
            x1: Math.round(sx * 100) / 100,
            y1: Math.round(sy * 100) / 100,
            x2: Math.round(hx * 100) / 100,
            y2: Math.round(hy * 100) / 100,
            strokeWidth: SHAPE_STROKE_WIDTH,
          };
          scene.add(item)
          publishLocalOp(item)
        }
      } else {
        const width = Math.abs(hx - sx)
        const height = Math.abs(hy - sy)
        if (width > 2 && height > 2) {
          const item: SceneItem = {
            id: crypto.randomUUID(),
            shape,
            kind: 'shape',
            color: ds.color,
            x: Math.round(Math.min(sx, hx) * 100) / 100,
            y: Math.round(Math.min(sy, hy) * 100) / 100,
            width: Math.round(width * 100) / 100,
            height: Math.round(height * 100) / 100,
            strokeWidth: SHAPE_STROKE_WIDTH,
          }
          scene.add(item)
          publishLocalOp(item)
        }
      }
    }

    ds.reset()
    pointsRef.current = []
    shapeStart.current = null
    shapeHead.current = null
  }, [])

  const handleUp = useCallback(() => {
    if (panning.current) {
      panning.current = false
      lastPan.current = null
      if (containerRef.current) {
        const ui = useCanvasUiStore.getState()
        const zoom = useViewportStore.getState().scale
        containerRef.current.style.cursor = cursorForTool(ui.tool, ui.size * zoom)
      }
      return
    }
    if (!drawing.current) return
    drawing.current = false
    if (rafRef.current !== null) {
      cancelAnimationFrame(rafRef.current)
      rafRef.current = null
    }
    commit()
  }, [commit])

  useGesture(
    {
      onDrag: ({ delta: [dx, dy], pinching, cancel }) => {
        if (pinching) {
          cancel()
          return
        }
        useViewportStore.getState().panBy(dx, dy)
      },
      onWheel: ({ event }) => {
        event.preventDefault()
        const viewport = useViewportStore.getState()
        const rect = containerRef.current?.getBoundingClientRect()
        const center = { x: event.clientX - (rect?.left ?? 0), y: event.clientY - (rect?.top ?? 0) }
        const factor = event.deltaY < 0 ? 1.1 : 1 / 1.1
        viewport.zoomTo(viewport.scale * factor, center)
      },
      onPinch: ({ offset: [s], origin: [ox, oy], memo }) => {
        const base = (memo as number) ?? useViewportStore.getState().scale
        const rect = containerRef.current?.getBoundingClientRect()
        useViewportStore
          .getState()
          .zoomTo(base * s, { x: ox - (rect?.left ?? 0), y: oy - (rect?.top ?? 0) })
        return base
      },
    },
    {
      target: containerRef,
      eventOptions: { passive: false },
      drag: { enabled: tool === 'hand', filterTaps: true },
      pinch: { scaleBounds: { min: 0.1, max: 10 } },
    },
  )

  return (
    <div
      ref={containerRef}
      className="absolute inset-0 z-0 bg-surface canvas-bg"
      onMouseDown={(e) => {
        if (e.button === 1) e.preventDefault()
      }}
      style={{
        touchAction: 'none',
        cursor: cursorForTool(tool, size * scale),
        backgroundPosition: `${x}px ${y}px`,
        backgroundSize: `${GRID * scale}px ${GRID * scale}px`,
      }}
    >
      <Stage
        width={dims.w}
        height={dims.h}
        x={x}
        y={y}
        scaleX={scale}
        scaleY={scale}
        onPointerDown={handleDown}
        onPointerMove={handleMove}
        onPointerUp={handleUp}
        onPointerLeave={handleUp}
      >
        <Layer>
          <SceneShapes />
          {tool === 'eraser' && <CurrentStroke />}
          {/* Peers' erasers cut the committed scene live, same as a local eraser. */}
          <RemoteLiveStrokes variant="eraser" />
        </Layer>
        <Layer listening={false}>
          {tool !== 'eraser' && <CurrentStroke />}
          <RemoteLiveStrokes variant="ink" />
          <RemoteLiveShapes />
        </Layer>
      </Stage>
    </div>
  )
}
