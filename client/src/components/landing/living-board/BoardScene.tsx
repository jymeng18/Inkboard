import { useCallback, useEffect, useRef, useState } from "react";
import { Canvas, useFrame, type ThreeEvent } from "@react-three/fiber";
import { Html, RoundedBox } from "@react-three/drei";
import { MousePointer2, Pencil } from "lucide-react";
import type { Group } from "three";
import {
  BOARD_H,
  BOARD_W,
  BoardSurface,
  MARKER_COLORS,
  uvToPixels,
} from "./boardDrawing";
import { createGhosts } from "./ghosts";

/* World-space sizing for the slab. Everything else is derived from these. */
const PLANE_W = 4.4;
const PLANE_H = PLANE_W * (BOARD_H / BOARD_W);
const DEPTH = 0.34;
const PAPER_Z = DEPTH / 2 + 0.002;

/* Resting pose: a pleasing 3/4 tilt (the angle the board used to reach only
 * when the cursor hit the bottom-left corner). The pointer nudges it gently
 * around this base instead of the board sitting flat until you move the mouse. */
const BASE_TILT_X = 0.16;
const BASE_TILT_Y = -0.22;
const TILT_AMP_X = 0.1;
const TILT_AMP_Y = 0.12;

function pixelToLocal(px: number, py: number): [number, number] {
  return [(px / BOARD_W - 0.5) * PLANE_W, (0.5 - py / BOARD_H) * PLANE_H];
}

interface BoardSceneProps {
  /* Off-screen? Freeze the render loop instead of tearing the context down. */
  active: boolean;
}

export default function BoardScene({ active }: BoardSceneProps) {
  return (
    <Canvas
      flat
      dpr={[1, 1.5]}
      frameloop={active ? "always" : "never"}
      camera={{ position: [0, 0.05, 6.1], fov: 33 }}
      gl={{ antialias: true, alpha: true, powerPreference: "high-performance" }}
      style={{ cursor: "crosshair", touchAction: "none" }}
    >
      <Board />
    </Canvas>
  );
}

