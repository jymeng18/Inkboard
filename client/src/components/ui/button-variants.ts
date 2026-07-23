import { cva } from 'class-variance-authority'

export const buttonVariants = cva(
  "inline-flex shrink-0 cursor-pointer items-center justify-center gap-2 rounded-full border-outline whitespace-nowrap outline-none transition-all hover:-translate-x-1 hover:-translate-y-1 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-secondary active:translate-x-0.5 active:translate-y-0.5 active:shadow-none disabled:pointer-events-none disabled:opacity-50 motion-reduce:transition-none [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        primary: 'bg-primary text-white',
        surface: 'bg-surface text-on-background',
        accent: 'bg-primary-container text-on-background',
        ghost: 'border-transparent shadow-none hover:bg-surface hover:shadow-none',
      },
      size: {
        sm: 'border-[3px] px-5 py-2 font-label text-sm font-bold sticker-shadow-sm hover:shadow-[6px_6px_0_0_var(--color-outline)]',
        md: 'border-[3px] px-7 py-2.5 font-display text-xl sticker-shadow-sm hover:shadow-[6px_6px_0_0_var(--color-outline)]',
        lg: 'border-[3px] px-9 py-3 font-display text-2xl sticker-shadow hover:shadow-[9px_9px_0_0_var(--color-outline)]',
        icon: 'size-10 border-[3px] sticker-shadow-sm hover:shadow-[6px_6px_0_0_var(--color-outline)]',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  },
)
