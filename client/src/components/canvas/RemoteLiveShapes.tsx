import { memo } from 'react'

import { useAuthStore } from '@/stores/authStore'
import { useLiveShapeStore } from '@/stores/liveShapeStore'
import ShapeNode from './ShapeNode'

/*
 * Other users' shapes and ruler lines as they drag them out. Lives on the
 * isolated overlay layer like RemoteLiveStrokes, and mirrors the local dashed
 * preview so a peer sees the shape forming, then the solid committed op replaces
 * it on release.
 */
function RemoteLiveShapes() {
  const shapes = useLiveShapeStore((s) => s.shapes)
  const currentUserId = useAuthStore((s) => s.userId)

  return (
    <>
      {Object.entries(shapes).map(([userId, live]) => {
        if (userId === currentUserId) return null

        return (
          <ShapeNode
            key={userId}
            shape={live.shape}
            ax={live.start[0]}
            ay={live.start[1]}
            bx={live.head[0]}
            by={live.head[1]}
            color={live.color}
            strokeWidth={2}
            dash={[8, 5]}
          />
        )
      })}
    </>
  )
}

export default memo(RemoteLiveShapes)
