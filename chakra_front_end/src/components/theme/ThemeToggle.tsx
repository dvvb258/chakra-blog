'use client'

import React, { useEffect, useState } from 'react'
import { useTheme } from 'next-themes'
import NinjaThemeTransition from './NinjaThemeTransition'

/**
 * 漩涡一族正统族徽（Uzumaki Clan Crest）
 * 官方标准矢量：深红厚实阿基米德旋涡，顶部带标志性自然收敛尖角
 */
function UzumakiIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 600 870" fill="none" className={className} aria-hidden="true">
      <path
        style={{
          fill: '#dc2626',
          stroke: '#0f172a',
          strokeWidth: '16px',
          strokeLinecap: 'butt',
          strokeLinejoin: 'miter',
        }}
        d="M 237.63 9.17 C 224.95 101.04 272.54 187.65 342.96 243.64 C 593.73 421.87 590.27 776.13 299.52 841.20 C 82.37 870.01 -4.84 730.39 12.04 579.22 C 11.32 394.94 217.19 334.27 311.78 404.28 C 463.16 523.06 377.91 732.85 201.84 698.77 C 156.39 683.02 134.91 598.95 172.44 550.67 C 213.51 499.03 290.52 552.57 263.11 576.25 C 242.29 608.25 298.74 595.31 298.99 533.19 C 284.03 416.33 112.44 441.07 83.65 548.51 C 52.38 691.33 132.09 805.27 302.26 777.33 C 494.20 730.09 494.55 428.48 318.04 302.32 C 228.64 236.09 190.63 110.06 237.63 9.17 Z"
      />
    </svg>
  )
}

/**
 * 宇智波一族正统团扇族徽（Uchiha Clan Crest · 団扇）
 * 官方标准矢量：正圆扇面，红白交界面呈自然仰角透视圆弧，下方接白色垂直短柄
 */
function UchihaIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 150 200" fill="none" className={className} aria-hidden="true">
      {/* 1. 上半部：红色扇面 */}
      <path
        style={{
          fill: '#dc2626',
          stroke: '#0f172a',
          strokeWidth: '6px',
          strokeLinecap: 'round',
          strokeLinejoin: 'round',
        }}
        d="M 75 2.86 C 35.18 2.86 2.86 34.4 2.86 73.26 C 2.86 79.38 3.66 85.31 5.16 90.98 C 21.63 75.02 46.8 64.81 75 64.81 C 103.2 64.81 128.37 75.02 144.83 90.98 C 146.34 85.31 147.14 79.38 147.14 73.26 C 147.14 34.4 114.82 2.86 75 2.86 Z"
      />
      {/* 2. 下半部：白色扇面与垂直短柄（一体白色成形） */}
      <path
        style={{
          fill: '#ffffff',
          stroke: '#0f172a',
          strokeWidth: '6px',
          strokeLinecap: 'round',
          strokeLinejoin: 'round',
        }}
        d="M 75 77.38 C 47.79 77.38 24.03 88.96 11.25 106.16 C 21.86 125.69 41.59 139.76 64.91 142.95 L 64.91 197.14 L 85.09 197.14 L 85.09 142.95 C 108.41 139.76 128.14 125.69 138.75 106.16 C 125.97 88.96 102.21 77.38 75 77.38 Z"
      />
    </svg>
  )
}

export default function ThemeToggle() {
  const { resolvedTheme, setTheme } = useTheme()
  const [mounted, setMounted] = useState(false)
  const [transitionTarget, setTransitionTarget] = useState<'light' | 'dark' | null>(null)
  const [isRotating, setIsRotating] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  if (!mounted) {
    return (
      <div className="w-9 h-9 rounded-xl border border-theme-border/60 bg-theme-surface/60" />
    )
  }

  const isDark = resolvedTheme === 'dark'
  const toggleTitle = isDark ? '切换为白天模式' : '切换为夜间模式'

  const handleToggle = () => {
    if (transitionTarget) return
    const nextTheme = isDark ? 'light' : 'dark'

    setIsRotating(true)
    setTimeout(() => setIsRotating(false), 600)

    // 触发全屏转场动效
    setTransitionTarget(nextTheme)

    // 在 220ms 光环扩散覆盖全屏时刻完成主题切换
    setTimeout(() => {
      setTheme(nextTheme)
    }, 220)
  }

  return (
    <>
      <button
        onClick={handleToggle}
        type="button"
        aria-label={toggleTitle}
        title={toggleTitle}
        className="relative p-2 rounded-xl border border-theme-border/70 bg-theme-surface-hover hover:border-theme-primary text-theme-primary hover:shadow-[0_0_15px_rgba(var(--theme-shadow-rgb),0.4)] transition-all active:scale-90 shadow-2xs cursor-pointer flex items-center justify-center group"
      >
        <div
          className={`relative flex items-center justify-center transition-transform duration-500 ${
            isRotating ? 'rotate-[360deg] scale-110' : ''
          }`}
        >
          {isDark ? (
            <UchihaIcon className="w-5 h-5 drop-shadow-[0_0_6px_rgba(168,85,247,0.7)]" />
          ) : (
            <UzumakiIcon className="w-5 h-5 drop-shadow-[0_0_6px_rgba(245,158,11,0.6)]" />
          )}
        </div>
      </button>

      {/* 昼夜全屏能量转场 */}
      {transitionTarget && (
        <NinjaThemeTransition
          targetTheme={transitionTarget}
          onComplete={() => setTransitionTarget(null)}
        />
      )}
    </>
  )
}
