'use client'

import React, { useEffect, useState, useRef, useTransition, useCallback } from 'react'
import { usePathname, useSearchParams } from 'next/navigation'
import Image from 'next/image'
import { siteConfig } from '@/config/site'

/**
 * 全局路由切换加载进度条指示器 (RouteLoadingIndicator)
 */
function RouteLoadingIndicatorInner() {
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const [, startTransition] = useTransition()

  const [isLoading, setIsLoading] = useState(false)
  const [progress, setProgress] = useState(0)
  const [isCompleted, setIsCompleted] = useState(false)

  const timerRef = useRef<NodeJS.Timeout | null>(null)
  const safetyTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  // 清除定时器
  const clearTimers = useCallback(() => {
    if (timerRef.current) clearInterval(timerRef.current)
    if (safetyTimeoutRef.current) clearTimeout(safetyTimeoutRef.current)
  }, [])

  // 完成加载
  const completeLoading = useCallback(() => {
    clearTimers()
    setIsCompleted(true)
    setProgress(100)

    setTimeout(() => {
      setIsLoading(false)
      setIsCompleted(false)
      setProgress(0)
    }, 280)
  }, [clearTimers])

  // 开始加载
  const startLoading = useCallback(() => {
    clearTimers()
    setIsCompleted(false)
    setIsLoading(true)
    setProgress(20)

    // 步进算法 (20% -> 88%)
    timerRef.current = setInterval(() => {
      setProgress((prev) => {
        if (prev >= 88) {
          if (prev >= 95) return 95
          return prev + 1
        }
        const delta = Math.max(2, Math.floor((90 - prev) * 0.18))
        return prev + delta
      })
    }, 120)

    // 8 秒超时保护
    safetyTimeoutRef.current = setTimeout(() => {
      completeLoading()
    }, 8000)
  }, [clearTimers, completeLoading])

  // 监听路由变更以完成加载
  useEffect(() => {
    if (isLoading) {
      completeLoading()
    }
  }, [pathname, searchParams, completeLoading, isLoading])

  // 全局拦截页面内部导航跳转
  useEffect(() => {
    const handleLinkClick = (e: MouseEvent) => {
      if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.altKey || e.shiftKey) {
        return
      }

      const target = (e.target as HTMLElement)?.closest('a')
      if (!target || !target.href) return

      if (target.target && target.target !== '_self') return
      if (target.hasAttribute('download') || target.href.startsWith('mailto:') || target.href.startsWith('tel:')) {
        return
      }

      try {
        const targetUrl = new URL(target.href)
        const currentUrl = new URL(window.location.href)

        // 仅同域跳转
        if (targetUrl.origin !== currentUrl.origin) return

        // 忽略页面内 hash 锚点
        if (targetUrl.pathname === currentUrl.pathname && targetUrl.search === currentUrl.search) {
          return
        }

        // 触发路由加载动效
        startTransition(() => {
          startLoading()
        })
      } catch {
        // 忽略异常
      }
    }

    const handlePopState = () => {
      startTransition(() => {
        startLoading()
      })
    }

    document.addEventListener('click', handleLinkClick, true)
    window.addEventListener('popstate', handlePopState)

    return () => {
      document.removeEventListener('click', handleLinkClick, true)
      window.removeEventListener('popstate', handlePopState)
      clearTimers()
    }
  }, [startLoading, clearTimers])

  if (!isLoading && !isCompleted) return null

  return (
    <div
      className="fixed top-[62px] left-1/2 -translate-x-1/2 z-[9999] pointer-events-none select-none transition-all duration-300 ease-out"
      style={{
        transform: isLoading || isCompleted ? 'translate(-50%, 0)' : 'translate(-50%, -8px)',
        opacity: isLoading || isCompleted ? 1 : 0,
      }}
      aria-hidden="true"
    >
      {/* 顶部加载状态胶囊 */}
      <div className="flex items-center gap-2.5 px-3.5 py-1.5 rounded-full bg-theme-surface/95 backdrop-blur-md border border-theme-border/80 shadow-lg shadow-theme-primary/10 text-xs font-mono">
        {/* 左侧：微标动效 */}
        <div className="relative w-4 h-4 rounded-full overflow-hidden flex-shrink-0 animate-spin [animation-duration:8s]">
          <Image
            src={siteConfig.startupLoader.icon}
            alt="Status Crest"
            fill
            className="object-cover"
          />
        </div>

        {/* 动态脉冲点 */}
        <span className="relative flex h-2 w-2 flex-shrink-0">
          <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-theme-primary opacity-75" />
          <span className="relative inline-flex rounded-full h-2 w-2 bg-theme-primary" />
        </span>

        {/* 中间：倾斜流动进度条 */}
        <div className="w-[96px] h-[5px] bg-theme-primary/15 overflow-hidden rounded-xs -skew-x-[15deg] border border-theme-border/70 p-[0.5px]">
          <div
            className="h-full bg-theme-primary rounded-2xs relative overflow-hidden transition-all duration-150 ease-out shadow-[0_0_8px_rgba(var(--theme-shadow-rgb),0.5)]"
            style={{ width: `${progress}%` }}
          >
            {/* 流动斜纹 */}
            <div
              className="absolute inset-0 opacity-40"
              style={{
                backgroundImage:
                  'repeating-linear-gradient(45deg, transparent, transparent 4px, rgba(255,255,255,0.8) 4px, rgba(255,255,255,0.8) 8px)',
                backgroundSize: '12px 12px',
                animation: 'stripes-move 0.8s linear infinite',
              }}
            />
          </div>
        </div>

        {/* 右侧：步进百分比与就绪状态 */}
        <div className="text-[11px] font-bold tabular-nums min-w-[38px] text-right">
          {isCompleted ? (
            <span className="text-emerald-500 font-extrabold flex items-center justify-end gap-0.5 animate-in fade-in zoom-in-75 duration-150">
              <span>OK ✓</span>
            </span>
          ) : (
            <span className="text-theme-primary font-extrabold">
              {progress}%
            </span>
          )}
        </div>
      </div>
    </div>
  )
}

export default function RouteLoadingIndicator() {
  return (
    <React.Suspense fallback={null}>
      <RouteLoadingIndicatorInner />
    </React.Suspense>
  )
}
