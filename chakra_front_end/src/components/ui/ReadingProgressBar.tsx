'use client'

import React, { useEffect, useState } from 'react'

export default function ReadingProgressBar() {
  const [progress, setProgress] = useState(0)

  useEffect(() => {
    const handleScroll = () => {
      const totalHeight = document.documentElement.scrollHeight - window.innerHeight
      if (totalHeight <= 0) {
        setProgress(0)
        return
      }
      const currentScroll = window.scrollY
      const pct = Math.min(100, Math.max(0, (currentScroll / totalHeight) * 100))
      setProgress(pct)
    }

    window.addEventListener('scroll', handleScroll, { passive: true })
    handleScroll()

    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  if (progress <= 0) return null

  return (
    <div className="fixed top-14 left-0 right-0 h-[2.5px] z-40 bg-transparent pointer-events-none">
      <div
        className="h-full bg-theme-primary transition-all duration-150 ease-out shadow-[0_0_8px_rgba(var(--theme-shadow-rgb),0.5)]"
        style={{ width: `${progress}%` }}
      />
    </div>
  )
}
