import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { Toaster } from 'sonner'
import './index.css'
import AppRoutes from './AppRoutes'
import { queryClient } from '@/lib/queryClient'
import { bootstrapSession } from '@/lib/session'

// Restore the session from the refresh cookie before the first paint decides routing.
bootstrapSession()

createRoot(document.getElementById('root')!).render(
  <QueryClientProvider client={queryClient}>
    <BrowserRouter>
      <AppRoutes />
      <Toaster richColors closeButton position="top-right" />
    </BrowserRouter>
  </QueryClientProvider>,
)
