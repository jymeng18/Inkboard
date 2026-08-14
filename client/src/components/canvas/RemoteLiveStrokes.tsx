import { memo } from 'react'
import { Line } from 'react-konva'

import { useAuthStore } from '@/stores/authStore'
import { useLiveStrokeStore } from '@/stores/liveStrokeStore'
import { strokeOutline } from './strokeGeometry'

/*
 * Other users' strokes as they're being drawn. Lives on the isolated overlay
 * layer (never the committed one), so these frequent updates only repaint here.
 * Eraser can't composite off-layer, so it shows a translucent ghost of the swept
 * path; the real cut lands when the finished op commits into the scene.
 */
function RemoteLiveStrokes() {
  const strokes = useLiveStrokeStore((s) => s.strokes)
  const currentUserId = useAuthStore((s) => s.userId)

  return (
    <>
      {Object.entries(strokes).map(([userId, stroke]) => {
        if (userId === currentUserId || stroke.points.length === 0) return null
        const erase = stroke.tool === 'eraser'

        return (
          <Line
            key={userId}
            points={strokeOutline(stroke.points, stroke.tool, stroke.size, false)}
            closed
            fill={erase ? '#2d2926' : stroke.color}
            opacity={erase ? 0.25 : 1}
            lineJoin="round"
            listening={false}
            perfectDrawEnabled={false}
          />
        )
      })}
    </>
  )
}

export default memo(RemoteLiveStrokes)
