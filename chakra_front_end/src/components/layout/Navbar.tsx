'use client'

import React from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { BookOpen, Layers, User, Sparkles, LucideIcon } from 'lucide-react'
import ThemeToggle from '@/components/theme/ThemeToggle'

interface NavItem {
  href: string
  label: string
  icon: LucideIcon
}

const NAV_ITEMS: NavItem[] = [
  { href: '/', label: '首页', icon: Sparkles },
  { href: '/articles', label: '文章', icon: BookOpen },
  { href: '/series', label: '系列', icon: Layers },
  { href: '/about', label: '关于', icon: User },
]

export default function Navbar() {
  const pathname = usePathname()

  return (
    <header className="sticky top-0 z-50 w-full border-b border-theme-border/50 bg-theme-surface/85 backdrop-blur-md transition-all">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-14 flex items-center justify-between">
        {/* 纯粹干净的通用技术导航菜单 */}
        <nav className="flex items-center gap-1.5 sm:gap-2">
          {NAV_ITEMS.map((item) => {
            const Icon = item.icon
            const isActive = pathname === item.href || (item.href !== '/' && pathname?.startsWith(item.href))
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`relative px-3.5 py-1.5 text-sm font-medium transition-all flex items-center gap-1.5 rounded-xl ${
                  isActive
                    ? 'text-theme-primary bg-theme-surface-hover font-bold shadow-xs'
                    : 'text-slate-600 dark:text-slate-300 hover:text-theme-primary hover:bg-theme-surface-hover'
                }`}
              >
                <Icon className={`w-4 h-4 ${isActive ? 'text-theme-primary' : 'text-slate-400 dark:text-slate-500'}`} />
                <span>{item.label}</span>
                {isActive && (
                  <span className="absolute -bottom-2.5 left-1/2 -translate-x-1/2 w-6 h-0.5 bg-theme-primary rounded-full shadow-[0_0_6px_rgba(var(--theme-shadow-rgb),0.6)]" />
                )}
              </Link>
            )
          })}
        </nav>

        {/* 昼夜主题切换按钮 */}
        <ThemeToggle />
      </div>
    </header>
  )
}
