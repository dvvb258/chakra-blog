'use client'

import React from 'react'
import Link from 'next/link'
import { ArrowUpRight, Github, BookOpen, Compass, ChevronDown } from 'lucide-react'
import SnakeGameWidget from './SnakeGameWidget'
import InteractiveTerminalWidget from './InteractiveTerminalWidget'
import NarutoCharacter from './NarutoCharacter'
import { siteConfig } from '@/config/site'

export default function HeroSection() {
  return (
    <section className="relative overflow-hidden min-h-[calc(100vh-56px)] flex flex-col justify-between pt-4 pb-4 transition-colors">
      {/* 1. 晨曦日光与深邃雷夜自然底色 */}
      <div
        className="absolute inset-0 -z-10 pointer-events-none bg-theme-bg transition-colors duration-500"
      />

      {/* 2. 点阵背景装饰 */}
      <div
        className="absolute inset-0 -z-10 pointer-events-none opacity-35 dark:opacity-25"
        style={{
          backgroundImage:
            'radial-gradient(rgb(var(--theme-primary)) 0.75px, transparent 0.75px), radial-gradient(rgb(var(--theme-secondary)) 0.75px, transparent 0.75px)',
          backgroundSize: '28px 28px',
          backgroundPosition: '0 0, 14px 14px',
        }}
      />


      {/* 3. 核心内容双栏区 */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 w-full my-auto py-2">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 lg:gap-10 items-center">
          {/* 左侧：看板小组件与快捷入口 */}
          <div className="lg:col-span-6 flex flex-col gap-4 z-20">
            {/* 动效组件 1: 贪吃蛇小游戏 */}
            <SnakeGameWidget />

            {/* 动效组件 2: 极客交互式微型终端 */}
            <InteractiveTerminalWidget />

            {/* 操作按钮区 */}
            <div className="flex flex-wrap items-center gap-3 pt-1">
              <a
                href="#latest-posts"
                className="px-5 py-2.5 bg-theme-primary hover:bg-theme-primary-hover text-white text-xs font-bold rounded-xl shadow-xs hover:shadow-[0_4px_14px_rgba(var(--theme-shadow-rgb),0.3)] transition-all active:scale-95 border border-white/20 flex items-center gap-1.5 cursor-pointer"
              >
                <BookOpen className="w-4 h-4" />
                <span>{siteConfig.hero.readButtonText}</span>
                <ArrowUpRight className="w-3.5 h-3.5" />
              </a>

              <Link
                href="/series"
                className="px-5 py-2.5 bg-theme-surface/85 hover:bg-theme-surface text-slate-700 dark:text-slate-200 hover:text-theme-primary text-xs font-bold rounded-xl border border-theme-border/60 hover:border-theme-primary shadow-paper hover:shadow-paper-hover transition-all flex items-center gap-1.5"
              >
                <Compass className="w-4 h-4 text-theme-primary" />
                <span>{siteConfig.hero.seriesButtonText}</span>
              </Link>

              {/* GitHub 外部入口 */}
              <div className="flex items-center gap-1.5 ml-auto">
                <a
                  href={siteConfig.socials.github}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="p-2.5 rounded-xl bg-theme-surface/80 hover:bg-theme-surface text-slate-600 dark:text-slate-300 hover:text-theme-primary border border-theme-border/60 hover:border-theme-primary shadow-2xs transition-colors"
                  title="GitHub"
                >
                  <Github className="w-4 h-4" />
                </a>
              </div>
            </div>
          </div>

          {/* 右侧：交互式形象展示 */}
          <div className="lg:col-span-6 z-20">
            <NarutoCharacter />
          </div>
        </div>
      </div>

      {/* 4. 底部滚动提示 */}
      <div className="w-full flex flex-col items-center justify-center pb-2 pt-2 z-20">
        <a
          href="#latest-posts"
          className="group inline-flex flex-col items-center gap-1 text-[11px] font-mono font-bold text-slate-400 dark:text-slate-500 hover:text-theme-primary transition-colors cursor-pointer select-none"
        >
          <span className="tracking-widest uppercase flex items-center gap-1.5">
            <span className="w-1.5 h-1.5 rounded-full bg-theme-primary animate-ping" />
            <span>{siteConfig.hero.scrollTipText}</span>
          </span>
          <ChevronDown className="w-4 h-4 text-theme-primary animate-bounce transition-transform group-hover:translate-y-0.5" />
        </a>
      </div>
    </section>
  )
}
