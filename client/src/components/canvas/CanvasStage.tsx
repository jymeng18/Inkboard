import { useCallback, useEffect, useRef, useState } from 'react'
import { Layer, Stage } from 'react-konva'
import { useGesture } from '@use-gesture/react'
import type Konva from 'konva'

import { useCanvasUiStore } from '@/stores/canvasUiStore'
import { useDrawingStore } from '@/stores/drawingStore'
import { useSceneStore } from '@/stores/sceneStore'
import { useViewportStore } from '@/stores/viewportStore'
import CurrentStroke from './CurrentStroke'
import SceneShapes from './SceneShapes'
import { strokeOutline, type StrokeTool } from './strokeGeometry'

const SHAPE_STROKE_WIDTH = 3
const GRID = 24

/*
 * The live Konva stage. Drawing is driven imperatively through refs + a per-frame
 * rAF flush so pointer bursts never cause more than one render per frame, and the
 * committed layer stays untouched while you draw. Pan/zoom come from @use-gesture.
 */
export default function CanvasStage() {
  const containerRef = useRef<HTMLDivElement>(null)
  const [dims, setDims] = useState(() => ({ w: window.innerWidth, h: window.innerHeight }))

  const tool = useCanvasUiStore((s) => s.tool)
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

    if (ui.tool === 'shapes') {
      shapeStart.current = [wx, wy]
      shapeHead.current = [wx, wy]
      useDrawingStore.getState().beginShape(ui.tool, ui.color, [wx, wy])
    } else {
      const pressure = e.evt.pressure || 0.5
      pointsRef.current = [[wx, wy, pressure]]
      useDrawingStore.getState().beginStroke(ui.tool, ui.color, [wx, wy, pressure])
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
      if (useDrawingStore.getState().mode === 'shape') {
        shapeHead.current = [wx, wy]
      } else {
        pointsRef.current.push([wx, wy, e.evt.pressure || 0.5])
      }
      scheduleFlush()
    },
    [scheduleFlush],
  )

  const commit = useCallback(() => {
    const ds = useDrawingStore.getState()
    const scene = useSceneStore.getState()

    if (ds.mode === 'stroke' && pointsRef.current.length > 0) {
      const strokeTool = ds.tool as StrokeTool
      const outline = strokeOutline(pointsRef.current, strokeTool, true)
      if (outline.length >= 6) {
        scene.add({
          id: crypto.randomUUID(),
          kind: 'stroke',
          tool: strokeTool,
          color: ds.color,
          outline,
        })
      }
    } else if (ds.mode === 'shape' && shapeStart.current && shapeHead.current) {
      const [sx, sy] = shapeStart.current
      const [hx, hy] = shapeHead.current
      const width = Math.abs(hx - sx)
      const height = Math.abs(hy - sy)
      if (width > 2 && height > 2) {
        scene.add({
          id: crypto.randomUUID(),
          kind: 'rect',
          color: ds.color,
          x: Math.min(sx, hx),
          y: Math.min(sy, hy),
          width,
          height,
          strokeWidth: SHAPE_STROKE_WIDTH,
        })
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
        containerRef.current.style.cursor =
          useCanvasUiStore.getState().tool === 'hand' ? 'grab' : 'crosshair'
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
        cursor: tool === 'hand' ? 'grab' : 'crosshair',
        backgroundPosition: `${x}px ${y}px`,
        backgroundSize: `${GRID * scale}px ${GRID * scale}px`,
      }}
    >
      <img
        src="/pen.svg"
        alt=""
        aria-hidden
        className="pointer-events-none absolute top-1/2 left-1/2 h-40 -translate-x-1/2 -translate-y-1/2 -rotate-30 opacity-[0.03]"
      />
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
        </Layer>
        <Layer listening={false}>{tool !== 'eraser' && <CurrentStroke />}</Layer>
      </Stage>
    </div>
  )
}
