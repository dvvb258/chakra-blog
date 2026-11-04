import React from 'react'
import Link from 'next/link'
import Image from 'next/image'
import { Home, AlertCircle, Compass } from 'lucide-react'
import { siteConfig } from '@/config/site'

export default function NotFound() {
  const cfg = siteConfig.notFound

  return (
    <div className="min-h-[calc(100vh-14rem)] flex items-center justify-center px-4 sm:px-6 py-12 sm:py-16">
      <div className="max-w-md w-full bg-theme-surface/95 rounded-3xl border border-theme-border/60 shadow-paper p-6 sm:p-9 text-center relative overflow-hidden animate-in fade-in duration-300">
        {/* 顶部主题微条 */}
        <div className="absolute top-0 left-0 right-0 h-1.5 bg-theme-primary/80" />

        {/* 状态指示徽章 */}
        <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-amber-500/10 dark:bg-amber-400/10 text-theme-primary border border-theme-border/70 rounded-full font-mono text-xs font-bold mb-5">
          <AlertCircle className="w-3.5 h-3.5 animate-pulse" />
          <span>{cfg.badge}</span>
        </div>

        {/* 卡卡西经典《亲热天堂》迷路立绘展示区 */}
        <div className="my-3 flex justify-center">
          <div className="relative w-44 h-44 sm:w-56 sm:h-56 rounded-2xl overflow-hidden bg-gradient-to-b from-amber-50/60 to-transparent dark:from-slate-900/60 p-1 border border-theme-border/50 shadow-md group">
            <Image
              src={cfg.image}
              alt={cfg.imageAlt}
              fill
              priority
              className="object-cover object-top drop-shadow-md group-hover:scale-105 transition-transform duration-300"
              sizes="(max-width: 640px) 176px, 224px"
            />
          </div>
        </div>

        {/* 404 状态码与主标题（完全独立行展示，杜绝任何字体重叠） */}
        <div className="mt-4 mb-2 select-none">
          <div className="text-4xl sm:text-5xl font-black tracking-tight font-mono text-transparent bg-clip-text bg-gradient-to-r from-theme-primary via-orange-500 to-amber-500">
            404
          </div>
          <h2 className="mt-1 text-lg sm:text-xl font-extrabold text-slate-800 dark:text-slate-100 tracking-tight">
            {cfg.title}
          </h2>
        </div>

        {/* 卡卡西经典名言气泡 */}
        <div className="mt-4 p-3 rounded-2xl bg-theme-surface-hover/80 border border-theme-border/60 shadow-2xs">
          <p className="text-xs sm:text-sm font-mono font-bold text-theme-primary leading-relaxed">
            {cfg.quote}
          </p>
        </div>

        {/* 提示文案 */}
        <p className="mt-3 text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed font-sans max-w-sm mx-auto">
          {cfg.description}
        </p>

        {/* 引导操作栏 */}
        <div className="mt-6 flex flex-col sm:flex-row items-center justify-center gap-2.5">
          <Link
            href="/"
            className="w-full sm:w-auto px-5 py-2.5 bg-theme-primary hover:bg-theme-primary-hover text-white text-xs font-bold rounded-xl shadow-xs border border-white/20 transition-all active:scale-95 flex items-center justify-center gap-1.5 cursor-pointer"
          >
            <Home className="w-4 h-4" />
            <span>{cfg.backHomeText}</span>
          </Link>
          <Link
            href="/articles"
            className="w-full sm:w-auto px-5 py-2.5 bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary text-xs font-bold rounded-xl border border-theme-border/60 shadow-2xs transition-colors flex items-center justify-center gap-1.5"
          >
            <Compass className="w-4 h-4 text-theme-primary" />
            <span>{cfg.viewArticlesText}</span>
          </Link>
        </div>
      </div>
    </div>
  )
}
