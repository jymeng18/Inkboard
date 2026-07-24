import { createRoot } from 'react-dom/client'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { Toaster } from 'sonner'
import './index.css'
import App from './App'
import AuthPage from './pages/AuthPage'
import LandingPage from './pages/LandingPage'

createRoot(document.getElementById('root')!).render(
  <BrowserRouter>
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/login" element={<AuthPage />} />
      <Route path="/app" element={<App />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
    <Toaster richColors closeButton position="top-right" />
  </BrowserRouter>,
)
