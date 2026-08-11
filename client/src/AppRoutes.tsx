import { Suspense, lazy } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import App from './App'
import LandingPage from './pages/LandingPage'

// The landing page is the common cold entry, so it stays in the initial bundle.
// Everything behind it splits into its own chunk, so a first visit to "/" never
// downloads the heavy stuff. RequireAuth is lazy too: it drags in the SignalR
// party hub, which the landing page has no use for.
const RequireAuth = lazy(() => import('@/components/RequireAuth'))
const AuthPage = lazy(() => import('./pages/AuthPage'))
const DashboardPage = lazy(() => import('./pages/DashboardPage'))
const CanvasPage = lazy(() => import('./pages/CanvasPage'))

function RouteFallback() {
  return (
    <div className="grid min-h-dvh place-items-center bg-background">
      <div className="size-10 animate-spin rounded-full border-4 border-outline border-t-transparent" />
    </div>
  )
}

export default function AppRoutes() {
  return (
    <Suspense fallback={<RouteFallback />}>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<AuthPage />} />
        <Route element={<RequireAuth />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/canvas/:canvasId" element={<CanvasPage />} />
          <Route path="/app" element={<App />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Suspense>
  )
}
