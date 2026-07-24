import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Lock, Mail, User } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { useAuth } from '../../hooks/useAuth'
import { useAuthStore } from '../../stores/authStore'
import AuthField from './AuthField'
import { GithubIcon, GoogleIcon } from './SocialIcons'

type Mode = 'login' | 'signup'

const COPY = {
  login: {
    title: 'Welcome Back',
    subtitle: 'Ready to start drawing again?',
    submit: 'Log In',
    pending: 'Logging in...',
  },
  signup: {
    title: 'Create Account',
    subtitle: 'Join the creative community today.',
    submit: 'Sign Up',
    pending: 'Creating account...',
  },
}

export default function AuthForm() {
  const navigate = useNavigate()
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const { login, register, loading, error } = useAuth()

  const [mode, setMode] = useState<Mode>('login')
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const copy = COPY[mode]

  useEffect(() => {
    if (isAuthenticated) navigate('/dashboard', { replace: true })
  }, [isAuthenticated, navigate])

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (mode === 'login') {
      await login(email, password)
    } else {
      await register(name, email, password)
    }
  }

  return (
    <div className="w-full max-w-lg">
      <div className="mb-7 text-center">
        <h2 className="mb-1 font-display text-4xl leading-none">{copy.title}</h2>
        <p className="font-body text-sm text-on-background/70">{copy.subtitle}</p>
      </div>

      <div className="mb-7 flex gap-1 rounded-full border-[3px] border-outline bg-background p-1 sticker-shadow-sm">
        <ModeTab mode="login" current={mode} onSelect={setMode}>
          Log In
        </ModeTab>
        <ModeTab mode="signup" current={mode} onSelect={setMode}>
          Sign Up
        </ModeTab>
      </div>

      <form className="space-y-4" onSubmit={handleSubmit}>
        {mode === 'signup' && (
          <AuthField
            id="name"
            label="Display Name"
            type="text"
            placeholder="Artist name"
            icon={User}
            value={name}
            onChange={setName}
            autoComplete="nickname"
            required
          />
        )}

        <AuthField
          id="email"
          label="Email Address"
          type="email"
          placeholder="you@example.com"
          icon={Mail}
          value={email}
          onChange={setEmail}
          autoComplete="email"
          required
        />

        <AuthField
          id="password"
          label="Password"
          type="password"
          placeholder="••••••••"
          icon={Lock}
          value={password}
          onChange={setPassword}
          autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
          required
        />

        {mode === 'login' && (
          <div className="flex justify-end">
            <a href="#" className="font-label text-xs font-bold text-primary hover:underline">
              Forgot Password?
            </a>
          </div>
        )}

        {error && (
          <p
            role="alert"
            className="rounded-2xl border-[3px] border-outline bg-primary/15 px-4 py-2.5 font-body text-sm font-medium text-on-background"
          >
            {error}
          </p>
        )}

        <Button type="submit" size="lg" className="w-full" disabled={loading}>
          {loading ? copy.pending : copy.submit}
        </Button>
      </form>

      <div className="my-7 flex items-center gap-4">
        <span className="h-0.5 flex-1 bg-outline/15" />
        <span className="font-label text-[10px] font-bold tracking-widest text-on-background/50 uppercase">
          Or continue with
        </span>
        <span className="h-0.5 flex-1 bg-outline/15" />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Button variant="surface" size="sm" className="w-full">
          <GoogleIcon />
          Google
        </Button>
        <Button variant="surface" size="sm" className="w-full">
          <GithubIcon />
          Github
        </Button>
      </div>

      <p className="mt-8 text-center font-body text-xs text-on-background/60">
        By continuing, you agree to Inkboard's{' '}
        <a href="#" className="underline">
          Terms of Service
        </a>{' '}
        and{' '}
        <a href="#" className="underline">
          Privacy Policy
        </a>
        .
      </p>
    </div>
  )
}

interface ModeTabProps {
  mode: Mode
  current: Mode
  onSelect: (mode: Mode) => void
  children: React.ReactNode
}

function ModeTab({ mode, current, onSelect, children }: ModeTabProps) {
  const isActive = mode === current

  return (
    <button
      type="button"
      onClick={() => onSelect(mode)}
      className={`flex-1 rounded-full py-2 font-label text-sm font-bold transition-colors ${
        isActive ? 'bg-primary text-white' : 'text-on-background/60 hover:text-on-background'
      }`}
    >
      {children}
    </button>
  )
}
