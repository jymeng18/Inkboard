import type { LucideIcon } from 'lucide-react'

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
}: AuthFieldProps) {
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
          type={type}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={placeholder}
          autoComplete={autoComplete}
          required={required}
          className="w-full rounded-full border-[3px] border-outline bg-background py-2.5 pr-5 pl-11 font-body text-sm font-medium transition-shadow outline-none placeholder:text-on-background/35 focus:border-primary focus:sticker-shadow-sm"
        />
      </div>
    </div>
  )
}
