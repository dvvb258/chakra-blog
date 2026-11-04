'use client'

import React, { useState, useEffect } from 'react'
import Image from 'next/image'
import { siteConfig } from '@/config/site'

interface TacticalLoaderProps {
  title?: string
  subtitle?: string
  mode?: 'full' | 'inline'
  className?: string
}

export default function TacticalLoader({
  title = 'LOADING...',
  subtitle = '内容正在载入中，请稍候...',
  mode = 'full',
  className = '',
}: TacticalLoaderProps) {
  const [progress, setProgress] = useState(18)

  useEffect(() => {
    // 动态模拟平滑加载进度
    const interval = setInterval(() => {
      setProgress((prev) => {
        if (prev >= 96) return 96
        const inc = Math.floor(Math.random() * 14) + 6
        return Math.min(96, prev + inc)
      })
    }, 180)

    return () => clearInterval(interval)
  }, [])

  const isFull = mode === 'full'

  return (
    <div
      className={`flex flex-col items-center justify-center select-none ${
        isFull ? 'min-h-[70vh] py-16 px-4' : 'py-12 px-4'
      } ${className}`}
      role="status"
      aria-label={subtitle}
    >
      {/* 加载徽章 */}
      <div className="relative w-20 h-20 sm:w-24 sm:h-24 mb-5 animate-bounce [animation-duration:1.8s]">
        <div className="absolute -inset-1.5 rounded-full bg-theme-primary/30 blur-xs animate-spin [animation-duration:8s]" />
        <div className="relative w-full h-full rounded-full overflow-hidden ring-3 ring-white dark:ring-slate-800 shadow-lg bg-white dark:bg-slate-900">
          <Image
            src={siteConfig.startupLoader.icon}
            alt="Site Logo"
            fill
            priority
            className="object-cover"
          />
        </div>
      </div>

      {/* 加载主标题 */}
      <h3 className="text-xl sm:text-2xl font-black italic tracking-tight text-slate-800 dark:text-slate-100 font-sans">
        {title}
      </h3>

      {/* 倾斜微光进度条 */}
      <div className="w-48 sm:w-56 h-2 mt-4 bg-theme-primary/15 overflow-hidden rounded-xs -skew-x-[15deg] border border-theme-border/60 p-[1px] shadow-inner">
        <div
          className="h-full bg-theme-primary rounded-2xs relative overflow-hidden transition-all duration-200 ease-out"
          style={{ width: `${progress}%` }}
        >
          {/* 流动斜纹动效 */}
          <div
            className="absolute inset-0 opacity-40"
            style={{
              backgroundImage:
                'repeating-linear-gradient(45deg, transparent, transparent 6px, rgba(255,255,255,0.8) 6px, rgba(255,255,255,0.8) 12px)',
              backgroundSize: '16px 16px',
              animation: 'stripes-move 1s linear infinite',
            }}
          />
        </div>
      </div>

      {/* 百分比数值与状态描述 */}
      <div className="mt-3 flex items-center gap-2 font-mono text-xs text-theme-primary font-bold">
        <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-ping" />
        <span className="tabular-nums">{progress}%</span>
        <span className="text-slate-400 dark:text-slate-500 font-normal">|</span>
        <span className="text-slate-600 dark:text-slate-400 text-[11px] font-sans">
          {subtitle}
        </span>
      </div>
    </div>
  )
}
