import React from 'react'
import Image from 'next/image'
import Link from 'next/link'
import type { Metadata } from 'next'
import { siteConfig } from '@/config/site'
import { Github, Mail, Flame, ShieldCheck, Zap, Mountain, Wind, Sparkles, Waves } from 'lucide-react'

export const metadata: Metadata = {
  title: `关于博主 · ${siteConfig.name}`,
  description: siteConfig.description,
}

/**
 * 图标映射字典
 */
const ICON_MAP: Record<string, React.ComponentType<{ className?: string }>> = {
  Flame,
  Waves,
  Zap,
  Mountain,
  Wind,
  Sparkles,
}

export default function AboutPage() {
  const aboutConfig = siteConfig.about
  const skillList = aboutConfig.skills

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-14 animate-in fade-in duration-300">
      {/* 头部*/}
      <div className="bg-theme-surface/90 backdrop-blur-md rounded-3xl border border-theme-border/60 shadow-paper p-6 sm:p-10 relative overflow-hidden">
        {/* 顶部主题颜色条 */}
        <div className="absolute top-0 left-0 right-0 h-1.5 bg-theme-primary/80" />

        {/* 顶部 */}
        <div className="flex items-center justify-between pb-4 border-b border-theme-border/40 font-mono text-[11px] text-slate-400 dark:text-slate-500">
          <div className="flex items-center gap-1.5">
            <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
            <span className="text-theme-primary font-bold">{aboutConfig.cardTitle}</span>
          </div>
          <span className="tracking-wider">{aboutConfig.regNumber}</span>
        </div>

        {/* 博主基础资料 */}
        <div className="mt-6 flex flex-col sm:flex-row items-center gap-6 sm:gap-8">
          {/* 头像 */}
          <div className="relative w-28 h-28 rounded-full overflow-hidden ring-4 ring-theme-primary/30 shadow-xl flex-shrink-0">
            <Image
              src={siteConfig.avatar}
              alt={siteConfig.name}
              fill
              className="object-cover"
              priority
            />
          </div>

          {/* 博主信息 */}
          <div className="text-center sm:text-left flex-1">
            <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2.5">
              <h1 className="text-2xl sm:text-3xl font-black text-slate-900 dark:text-slate-100 tracking-tight">
                {siteConfig.name}
              </h1>
              <span className="text-xs px-2.5 py-0.5 rounded-md bg-theme-surface-hover border border-theme-border/80 text-theme-primary font-mono font-bold">
                {siteConfig.authorSubtitle}
              </span>
            </div>

            <p className="mt-3 text-xs sm:text-sm text-slate-600 dark:text-slate-300 leading-relaxed max-w-xl">
              {siteConfig.authorIntro}
            </p>
          </div>
        </div>

        {/* 座右铭 */}
        <div className="mt-6 p-4 rounded-2xl bg-theme-surface-hover/70 border border-theme-border/60 flex items-start sm:items-center gap-3">
          <Flame className="w-4 h-4 text-theme-primary fill-theme-primary/20 flex-shrink-0 mt-0.5 sm:mt-0" />
          <p className="text-xs sm:text-sm font-mono text-slate-700 dark:text-slate-300 leading-relaxed">
            <span className="font-bold text-theme-primary">座右铭：</span>
            {siteConfig.wisdom}
          </p>
        </div>

        {/* 外部联系按钮 */}
        <div className="mt-6 flex flex-wrap items-center justify-center sm:justify-start gap-3">
          <Link
            href={siteConfig.socials.github}
            target="_blank"
            rel="noopener noreferrer"
            className="px-4 py-2 bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary text-xs font-bold rounded-xl border border-theme-border/60 hover:border-theme-primary transition-colors flex items-center gap-1.5 shadow-2xs cursor-pointer"
          >
            <Github className="w-4 h-4" />
            <span>GitHub 开源代码库</span>
          </Link>
          <a
            href={siteConfig.socials.email}
            className="px-4 py-2 bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary text-xs font-bold rounded-xl border border-theme-border/60 hover:border-theme-primary transition-colors flex items-center gap-1.5 shadow-2xs cursor-pointer"
          >
            <Mail className="w-4 h-4" />
            <span>发送邮件</span>
          </a>
        </div>
      </div>

      {/* 技术栈描述 */}
      <div className="mt-10 bg-theme-surface/90 backdrop-blur-md rounded-3xl border border-theme-border/60 shadow-paper p-6 sm:p-8">
        <div className="flex items-center justify-between pb-4 border-b border-theme-border/40 mb-6">
          <div className="flex items-center gap-2">
            <div className="w-1.5 h-4 bg-theme-primary rounded-xs" />
            <h2 className="text-lg font-bold text-slate-800 dark:text-slate-100">
              {aboutConfig.matrixTitle}
            </h2>
          </div>
        </div>

        {/* 技术卡片网格 */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {skillList.map((item, idx) => {
            const Icon = (item.iconName && ICON_MAP[item.iconName]) || Sparkles
            return (
              <div
                key={idx}
                className="p-4 rounded-2xl bg-theme-surface-hover/50 border border-theme-border/50 hover:border-theme-primary transition-all flex flex-col justify-between group"
              >
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-[11px] font-mono font-bold text-slate-500 dark:text-slate-400">
                      {item.element}
                    </span>
                    <div className={`p-1.5 rounded-lg ${item.bg} ${item.color}`}>
                      <Icon className="w-4 h-4" />
                    </div>
                  </div>
                  <h3 className="text-base font-extrabold text-slate-800 dark:text-slate-100 group-hover:text-theme-primary transition-colors">
                    {item.value}
                  </h3>
                  <p className="mt-1 text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
                    {item.desc}
                  </p>
                </div>


              </div>
            )
          })}
        </div>

        {/* 底部架构说明 */}
        <div className="mt-6 p-4 rounded-2xl bg-theme-surface-hover/30 border border-theme-border/40 flex items-center gap-3 text-xs text-slate-600 dark:text-slate-400 font-mono">
          <ShieldCheck className="w-4 h-4 text-emerald-500 flex-shrink-0" />
          <span>{aboutConfig.systemAssurance}</span>
        </div>
      </div>
    </div>
  )
}
