import { Ellipse, Line, Rect, Star } from 'react-konva'

import type { DrawShape } from '@/stores/drawingStore'
import { boxMetrics, diamondPoints, starRadii, trianglePoints } from './shapeGeometry'

/*
 * The single source of truth for how each outline shape is drawn, shared by the
 * committed scene and the in-progress preview so the two can never drift. Takes
 * two points: a bounding-box corner pair for the box shapes, or the real
 * endpoints for a line. Everything is a stroked outline, no fill.
 */
interface ShapeNodeProps {
  shape: DrawShape
  ax: number
  ay: number
  bx: number
  by: number
  color: string
  strokeWidth: number
  /* Dashed while dragging, solid once committed. */
  dash?: number[]
}

export default function ShapeNode({ shape, ax, ay, bx, by, color, strokeWidth, dash }: ShapeNodeProps) {
  const common = {
    stroke: color,
    strokeWidth,
    dash,
    lineJoin: 'round' as const,
    listening: false,
    perfectDrawEnabled: false,
  }

  if (shape === 'line') {
    return <Line points={[ax, ay, bx, by]} lineCap="round" {...common} />
  }

  const m = boxMetrics(ax, ay, bx, by)

  switch (shape) {
    case 'rectangle':
      return <Rect x={m.minX} y={m.minY} width={m.width} height={m.height} {...common} />
    case 'ellipse':
      return <Ellipse x={m.cx} y={m.cy} radiusX={m.width / 2} radiusY={m.height / 2} {...common} />
    case 'triangle':
      return <Line points={trianglePoints(m)} closed {...common} />
    case 'diamond':
      return <Line points={diamondPoints(m)} closed {...common} />
    case 'star': {
      const { inner, outer } = starRadii(m)
      return <Star x={m.cx} y={m.cy} numPoints={5} innerRadius={inner} outerRadius={outer} {...common} />
    }
  }
}
