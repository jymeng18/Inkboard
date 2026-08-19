import { memo } from 'react'
import { Line } from 'react-konva'

import { useDrawingStore } from '@/stores/drawingStore'
import ShapeNode from './ShapeNode'
import { strokeOutline, type StrokeTool } from './strokeGeometry'

const STROKE_TOOLS: StrokeTool[] = ['pencil', 'brush', 'marker', 'calligraphy', 'eraser']

/*
 * The in-progress stroke/shape. The only subscriber to drawingStore, so it's the
 * one component that re-renders per frame while drawing.
 */
function CurrentStroke() {
  const mode = useDrawingStore((s) => s.mode)
  const tool = useDrawingStore((s) => s.tool)
  const shape = useDrawingStore((s) => s.shape)
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

  if (mode === 'shape' && shape && start && head) {
    return (
      <ShapeNode
        shape={shape}
        ax={start[0]}
        ay={start[1]}
        bx={head[0]}
        by={head[1]}
        color={color}
        strokeWidth={2}
        dash={[8, 5]}
      />
    )
  }

  return null
}

export default memo(CurrentStroke)
