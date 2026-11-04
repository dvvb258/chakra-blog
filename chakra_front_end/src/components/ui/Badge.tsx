import React from 'react'

export type BadgeVariant = 'theme' | 'sky' | 'amber' | 'emerald' | 'rose' | 'slate'

interface BadgeProps {
  children: React.ReactNode
  variant?: BadgeVariant
  className?: string
}

export default function Badge({ children, variant = 'theme', className = '' }: BadgeProps) {
  const themeStyle =
    'bg-theme-surface-hover text-theme-primary border-theme-border/70 hover:border-theme-border-hover'

  const variantStyles: Record<BadgeVariant, string> = {
    theme: themeStyle,
    sky: themeStyle,
    amber: 'bg-amber-50 dark:bg-amber-950/50 text-amber-700 dark:text-amber-300 border-amber-200 dark:border-amber-800 hover:bg-amber-100 dark:hover:bg-amber-900/50',
    emerald: 'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800 hover:bg-emerald-100 dark:hover:bg-emerald-900/50',
    rose: 'bg-rose-50 dark:bg-rose-950/50 text-rose-700 dark:text-rose-300 border-rose-200 dark:border-rose-800 hover:bg-rose-100 dark:hover:bg-rose-900/50',
    slate: 'bg-slate-50 dark:bg-slate-800 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-700',
  }

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 text-xs font-semibold font-mono border rounded-md transition-colors ${
        variantStyles[variant] || themeStyle
      } ${className}`}
    >
      {children}
    </span>
  )
}
