import { memo } from 'react'
import { Line, Rect } from 'react-konva'

import { useSceneStore } from '@/stores/sceneStore'

/*
 * Every committed item. Memoized and self-subscribed, so it only re-renders when
 * the scene actually changes (a commit / undo / redo) — never during a stroke or
 * a pan. listening=false skips hit-detection we don't need.
 */
function SceneShapes() {
  const items = useSceneStore((s) => s.items)

  return (
    <>
      {items.map((item) =>
        item.kind === 'stroke' ? (
          <Line
            key={item.id}
            points={item.outline}
            closed
            fill={item.tool === 'eraser' ? '#000' : item.color}
            globalCompositeOperation={item.tool === 'eraser' ? 'destination-out' : undefined}
            lineJoin="round"
            listening={false}
            perfectDrawEnabled={false}
          />
        ) : (
          <Rect
            key={item.id}
            x={item.x}
            y={item.y}
            width={item.width}
            height={item.height}
            stroke={item.color}
            strokeWidth={item.strokeWidth}
            lineJoin="round"
            listening={false}
            perfectDrawEnabled={false}
          />
        ),
      )}
    </>
  )
}

export default memo(SceneShapes)
