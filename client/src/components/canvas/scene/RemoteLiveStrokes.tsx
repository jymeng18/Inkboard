import { memo } from 'react'
import { Line } from 'react-konva'

import { useAuthStore } from '@/stores/authStore'
import { useLiveStrokeStore } from '@/stores/liveStrokeStore'
import { strokeOutline } from './strokeGeometry'

/*
 * Other users' strokes as they're being drawn, split by how they composite:
 *
 *  - `ink`: normal strokes on the isolated overlay layer, so these frequent
 *    updates only repaint there and never disturb the committed scene.
 *  - `eraser`: rendered on the committed layer (via `variant="eraser"`) with the
 *    same destination-out cut a local eraser uses, so a peer's erase actually
 *    removes ink live instead of only painting a grey ghost until they commit.
 *
 * Each variant renders only its matching strokes, so the two instances (one per
 * layer) never draw the same stroke twice.
 */
function RemoteLiveStrokes({ variant }: { variant: 'ink' | 'eraser' }) {
  const strokes = useLiveStrokeStore((s) => s.strokes)
  const currentUserId = useAuthStore((s) => s.userId)

  return (
    <>
      {Object.entries(strokes).map(([userId, stroke]) => {
        if (userId === currentUserId || stroke.points.length === 0) return null

        const erase = stroke.tool === 'eraser'
        if (erase !== (variant === 'eraser')) return null

        return (
          <Line
            key={userId}
            points={strokeOutline(stroke.points, stroke.tool, stroke.size, false)}
            closed
            fill={erase ? '#000' : stroke.color}
            globalCompositeOperation={erase ? 'destination-out' : undefined}
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
