'use client'

import React, { useEffect, useRef, memo } from 'react'
import { Activity, Wifi, Sun, Cpu } from 'lucide-react'

function TacticalProgressBar() {
  const progressBarRef = useRef<HTMLDivElement>(null)
  const progressValueRef = useRef<HTMLSpanElement>(null)
  const fpsRef = useRef<HTMLSpanElement>(null)
  const progressRef = useRef(92.4)

  useEffect(() => {
    const interval = setInterval(() => {
      const delta = (Math.random() - 0.48) * 0.4
      progressRef.current = Math.min(99.8, Math.max(88, progressRef.current + delta))
      const fps = Math.floor(58 + Math.random() * 4)

      if (progressBarRef.current) {
        progressBarRef.current.style.width = `${progressRef.current}%`
      }
      if (progressValueRef.current) {
        progressValueRef.current.textContent = `${progressRef.current.toFixed(1)}%`
      }
      if (fpsRef.current) {
        fpsRef.current.textContent = `帧率: ${fps}`
      }
    }, 1200)

    return () => clearInterval(interval)
  }, [])

  return (
    <div className="rounded-2xl bg-theme-surface/85 backdrop-blur-md border border-theme-border/60 shadow-paper p-4 overflow-hidden font-mono">
      {/* 顶部指标 */}
      <div className="flex items-center justify-between gap-2 text-xs mb-2">
        <div className="flex items-center gap-1.5">
          <Activity className="w-3.5 h-3.5 text-theme-primary animate-pulse" />
          <span className="text-[11px] font-bold text-slate-700 dark:text-slate-200">
            系统监控
          </span>
        </div>
        <div className="flex items-center gap-3 text-[11px] text-slate-400">
          <span className="flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-bold">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-ping" />
            <span>在线运行</span>
          </span>
          <span ref={fpsRef}>帧率: 60</span>
        </div>
      </div>

      {/* 动态倾斜条纹流动进度条 */}
      <div className="relative h-4 bg-theme-surface-hover/80 rounded-md overflow-hidden p-0.5 border border-theme-border/50 shadow-inner">
        <div
          ref={progressBarRef}
          className="h-full bg-theme-primary rounded-[3px] transition-all duration-700 relative overflow-hidden"
          style={{ width: '92.4%' }}
        >
          {/* 流动斜纹 */}
          <div
            className="absolute inset-0 opacity-40"
            style={{
              backgroundImage:
                'repeating-linear-gradient(45deg, transparent, transparent 8px, rgba(255,255,255,0.7) 8px, rgba(255,255,255,0.7) 16px)',
              backgroundSize: '24px 24px',
              animation: 'stripes-move 1.4s linear infinite',
            }}
          />
        </div>
      </div>

      {/* 进度数值与指标标签 */}
      <div className="mt-2.5 flex items-center justify-between text-[11px] text-slate-500 dark:text-slate-400">
        <div className="flex items-center gap-2">
          <span ref={progressValueRef} className="text-theme-primary font-extrabold text-xs">
            92.4%
          </span>
          <span>数据同步完成</span>
        </div>

        <div className="flex items-center gap-3 text-slate-400">
          <span className="flex items-center gap-1">
            <Sun className="w-3 h-3 text-theme-primary" />
            <span>24°C</span>
          </span>
          <span className="flex items-center gap-1">
            <Cpu className="w-3 h-3 text-theme-primary" />
            <span>12%</span>
          </span>
          <span className="flex items-center gap-1">
            <Wifi className="w-3 h-3 text-emerald-500" />
            <span>18ms</span>
          </span>
        </div>
      </div>
    </div>
  )
}

export default memo(TacticalProgressBar)
