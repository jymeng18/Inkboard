import { useState } from 'react'
import { Eye, EyeOff, type LucideIcon } from 'lucide-react'

interface AuthFieldProps {
  id: string
  label: string
  type: string
  placeholder: string
  icon: LucideIcon
  value: string
  onChange: (value: string) => void
  autoComplete?: string
  required?: boolean
  /** Adds a show/hide toggle; use for password fields. */
  revealable?: boolean
  maxLength?: number
  error?: string
}

export default function AuthField({
  id,
  label,
  type,
  placeholder,
  icon: Icon,
  value,
  onChange,
  autoComplete,
  required,
  revealable,
  maxLength,
  error,
}: AuthFieldProps) {
  const errorId = `${id}-error`
  const [revealed, setRevealed] = useState(false)
  const inputType = revealable && revealed ? 'text' : type

  return (
    <div>
      <label htmlFor={id} className="mb-1.5 ml-4 block font-label text-xs font-bold">
        {label}
      </label>

      <div className="relative">
        <Icon
          className="pointer-events-none absolute top-1/2 left-4 size-4 -translate-y-1/2 text-on-background/40"
          aria-hidden
        />
        <input
          id={id}
          type={inputType}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={placeholder}
          autoComplete={autoComplete}
          required={required}
          maxLength={maxLength}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? errorId : undefined}
          className={`w-full rounded-full border-[3px] bg-background py-2.5 pl-11 font-body text-sm font-medium transition-shadow outline-none placeholder:text-on-background/35 focus:sticker-shadow-sm ${
            revealable ? 'pr-11' : 'pr-5'
          } ${error ? 'border-primary' : 'border-outline focus:border-primary'}`}
        />

        {revealable && (
          <button
            type="button"
            onClick={() => setRevealed((prev) => !prev)}
            aria-label={revealed ? 'Hide password' : 'Show password'}
            aria-pressed={revealed}
            className="absolute top-1/2 right-4 -translate-y-1/2 text-on-background/40 transition-colors hover:text-on-background"
          >
            {revealed ? <EyeOff className="size-4" aria-hidden /> : <Eye className="size-4" aria-hidden />}
          </button>
        )}
      </div>

      {error && (
        <p
          id={errorId}
          className="mt-1.5 ml-4 font-body text-xs font-medium text-tertiary"
        >
          {error}
        </p>
      )}
    </div>
  )
}
