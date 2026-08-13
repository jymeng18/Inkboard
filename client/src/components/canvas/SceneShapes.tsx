import { memo, useMemo } from 'react'
import { Line, Rect } from 'react-konva'

import { useSceneStore, type StrokeItem } from '@/stores/sceneStore'
import { strokeOutline } from './strokeGeometry'

/*
 * One committed stroke. The perfect-freehand outline is derived from the stored
 * raw points here, memoized on the item so it computes once, and the whole
 * component is memoized so an unchanged stroke never recomputes when the scene
 * changes around it (committed item objects keep a stable reference).
 */
const StrokeShape = memo(function StrokeShape({ item }: { item: StrokeItem }) {
  const outline = useMemo(
    () => strokeOutline(item.points, item.tool, item.size, true),
    [item.points, item.tool, item.size],
  )
  const erase = item.tool === 'eraser'

  return (
    <Line
      points={outline}
      closed
      fill={erase ? '#000' : item.color}
      globalCompositeOperation={erase ? 'destination-out' : undefined}
      lineJoin="round"
      listening={false}
      perfectDrawEnabled={false}
    />
  )
})

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
          <StrokeShape key={item.id} item={item} />
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
