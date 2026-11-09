import React from 'react'
import Link from 'next/link'
import { Flame, Sparkles, Github, User, Rss } from 'lucide-react'
import { siteConfig } from '@/config/site'
import { getSystemVisits } from '@/lib/api'

export default async function Footer() {
  let totalVisits: string | null = null
  try {
    totalVisits = await getSystemVisits()
  } catch {
    // 请求层已记录错误；非关键统计不可用时不阻断正文。
  }

  return (
    <footer className="border-t border-theme-border/50 bg-theme-surface/75 backdrop-blur-md transition-colors">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 items-center pb-8 border-b border-theme-border/40">
          {/* 左侧：站点介绍与寄语 */}
          <div className="flex flex-col gap-2 text-left">
            <div className="flex items-center gap-2 text-slate-800 dark:text-slate-100 font-bold text-sm">
              <Flame className="w-4 h-4 text-theme-primary fill-theme-primary/20" />
              <span>{siteConfig.workshopName}</span>
            </div>
            <p className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed">
              {siteConfig.quote}
            </p>
          </div>

          {/* 中间：系统访问量统计 */}
          <div className="flex flex-col items-center justify-center p-3 bg-theme-surface-hover rounded-xl border border-theme-border/60 shadow-2xs">
            <div className="flex items-center gap-2 text-xs font-mono text-slate-700 dark:text-slate-300">
              <Sparkles className="w-3.5 h-3.5 text-theme-primary" />
              <span>{siteConfig.stationStatus}</span>
            </div>
            <div className="mt-1 flex items-baseline gap-2 font-mono">
              <span className="text-xs text-slate-500 dark:text-slate-400">总访问量：</span>
              <span className="text-base font-extrabold text-theme-primary tracking-wider">
                {totalVisits === null ? '暂不可用' : BigInt(totalVisits).toLocaleString('zh-CN')}
              </span>
            </div>
          </div>

          {/* 右侧：快速入口与 Github / RSS */}
          <div className="flex items-center justify-start md:justify-end gap-3 text-xs text-slate-500">
            <Link
              href="/feed.xml"
              target="_blank"
              rel="alternate"
              type="application/rss+xml"
              className="px-3.5 py-2 rounded-lg bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary transition-colors border border-theme-border/60 shadow-2xs flex items-center gap-1.5"
            >
              <Rss className="w-3.5 h-3.5 text-amber-500" />
              <span>RSS 订阅</span>
            </Link>
            <Link
              href={siteConfig.socials.github}
              target="_blank"
              rel="noopener noreferrer"
              className="p-2 rounded-lg bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary transition-colors border border-theme-border/60 flex items-center gap-1.5 shadow-2xs"
            >
              <Github className="w-4 h-4" />
              <span>GitHub</span>
            </Link>
            <Link
              href="/about"
              className="px-3.5 py-2 rounded-lg bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary transition-colors border border-theme-border/60 shadow-2xs flex items-center gap-1.5"
            >
              <User className="w-3.5 h-3.5" />
              <span>关于博主</span>
            </Link>
          </div>
        </div>

        {/* 底部版权与寄语 */}
        <div className="pt-6 flex flex-col sm:flex-row items-center justify-between text-xs text-slate-400 dark:text-slate-500 gap-2 font-mono">
          <p>© {new Date().getFullYear()} {siteConfig.name}{siteConfig.footerMotto ? ` · ${siteConfig.footerMotto}` : ''}</p>
          <div className="flex items-center gap-1 text-[11px]">
            <span>{siteConfig.poweredBy}</span>
          </div>
        </div>
      </div>
    </footer>
  )
}
