// Cloudflare worker
// This Worker sits in front of Cloudflare Pages site.
// Requests to /api/* and /hubs/* are forwarded to the DigitalOcean backend.

const BACKEND_ORIGIN = "https://inkboard-backend-hxjq6.ondigitalocean.app";

export default {
  async fetch(request, env, ctx) {
    const url = new URL(request.url);

    const isApi = url.pathname.startsWith("/api/");
    const isHub = url.pathname.startsWith("/hubs/");

    if (isApi || isHub) {
      const targetUrl = BACKEND_ORIGIN + url.pathname + url.search;

      const proxiedRequest = new Request(targetUrl, request);

      return fetch(proxiedRequest);
    }

    return env.ASSETS.fetch(request);
  },
};
