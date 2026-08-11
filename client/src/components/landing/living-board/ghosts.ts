import { BOARD_H, BOARD_W, type BoardSurface } from "./boardDrawing";

/*
 * The "other people in the room." Each ghost owns one stroke on the shared
 * surface at a time, tracing a doodle point by point so the board is never
 * empty and a first-time visitor sees collaboration happening on its own.
 */

type Point = [number, number];

/* Doodles live in a 0..1 box and get placed + scaled onto the board. Each is a
 * single continuous stroke so a ghost only ever manages one path. */
const DOODLES: Point[][] = [
  // five-point star, drawn as one pentagram
  [
    [0.5, 0.02],
    [0.22, 0.89],
    [0.96, 0.35],
    [0.04, 0.35],
    [0.78, 0.89],
    [0.5, 0.02],
  ],
  // heart
  [
    [0.5, 0.26],
    [0.42, 0.1],
    [0.28, 0.04],
    [0.14, 0.13],
    [0.09, 0.31],
    [0.2, 0.51],
    [0.5, 0.83],
    [0.8, 0.51],
    [0.91, 0.31],
    [0.86, 0.13],
    [0.72, 0.04],
    [0.58, 0.1],
    [0.5, 0.26],
  ],
  // squiggle
  [
    [0.04, 0.5],
    [0.2, 0.2],
    [0.36, 0.82],
    [0.5, 0.22],
    [0.64, 0.82],
    [0.8, 0.2],
    [0.96, 0.5],
  ],
  // lightning bolt
  [
    [0.6, 0.03],
    [0.32, 0.52],
    [0.5, 0.52],
    [0.36, 0.97],
    [0.72, 0.44],
    [0.54, 0.44],
    [0.6, 0.03],
  ],
  // spiral
  spiral(),
];

function spiral(): Point[] {
  const pts: Point[] = [];
  const turns = 2.6;
  const steps = 44;
  for (let i = 0; i <= steps; i++) {
    const t = i / steps;
    const angle = t * turns * Math.PI * 2;
    const radius = 0.06 + t * 0.42;
    pts.push([0.5 + Math.cos(angle) * radius, 0.5 + Math.sin(angle) * radius]);
  }
  return pts;
}

/* Resample a polyline to roughly even spacing so the pen glides at a steady
 * pace regardless of how far apart the source vertices are. */
function densify(vertices: Point[], spacing: number): Point[] {
  const out: Point[] = [vertices[0]];
  for (let i = 1; i < vertices.length; i++) {
    const [ax, ay] = vertices[i - 1];
    const [bx, by] = vertices[i];
    const dist = Math.hypot(bx - ax, by - ay);
    const steps = Math.max(1, Math.round(dist / spacing));
    for (let s = 1; s <= steps; s++) {
      const t = s / steps;
      out.push([ax + (bx - ax) * t, ay + (by - ay) * t]);
    }
  }
  return out;
}

const MARGIN = 90;

export class Ghost {
  readonly id: string;
  readonly name: string;
  readonly color: string;
  current: Point = [BOARD_W / 2, BOARD_H / 2];
  private surface: BoardSurface;
  private pointsPerSecond: number;
  private path: Point[] = [];
  private cursor = 0;
  private drawn = 0;
  private pausing = true;
  private pauseUntil = 0;

  constructor(
    surface: BoardSurface,
    id: string,
    name: string,
    color: string,
    pointsPerSecond: number,
  ) {
    this.surface = surface;
    this.id = id;
    this.name = name;
    this.color = color;
    this.pointsPerSecond = pointsPerSecond;
  }

  private begin() {
    const doodle = DOODLES[Math.floor(Math.random() * DOODLES.length)];
    const scale = 150 + Math.random() * 150;
    const x = MARGIN + Math.random() * (BOARD_W - scale - MARGIN * 2);
    const y = MARGIN + Math.random() * (BOARD_H - scale - MARGIN * 2);
    const placed: Point[] = doodle.map(([u, v]) => [x + u * scale, y + v * scale]);
    this.path = densify(placed, 7);
    this.cursor = 0;
    this.drawn = 0;
    this.pausing = false;
    this.current = this.path[0];
    this.surface.start(this.id, this.color, 12);
    this.surface.addPoint(this.id, this.path[0][0], this.path[0][1]);
  }

  step(dt: number, now: number) {
    if (this.pausing) {
      if (now >= this.pauseUntil) this.begin();
      return;
    }
    this.cursor += this.pointsPerSecond * dt;
    const target = Math.min(Math.floor(this.cursor), this.path.length - 1);
    while (this.drawn < target) {
      this.drawn++;
      const p = this.path[this.drawn];
      this.surface.addPoint(this.id, p[0], p[1]);
    }
    this.current = this.path[target];
    if (target >= this.path.length - 1) {
      this.surface.end(this.id);
      this.pausing = true;
      this.pauseUntil = now + 900 + Math.random() * 1600;
    }
  }
}

export function createGhosts(surface: BoardSurface): Ghost[] {
  return [
    new Ghost(surface, "ghost-lina", "Lina", "#00c1fd", 34),
    new Ghost(surface, "ghost-tom", "Tom", "#ff7070", 28),
  ];
}
