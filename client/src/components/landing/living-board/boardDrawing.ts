import { getStroke } from "perfect-freehand";
import { CanvasTexture, LinearFilter, SRGBColorSpace } from "three";

/*
 * The board's drawing surface lives entirely on a 2D canvas that Three samples
 * as a texture. Keeping the ink in a texture (instead of per-stroke geometry)
 * means the whole board stays a couple of draw calls no matter how much gets
 * scribbled, which is what keeps it cheap enough to leave running in a hero.
 */

export const BOARD_W = 1200;
export const BOARD_H = 760;

export const MARKER_COLORS = ["#ff7070", "#ffd23f", "#00c1fd"] as const;
const OUTLINE = "#2d2926";
const PAPER = "#faf9f6";
const GRID_DOT = "#dcd9d9";

/* Finished strokes are baked into the base layer; older ink fades once the
 * board gets crowded so an always-on hero never accumulates forever. */
const MAX_COMMITTED = 26;

type Point = [number, number];

interface Stroke {
  points: Point[];
  color: string;
  size: number;
}

function freehandPath(points: Point[], size: number): Path2D | null {
  if (points.length === 0) return null;
  const outline = getStroke(points, {
    size,
    thinning: 0.55,
    smoothing: 0.62,
    streamline: 0.55,
    last: true,
  });
  if (outline.length < 2) return null;
  const path = new Path2D();
  path.moveTo(outline[0][0], outline[0][1]);
  for (let i = 1; i < outline.length; i++) {
    path.lineTo(outline[i][0], outline[i][1]);
  }
  path.closePath();
  return path;
}

/* Every stroke is drawn twice: a fat ink halo, then the color on top. That is
 * the same outlined-sticker treatment the rest of the site uses, just baked
 * into pixels instead of a box-shadow. */
function paintStroke(ctx: CanvasRenderingContext2D, stroke: Stroke) {
  const fill = freehandPath(stroke.points, stroke.size);
  if (!fill) return;
  const halo = freehandPath(stroke.points, stroke.size + 9);
  if (halo) {
    ctx.fillStyle = OUTLINE;
    ctx.fill(halo);
  }
  ctx.fillStyle = stroke.color;
  ctx.fill(fill);
}

function paintPaper(ctx: CanvasRenderingContext2D) {
  ctx.fillStyle = PAPER;
  ctx.fillRect(0, 0, BOARD_W, BOARD_H);
  ctx.fillStyle = GRID_DOT;
  const step = 34;
  for (let y = step; y < BOARD_H; y += step) {
    for (let x = step; x < BOARD_W; x += step) {
      ctx.beginPath();
      ctx.arc(x, y, 1.4, 0, Math.PI * 2);
      ctx.fill();
    }
  }
}

export class BoardSurface {
  readonly texture: CanvasTexture;
  private base: CanvasRenderingContext2D;
  private view: CanvasRenderingContext2D;
  private active = new Map<string, Stroke>();
  private committed = 0;
  private dirty = true;

  constructor() {
    const baseCanvas = document.createElement("canvas");
    baseCanvas.width = BOARD_W;
    baseCanvas.height = BOARD_H;
    this.base = baseCanvas.getContext("2d")!;
    paintPaper(this.base);

    const viewCanvas = document.createElement("canvas");
    viewCanvas.width = BOARD_W;
    viewCanvas.height = BOARD_H;
    this.view = viewCanvas.getContext("2d")!;

    this.texture = new CanvasTexture(viewCanvas);
    this.texture.colorSpace = SRGBColorSpace;
    this.texture.minFilter = LinearFilter;
    this.texture.magFilter = LinearFilter;
  }

  start(id: string, color: string, size: number) {
    this.active.set(id, { points: [], color, size });
  }

  addPoint(id: string, x: number, y: number) {
    this.active.get(id)?.points.push([x, y]);
    this.dirty = true;
  }

  end(id: string) {
    const stroke = this.active.get(id);
    this.active.delete(id);
    if (!stroke || stroke.points.length < 2) {
      this.dirty = true;
      return;
    }
    // Once the board fills up, wash the oldest ink out with a translucent
    // sheet of paper before baking the new stroke in.
    if (this.committed >= MAX_COMMITTED) {
      this.base.globalAlpha = 0.14;
      paintPaper(this.base);
      this.base.globalAlpha = 1;
      this.committed = Math.floor(MAX_COMMITTED / 2);
    }
    paintStroke(this.base, stroke);
    this.committed++;
    this.dirty = true;
  }

  isDrawing(id: string) {
    return this.active.has(id);
  }

  /* Called once per frame: only re-composite when something actually moved. */
  render() {
    if (!this.dirty) return;
    this.view.drawImage(this.base.canvas, 0, 0);
    this.active.forEach((stroke) => paintStroke(this.view, stroke));
    this.texture.needsUpdate = true;
    this.dirty = false;
  }

  dispose() {
    this.texture.dispose();
  }
}

/* Board texture pixels <-> the plane's UV coords. Plane UV origin is the
 * bottom-left; the canvas origin is the top-left, so V is flipped. */
export function uvToPixels(u: number, v: number): Point {
  return [u * BOARD_W, (1 - v) * BOARD_H];
}
