import { memo, useMemo } from 'react'
import { Line } from 'react-konva'

import { useSceneStore, type StrokeItem } from '@/stores/sceneStore'
import ShapeNode from './ShapeNode'
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
      {items.map((item) => {
        switch (item.kind) {
          case 'stroke':
            return <StrokeShape key={item.id} item={item} />
          case 'shape':
            return (
              <ShapeNode
                key={item.id}
                shape={item.shape}
                ax={item.x}
                ay={item.y}
                bx={item.x + item.width}
                by={item.y + item.height}
                color={item.color}
                strokeWidth={item.strokeWidth}
              />
            )
          case 'line':
            return (
              <ShapeNode
                key={item.id}
                shape="line"
                ax={item.x1}
                ay={item.y1}
                bx={item.x2}
                by={item.y2}
                color={item.color}
                strokeWidth={item.strokeWidth}
              />
            )
          // Legacy rectangle op predating the generalized shape.
          case 'rect':
            return (
              <ShapeNode
                key={item.id}
                shape="rectangle"
                ax={item.x}
                ay={item.y}
                bx={item.x + item.width}
                by={item.y + item.height}
                color={item.color}
                strokeWidth={item.strokeWidth}
              />
            )
        }
      })}
    </>
  )
}

export default memo(SceneShapes)
