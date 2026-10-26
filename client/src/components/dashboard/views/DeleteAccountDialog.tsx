import { useEffect, useRef, useState } from 'react'
import { AlertTriangle, Eye, EyeOff, LoaderCircle, Trash2, X } from 'lucide-react'
import { toast } from 'sonner'

import { deleteAccount } from '@/api/account'
import { AUTH_LIMITS } from '@/api/auth'
import { extractErrorMessage } from '@/api/party'
import { Button } from '@/components/ui/button'
import { clearLocalSession } from '@/lib/clearSession'

export default function DeleteAccountDialog({
  email,
  onClose,
}: {
  email: string
  onClose: () => void
}) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const passwordRef = useRef<HTMLInputElement>(null)
  const [password, setPassword] = useState('')
  const [revealed, setRevealed] = useState(false)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const dialog = dialogRef.current
    dialog?.showModal()
    passwordRef.current?.focus()
    return () => dialog?.close()
  }, [])

  async function handleDelete() {
    if (!password || pending) return
    setPending(true)
    setError(null)
    try {
      await deleteAccount(password)
      setPassword('')
      onClose()
      clearLocalSession()
      toast.success('Your account has been deleted.')
    } catch (err) {
      setError(extractErrorMessage(err))
    } finally {
      setPending(false)
    }
  }

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="delete-account-title"
      aria-describedby="delete-account-description"
      onCancel={(event) => {
        event.preventDefault()
        if (!pending) onClose()
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget && !pending) onClose()
      }}
      className="fixed inset-0 m-auto max-h-[calc(100dvh-2rem)] w-[calc(100%-2rem)] max-w-md overflow-y-auto rounded-3xl border-4 border-outline bg-surface p-6 text-on-background sticker-shadow-lg backdrop:bg-outline/40 backdrop:backdrop-blur-sm"
    >
      <button
        type="button"
        onClick={onClose}
        disabled={pending}
        aria-label="Close account deletion"
        className="absolute top-4 right-4 flex size-8 items-center justify-center rounded-full hover:bg-background focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-secondary disabled:opacity-40"
      >
        <X className="size-5" aria-hidden />
      </button>

      <span className="mb-5 flex size-12 items-center justify-center rounded-2xl border-[3px] border-outline bg-primary sticker-shadow-sm">
        <AlertTriangle className="size-6" aria-hidden />
      </span>
      <h2 id="delete-account-title" className="font-display text-3xl leading-none">Delete your account?</h2>
      <p className="mt-3 break-all font-label text-sm font-bold">{email}</p>
      <p id="delete-account-description" className="mt-3 font-body text-sm leading-relaxed text-on-background/75">
        This permanently removes your account, your friendships, and every canvas you own.
        Drawings on other people&apos;s canvases stay, without your name. You&apos;ll also leave your current party.
      </p>
      <p className="mt-3 font-label text-xs font-bold text-tertiary">This cannot be undone.</p>

      <form
        className="mt-6"
        aria-busy={pending}
        onSubmit={(event) => {
          event.preventDefault()
          void handleDelete()
        }}
      >
        <label htmlFor="delete-account-password" className="mb-2 block font-label text-xs font-bold">
          Confirm with your current password
        </label>
        <div className="relative">
          <input
            id="delete-account-password"
            ref={passwordRef}
            type={revealed ? 'text' : 'password'}
            autoComplete="current-password"
            value={password}
            onChange={(event) => {
              setPassword(event.target.value)
              setError(null)
            }}
            maxLength={AUTH_LIMITS.password.max}
            required
            disabled={pending}
            aria-invalid={!!error}
            aria-describedby={error ? 'delete-account-error' : undefined}
            className="w-full rounded-2xl border-[3px] border-outline bg-background py-3 pr-12 pl-4 font-body text-sm outline-none focus:border-secondary disabled:opacity-60"
          />
          <button
            type="button"
            onClick={() => setRevealed(!revealed)}
            disabled={pending}
            aria-label={revealed ? 'Hide password' : 'Show password'}
            aria-pressed={revealed}
            className="absolute top-1/2 right-4 -translate-y-1/2 rounded text-on-background/60 focus-visible:outline-2 focus-visible:outline-secondary"
          >
            {revealed ? <EyeOff className="size-4" aria-hidden /> : <Eye className="size-4" aria-hidden />}
          </button>
        </div>
        {error && <p id="delete-account-error" role="alert" className="mt-3 font-body text-xs leading-relaxed text-tertiary">{error}</p>}

        <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <Button type="button" variant="surface" size="sm" onClick={onClose} disabled={pending}>Keep my account</Button>
          <Button type="submit" size="sm" disabled={!password || pending}>
            {pending ? <LoaderCircle className="size-4 animate-spin" aria-hidden /> : <Trash2 className="size-4" aria-hidden />}
            {pending ? 'Deleting…' : 'Delete permanently'}
          </Button>
        </div>
      </form>
    </dialog>
  )
}