function Board() {
  // Lazy state initialisers keep one stable instance for the component's life,
  // which is legal to read during render (a ref's .current is not).
  const [surface] = useState(() => new BoardSurface());
  const [ghosts] = useState(() => createGhosts(surface));

  const groupRef = useRef<Group>(null);
  const ghostGroups = useRef<(Group | null)[]>([]);

  const drawing = useRef(false);
  const penColor = useRef<string>(MARKER_COLORS[0]);
  const [activeColor, setActiveColor] = useState<string>(MARKER_COLORS[0]);
  const [hasDrawn, setHasDrawn] = useState(false);

  useEffect(() => () => surface.dispose(), [surface]);

  const beginStroke = useCallback(
    (e: ThreeEvent<PointerEvent>) => {
      if (!e.uv) return;
      e.stopPropagation();
      drawing.current = true;
      const [x, y] = uvToPixels(e.uv.x, e.uv.y);
      surface.start("user", penColor.current, 16);
      surface.addPoint("user", x, y);
      setHasDrawn(true);
    },
    [surface],
  );

  const extendStroke = useCallback(
    (e: ThreeEvent<PointerEvent>) => {
      if (!drawing.current || !e.uv) return;
      const [x, y] = uvToPixels(e.uv.x, e.uv.y);
      surface.addPoint("user", x, y);
    },
    [surface],
  );

  const endStroke = useCallback(() => {
    if (!drawing.current) return;
    drawing.current = false;
    surface.end("user");
  }, [surface]);

  useFrame((state, delta) => {
    const now = performance.now();
    const group = groupRef.current;
    if (group) {
      group.rotation.y +=
        (BASE_TILT_Y + state.pointer.x * TILT_AMP_Y - group.rotation.y) * 0.06;
      group.rotation.x +=
        (BASE_TILT_X - state.pointer.y * TILT_AMP_X - group.rotation.x) * 0.06;
      group.position.y = Math.sin(state.clock.elapsedTime * 1.1) * 0.05;
    }
    ghosts.forEach((ghost, i) => {
      ghost.step(delta, now);
      const g = ghostGroups.current[i];
      if (g) {
        const [x, y] = pixelToLocal(ghost.current[0], ghost.current[1]);
        g.position.set(x, y, PAPER_Z + 0.03);
      }
    });
    surface.render();
  });

  return (
    <group ref={groupRef} rotation={[0, 0, -0.03]}>
      {/* Hard offset shadow, the sticker drop given real depth behind the slab */}
      <RoundedBox
        args={[PLANE_W + 0.16, PLANE_H + 0.16, DEPTH]}
        radius={0.12}
        smoothness={3}
        position={[0.2, -0.2, -0.34]}
      >
        <meshBasicMaterial color="#2d2926" toneMapped={false} />
      </RoundedBox>

      {/* Ink border / body of the board */}
      <RoundedBox
        args={[PLANE_W + 0.16, PLANE_H + 0.16, DEPTH]}
        radius={0.12}
        smoothness={3}
      >
        <meshBasicMaterial color="#2d2926" toneMapped={false} />
      </RoundedBox>

      {/* The paper you actually draw on */}
      <mesh
        position={[0, 0, PAPER_Z]}
        onPointerDown={beginStroke}
        onPointerMove={extendStroke}
        onPointerUp={endStroke}
        onPointerOut={endStroke}
      >
        <planeGeometry args={[PLANE_W, PLANE_H]} />
        <meshBasicMaterial map={surface.texture} toneMapped={false} />
      </mesh>

      {/* Pen colour picker, anchored to the board's top-left corner */}
      <Html
        position={[-PLANE_W / 2 + 0.34, PLANE_H / 2 - 0.26, PAPER_Z + 0.05]}
        center
        zIndexRange={[30, 0]}
      >
        <div className="flex gap-1.5 rounded-full border-[3px] border-outline bg-surface px-2 py-1.5 sticker-shadow-sm">
          {MARKER_COLORS.map((c) => (
            <button
              key={c}
              type="button"
              onClick={() => {
                penColor.current = c;
                setActiveColor(c);
              }}
              aria-label={`Use the ${c} marker`}
              className={`size-5 rounded-full border-2 border-outline transition-transform ${
                activeColor === c
                  ? "scale-110 ring-2 ring-outline ring-offset-1"
                  : "hover:scale-105"
              }`}
              style={{ background: c }}
            />
          ))}
        </div>
      </Html>

      {/* Ghost collaborators */}
      {ghosts.map((ghost, i) => (
        <group
          key={ghost.id}
          ref={(el) => {
            ghostGroups.current[i] = el;
          }}
        >
          <Html center style={{ pointerEvents: "none" }} zIndexRange={[20, 0]}>
            <div
              className="flex translate-x-3 translate-y-1 items-center gap-1 whitespace-nowrap rounded-full border-2 border-outline px-2 py-0.5 text-white sticker-shadow-sm"
              style={{ background: ghost.color }}
            >
              <MousePointer2 className="size-3" aria-hidden />
              <span className="font-label text-[10px] font-bold">
                {ghost.name}
              </span>
            </div>
          </Html>
        </group>
      ))}

      {/* First-run nudge, gone the moment you leave a mark */}
      {!hasDrawn && (
        <Html
          position={[0, -PLANE_H / 2 + 0.34, PAPER_Z + 0.05]}
          center
          style={{ pointerEvents: "none" }}
          zIndexRange={[25, 0]}
        >
          <div className="flex animate-float items-center gap-2 whitespace-nowrap rounded-full border-2 border-outline bg-primary-container px-3 py-1 font-handwriting text-lg text-outline sticker-shadow-sm">
            <Pencil className="size-4" aria-hidden />
            psst &mdash; drag right here to draw
          </div>
        </Html>
      )}
    </group>
  );
}
