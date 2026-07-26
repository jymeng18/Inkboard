/**
 * All code used in this component comes from https://reactbits.dev/
 * Slightly edited based on project needs
 */

import React, { useEffect, useRef } from "react";
import { Renderer, Transform, Vec3, Color, Polyline } from "ogl";
import {
  useHasFinePointer,
  usePrefersReducedMotion,
} from "@/hooks/useMediaQuery";

interface RibbonsProps {
  colors?: string[];
  baseSpring?: number;
  baseFriction?: number;
  baseThickness?: number;
  offsetFactor?: number;
  maxAge?: number;
  pointCount?: number;
  speedMultiplier?: number;
  enableFade?: boolean;
  enableShaderEffect?: boolean;
  effectAmplitude?: number;
  backgroundColor?: number[];
  /* Hard ink border drawn under each ribbon, matching the sticker shadows. */
  outlineColor?: string;
  outlineWidth?: number;
}

const Ribbons: React.FC<RibbonsProps> = ({
  colors = ["#ff7070", "#ffd23f", "#00c1fd"],
  baseSpring = 0.03,
  baseFriction = 0.9,
  baseThickness = 30,
  offsetFactor = 0.05,
  maxAge = 500,
  pointCount = 50,
  speedMultiplier = 0.6,
  enableFade = false,
  enableShaderEffect = false,
  effectAmplitude = 2,
  backgroundColor = [0, 0, 0, 0],
  outlineColor = "#2d2926",
  outlineWidth = 5,
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const hasFinePointer = useHasFinePointer();
  const prefersReducedMotion = usePrefersReducedMotion();
  const enabled = hasFinePointer && !prefersReducedMotion;

  useEffect(() => {
    const container = containerRef.current;
    if (!container || !enabled) return;

    const renderer = new Renderer({
      dpr: window.devicePixelRatio || 2,
      alpha: true,
    });
    const gl = renderer.gl;
    if (Array.isArray(backgroundColor) && backgroundColor.length === 4) {
      gl.clearColor(
        backgroundColor[0],
        backgroundColor[1],
        backgroundColor[2],
        backgroundColor[3],
      );
    } else {
      gl.clearColor(0, 0, 0, 0);
    }

    gl.canvas.style.position = "absolute";
    gl.canvas.style.top = "0";
    gl.canvas.style.left = "0";
    gl.canvas.style.width = "100%";
    gl.canvas.style.height = "100%";
    container.appendChild(gl.canvas);

    const scene = new Transform();
    const lines: {
      spring: number;
      friction: number;
      mouseVelocity: Vec3;
      mouseOffset: Vec3;
      points: Vec3[];
      /* The outline first, then the fill it sits behind. */
      polylines: Polyline[];
    }[] = [];

    const vertex = `
      precision highp float;
      
      attribute vec3 position;
      attribute vec3 next;
      attribute vec3 prev;
      attribute vec2 uv;
      attribute float side;
      
      uniform vec2 uResolution;
      uniform float uDPR;
      uniform float uThickness;
      uniform float uTime;
      uniform float uEnableShaderEffect;
      uniform float uEffectAmplitude;
      
      varying vec2 vUV;
      
      vec4 getPosition() {
          vec4 current = vec4(position, 1.0);
          vec2 aspect = vec2(uResolution.x / uResolution.y, 1.0);
          vec2 nextScreen = next.xy * aspect;
          vec2 prevScreen = prev.xy * aspect;
          vec2 tangent = normalize(nextScreen - prevScreen);
          vec2 normal = vec2(-tangent.y, tangent.x);
          normal /= aspect;
          normal *= mix(1.0, 0.1, pow(abs(uv.y - 0.5) * 2.0, 2.0));
          float dist = length(nextScreen - prevScreen);
          normal *= smoothstep(0.0, 0.02, dist);
          float pixelWidthRatio = 1.0 / (uResolution.y / uDPR);
          float pixelWidth = current.w * pixelWidthRatio;
          normal *= pixelWidth * uThickness;
          current.xy -= normal * side;
          if(uEnableShaderEffect > 0.5) {
            current.xy += normal * sin(uTime + current.x * 10.0) * uEffectAmplitude;
          }
          return current;
      }
      
      void main() {
          vUV = uv;
          gl_Position = getPosition();
      }
    `;

    const fragment = `
      precision highp float;
      uniform vec3 uColor;
      uniform float uOpacity;
      uniform float uEnableFade;
      varying vec2 vUV;
      void main() {
          float fadeFactor = 1.0;
          if(uEnableFade > 0.5) {
              fadeFactor = 1.0 - smoothstep(0.0, 1.0, vUV.y);
          }
          gl_FragColor = vec4(uColor, uOpacity * fadeFactor);
      }
    `;

    function resize() {
      if (!container) return;
      const width = container.clientWidth;
      const height = container.clientHeight;
      renderer.setSize(width, height);
      lines.forEach((line) => line.polylines.forEach((p) => p.resize()));
    }
    window.addEventListener("resize", resize);

    const center = (colors.length - 1) / 2;
    colors.forEach((color, index) => {
      const spring = baseSpring + (Math.random() - 0.5) * 0.05;
      const friction = baseFriction + (Math.random() - 0.5) * 0.05;
      const thickness = baseThickness + (Math.random() - 0.5) * 3;
      const mouseOffset = new Vec3(
        (index - center) * offsetFactor + (Math.random() - 0.5) * 0.01,
        (Math.random() - 0.5) * 0.1,
        0,
      );

      const count = pointCount;
      const points: Vec3[] = [];
      for (let i = 0; i < count; i++) {
        points.push(new Vec3());
      }

      const makePolyline = (lineColor: string, lineThickness: number) =>
        new Polyline(gl, {
          points,
          vertex,
          fragment,
          uniforms: {
            uColor: { value: new Color(lineColor) },
            uThickness: { value: lineThickness },
            uOpacity: { value: 1.0 },
            uTime: { value: 0.0 },
            uEnableShaderEffect: { value: enableShaderEffect ? 1.0 : 0.0 },
            uEffectAmplitude: { value: effectAmplitude },
            uEnableFade: { value: enableFade ? 1.0 : 0.0 },
          },
        });

      /*
       * Two passes over the same point list: a fatter ink stroke underneath and
       * the colored fill on top, so every ribbon reads as an outlined sticker.
       */
      const polylines: Polyline[] = [];
      if (outlineWidth > 0) {
        polylines.push(makePolyline(outlineColor, thickness + outlineWidth * 2));
      }
      polylines.push(makePolyline(color, thickness));
      polylines.forEach((p, i) => {
        p.mesh.setParent(scene);
        /*
         * Every ribbon sits at z = 0, so depth testing would hide whichever one
         * happened to be drawn second. Dropping it leaves a plain painter's
         * order: outline, then fill, then the next ribbon on top.
         */
        p.program.depthTest = false;
        p.program.depthWrite = false;
        p.program.setBlendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        p.mesh.renderOrder = index * 2 + i;
      });

      lines.push({
        spring,
        friction,
        mouseVelocity: new Vec3(),
        mouseOffset,
        points,
        polylines,
      });
    });

    resize();

    const mouse = new Vec3();
    function updateMouse(e: MouseEvent | TouchEvent) {
      let x: number, y: number;
      if (!container) return;
      const rect = container.getBoundingClientRect();
      if ("changedTouches" in e && e.changedTouches.length) {
        x = e.changedTouches[0].clientX - rect.left;
        y = e.changedTouches[0].clientY - rect.top;
      } else if (e instanceof MouseEvent) {
        x = e.clientX - rect.left;
        y = e.clientY - rect.top;
      } else {
        x = 0;
        y = 0;
      }
      const width = container.clientWidth;
      const height = container.clientHeight;
      mouse.set((x / width) * 2 - 1, (y / height) * -2 + 1, 0);
    }
    window.addEventListener("mousemove", updateMouse);
    window.addEventListener("touchstart", updateMouse);
    window.addEventListener("touchmove", updateMouse);

    const tmp = new Vec3();
    let frameId: number;
    let lastTime = performance.now();
    function update() {
      frameId = requestAnimationFrame(update);
      const currentTime = performance.now();
      const dt = currentTime - lastTime;
      lastTime = currentTime;

      lines.forEach((line) => {
        tmp
          .copy(mouse)
          .add(line.mouseOffset)
          .sub(line.points[0])
          .multiply(line.spring);
        line.mouseVelocity.add(tmp).multiply(line.friction);
        line.points[0].add(line.mouseVelocity);

        for (let i = 1; i < line.points.length; i++) {
          if (isFinite(maxAge) && maxAge > 0) {
            const segmentDelay = maxAge / (line.points.length - 1);
            const alpha = Math.min(1, (dt * speedMultiplier) / segmentDelay);
            line.points[i].lerp(line.points[i - 1], alpha);
          } else {
            line.points[i].lerp(line.points[i - 1], 0.9);
          }
        }
        line.polylines.forEach((p) => {
          if (p.mesh.program.uniforms.uTime) {
            p.mesh.program.uniforms.uTime.value = currentTime * 0.001;
          }
          p.updateGeometry();
        });
      });

      renderer.render({ scene });
    }
    update();

    return () => {
      window.removeEventListener("resize", resize);
      window.removeEventListener("mousemove", updateMouse);
      window.removeEventListener("touchstart", updateMouse);
      window.removeEventListener("touchmove", updateMouse);
      cancelAnimationFrame(frameId);
      if (gl.canvas && gl.canvas.parentNode === container) {
        container.removeChild(gl.canvas);
      }
    };
  }, [
    colors,
    baseSpring,
    baseFriction,
    baseThickness,
    offsetFactor,
    maxAge,
    pointCount,
    speedMultiplier,
    enableFade,
    enableShaderEffect,
    effectAmplitude,
    backgroundColor,
    outlineColor,
    outlineWidth,
    enabled,
  ]);

  if (!enabled) return null;

  return <div ref={containerRef} className="relative w-full h-full" />;
};

export default Ribbons;
