import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Toaster } from 'sonner'
import './index.css'
import App from './App'
import RequireAuth from '@/components/RequireAuth'
import { queryClient } from '@/lib/queryClient'
import { bootstrapSession } from '@/lib/session'
import AuthPage from './pages/AuthPage'
import CanvasPage from './pages/CanvasPage'
import DashboardPage from './pages/DashboardPage'
import LandingPage from './pages/LandingPage'

// Restore the session from the refresh cookie before the first paint decides routing.
bootstrapSession()

createRoot(document.getElementById('root')!).render(
  <QueryClientProvider client={queryClient}>
    <BrowserRouter>
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
      <Toaster richColors closeButton position="top-right" />
    </BrowserRouter>
  </QueryClientProvider>,
)
