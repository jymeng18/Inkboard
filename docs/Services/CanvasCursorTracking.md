## Micro-Optimizations

### A. Sender-Side Throttling (Client)
Raw `mousemove` events fire up to 120+ times per second. Browsers must throttle transmissions to a **30ms–50ms interval**. This preserves human perception of real-time movement while reducing outbound network traffic.

### B. Payload Minimization
To minimize bandwidth consumption and serialization overhead under high concurrency, JSON payloads must use ultra-short keys.

*   **Bad Payload (Disallowed):**
    ```json
    {
      "eventType": "CURSOR_MOVEMENT_REPORT",
      "userId": 9845,
      "coordinateX": 1024.55,
      "coordinateY": 768.22
    }
    ```
*   **Optimized Payload (Enforced):**
    ```json
    {
      "id": 9845,
      "x": 1025,
      "y": 768
    }
    ```
    *Note: Coordinates should be rounded to integers where sub-pixel precision is visually negligible.*

### C. Server-Side Bypass
The WebSocket server must act as a pure, memory-only routing hub. 
*   **No Database Operations:** Never write cursor states to PostgreSQL or Redis persistence layers.
*   **In-Memory Room Context:** Group connection handles using light in-memory lookup sets hashed by Canvas/Project ID. 

### D. Receiver-Side Interpolation (Client Canvas Rendering)
Because incoming coordinates are throttled, snapping Konva.js shapes directly to new positions creates visual jitter. Receivers must use Linear Interpolation (Lerp) inside a frame animation loop to bridge the 40ms gaps.

```javascript
// Executed inside Konva.Animation or requestAnimationFrame
// Smoothly slides current position toward target position by 20% each frame
currentX += (targetX - currentX) * 0.2;
currentY += (targetY - currentY) * 0.2;
cursorShape.position({ x: currentX, y: currentY });