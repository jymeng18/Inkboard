import { useState } from 'react'
import type { ReactNode } from 'react'

interface RemoteImageProps {
  src: string
  alt: string
  className?: string
  fallback: ReactNode
}

export default function RemoteImage({ src, alt, className, fallback }: RemoteImageProps) {
  const [failed, setFailed] = useState(false)

  if (failed) return <>{fallback}</>

  return (
    <img
      src={src}
      alt={alt}
      className={className}
      loading="lazy"
      decoding="async"
      onError={() => setFailed(true)}
    />
  )
}
