import { memo } from 'react'
import { Line, Rect } from 'react-konva'

import { useDrawingStore } from '@/stores/drawingStore'
import { strokeOutline, type StrokeTool } from './strokeGeometry'

const STROKE_TOOLS: StrokeTool[] = ['pencil', 'brush', 'eraser']

/*
 * The in-progress stroke/shape. The only subscriber to drawingStore, so it's the
 * one component that re-renders per frame while drawing.
 */
function CurrentStroke() {
  const mode = useDrawingStore((s) => s.mode)
  const tool = useDrawingStore((s) => s.tool)
  const color = useDrawingStore((s) => s.color)
  const size = useDrawingStore((s) => s.size)
  const points = useDrawingStore((s) => s.points)
  const start = useDrawingStore((s) => s.start)
  const head = useDrawingStore((s) => s.head)

  if (mode === 'stroke' && STROKE_TOOLS.includes(tool as StrokeTool) && points.length > 0) {
    const strokeTool = tool as StrokeTool
    const erase = strokeTool === 'eraser'
    return (
      <Line
        points={strokeOutline(points, strokeTool, size, false)}
        closed
        fill={erase ? '#000' : color}
        globalCompositeOperation={erase ? 'destination-out' : undefined}
        lineJoin="round"
        listening={false}
        perfectDrawEnabled={false}
      />
    )
  }

  if (mode === 'shape' && start && head) {
    return (
      <Rect
        x={Math.min(start[0], head[0])}
        y={Math.min(start[1], head[1])}
        width={Math.abs(head[0] - start[0])}
        height={Math.abs(head[1] - start[1])}
        stroke={color}
        strokeWidth={2}
        dash={[8, 5]}
        listening={false}
        perfectDrawEnabled={false}
      />
    )
  }

  return null
}

export default memo(CurrentStroke)
