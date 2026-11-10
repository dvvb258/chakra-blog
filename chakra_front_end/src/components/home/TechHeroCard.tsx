'use client'

import React from 'react'
import { Terminal, Cpu, Layers, Zap, Sparkles } from 'lucide-react'
import { siteConfig } from '@/config/site'

const TECH_TAGS = [
  '.NET 10',
  'C# 14',
  'Next.js 15',
  'React 19',
  'CQRS',
  'PostgreSQL',
  'Redis',
  'Docker',
  'Tailwind CSS',
]

export default function TechHeroCard() {
  return (
    <div className="relative w-full max-w-lg mx-auto p-6 sm:p-7 rounded-3xl bg-theme-surface/90 dark:bg-theme-surface/80 backdrop-blur-xl border border-theme-border/70 shadow-paper hover:shadow-paper-hover transition-all duration-300">
      {/* 顶部环境与状态条 */}
      <div className="flex items-center justify-between pb-4 border-b border-theme-border/50">
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 mr-2">
            <span className="w-2.5 h-2.5 rounded-full bg-rose-500/80 inline-block" />
            <span className="w-2.5 h-2.5 rounded-full bg-amber-500/80 inline-block" />
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-500/80 inline-block" />
          </div>
          <Terminal className="w-4 h-4 text-theme-primary" />
          <span className="text-xs font-mono font-bold tracking-wider text-slate-700 dark:text-slate-200">
            {siteConfig.siteName || 'chakra.blog'}
          </span>
        </div>

        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-[11px] font-mono font-semibold">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
          </span>
          <span>{siteConfig.stationStatus || '在线运行'}</span>
        </div>
      </div>

      {/* 核心介绍内容 */}
      <div className="py-5 space-y-4">
        <div>
          <div className="flex items-center gap-2 mb-1.5">
            <span className="text-[11px] font-mono uppercase tracking-widest text-theme-primary font-bold">
              Engineering Architecture
            </span>
          </div>
          <h3 className="text-lg sm:text-xl font-extrabold text-slate-900 dark:text-white tracking-tight">
            {siteConfig.authorSubtitle || '全栈工程师 & 技术架构实践者'}
          </h3>
          <p className="text-xs sm:text-sm text-slate-600 dark:text-slate-300 mt-1 leading-relaxed">
            {siteConfig.authorIntro || '专注于 .NET 10、Next.js 现代微服务与垂直切片架构工程实践。'}
          </p>
        </div>

        {/* 格言 / 理念框 */}
        <div className="p-3.5 rounded-2xl bg-theme-surface-hover/80 border border-theme-border/60">
          <p className="text-xs text-slate-600 dark:text-slate-300 font-mono italic">
            “{siteConfig.wisdom || siteConfig.quote || '所有的奇迹，都始于日常的专注。'}”
          </p>
        </div>

        {/* 关键技术特征矩阵 */}
        <div className="grid grid-cols-3 gap-2.5 pt-1">
          <div className="p-2.5 rounded-xl bg-theme-surface-hover/60 border border-theme-border/40 text-center">
            <Cpu className="w-4 h-4 mx-auto mb-1 text-sky-500" />
            <div className="text-[10px] font-mono text-slate-400">核心架构</div>
            <div className="text-xs font-bold text-slate-800 dark:text-slate-200 truncate">Vertical Slice</div>
          </div>
          <div className="p-2.5 rounded-xl bg-theme-surface-hover/60 border border-theme-border/40 text-center">
            <Layers className="w-4 h-4 mx-auto mb-1 text-indigo-500" />
            <div className="text-[10px] font-mono text-slate-400">数据通信</div>
            <div className="text-xs font-bold text-slate-800 dark:text-slate-200 truncate">MediatR CQRS</div>
          </div>
          <div className="p-2.5 rounded-xl bg-theme-surface-hover/60 border border-theme-border/40 text-center">
            <Zap className="w-4 h-4 mx-auto mb-1 text-amber-500" />
            <div className="text-[10px] font-mono text-slate-400">渲染模式</div>
            <div className="text-xs font-bold text-slate-800 dark:text-slate-200 truncate">Next.js 流式</div>
          </div>
        </div>

        {/* 技术标签墙 */}
        <div className="pt-2">
          <div className="flex flex-wrap gap-1.5">
            {TECH_TAGS.map((tag) => (
              <span
                key={tag}
                className="px-2.5 py-1 text-[11px] font-mono font-medium rounded-lg bg-theme-surface-hover text-slate-700 dark:text-slate-300 border border-theme-border/60 shadow-2xs"
              >
                {tag}
              </span>
            ))}
          </div>
        </div>
      </div>

      {/* 底部微型状态栏 */}
      <div className="pt-3 border-t border-theme-border/50 flex items-center justify-between text-[11px] font-mono text-slate-400">
        <span className="flex items-center gap-1">
          <Sparkles className="w-3 h-3 text-theme-primary" />
          <span>极简技术风预设</span>
        </span>
        <span>Ready for Production</span>
      </div>
    </div>
  )
}
