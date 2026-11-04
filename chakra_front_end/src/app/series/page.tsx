import React, { Suspense } from 'react'
import Link from 'next/link'
import Image from 'next/image'
import type { Metadata } from 'next'
import { getSeriesList, isExternalUrl } from '@/lib/api'
import TacticalLoader from '@/components/loading/TacticalLoader'
import { Layers, ArrowRight } from 'lucide-react'

export const metadata: Metadata = {
  title: '专题专栏',
  description: '按体系梳理的系统性技术专题与深度实践指南。',
}

// 强制运行时动态获取，避免构建时因为内网不可达打包出空白页面
export const dynamic = 'force-dynamic'

/**
 * 专栏列表卡片网格异步子组件
 */
async function SeriesListContentAsync() {
  const { items: seriesList } = await getSeriesList()

  if (seriesList.length === 0) {
    return (
      <div className="text-center py-20 bg-theme-surface/90 rounded-3xl border border-theme-border/60 p-8 shadow-paper text-slate-400 dark:text-slate-500 text-xs font-mono">
        暂无专题专栏
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-8 animate-in fade-in duration-300">
      {seriesList.map((series) => (
        <div
          key={series.id}
          className="group flex flex-col bg-theme-surface/95 rounded-3xl border border-theme-border/60 shadow-paper hover:shadow-paper-hover transition-all duration-300 overflow-hidden"
        >
          {/* 专栏封面 */}
          <div className="relative w-full h-56 bg-slate-100 dark:bg-slate-800 overflow-hidden">
            <Image
              src={series.coverImage}
              alt={series.title}
              fill
              unoptimized={isExternalUrl(series.coverImage)}
              className="object-cover group-hover:scale-105 transition-transform duration-500"
              sizes="(max-width: 768px) 100vw, 600px"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-slate-900/70 via-slate-900/20 to-transparent" />

            <div className="absolute bottom-4 left-4 right-4 flex items-center justify-between text-white">
              <span className="px-2.5 py-1 bg-theme-primary text-white text-xs font-mono font-bold rounded-lg shadow-sm">
                {series.articleCount} 篇精选篇章
              </span>
              <span className="text-xs font-mono text-slate-200">
                更新于 {series.updatedAt}
              </span>
            </div>
          </div>

          {/* 专栏简介与目录预览 */}
          <div className="p-6 flex-1 flex flex-col justify-between">
            <div>
              <h3 className="text-lg sm:text-xl font-extrabold text-slate-800 dark:text-slate-100 group-hover:text-theme-primary transition-colors">
                <Link href={`/series/${series.id}`}>
                  {series.title}
                </Link>
              </h3>
              <p className="mt-2 text-xs sm:text-sm text-slate-500 dark:text-slate-400 leading-relaxed">
                {series.description}
              </p>

              {/* 篇章小预览 */}
              <div className="mt-5 space-y-2 bg-theme-surface-hover/60 p-3.5 rounded-2xl border border-theme-border/50">
                <span className="text-[11px] font-mono font-bold text-theme-primary block tracking-wider mb-1">
                  章节导览
                </span>
                {series.articles?.map((art, idx) => (
                  <Link
                    key={art.id}
                    href={`/articles/${art.id}`}
                    className="flex items-center justify-between text-xs text-slate-600 dark:text-slate-300 hover:text-theme-primary transition-colors py-1 group/item"
                  >
                    <span className="flex items-center gap-2 truncate">
                      <span className="w-5 h-5 rounded-full bg-theme-surface border border-theme-border/80 text-slate-400 dark:text-slate-500 group-hover/item:border-theme-primary group-hover/item:text-theme-primary text-[10px] font-mono font-bold flex items-center justify-center flex-shrink-0">
                        {idx + 1}
                      </span>
                      <span className="truncate">{art.title}</span>
                    </span>
                    <span className="text-[11px] font-mono text-slate-400 dark:text-slate-500 flex-shrink-0 ml-2">
                      {art.readTime}
                    </span>
                  </Link>
                ))}
              </div>
            </div>

            {/* 底部进入按钮 */}
            <div className="mt-6 pt-4 border-t border-theme-border/40 flex items-center justify-between">
              <div className="flex gap-1.5 flex-wrap">
                {series.tags?.map((tag, idx) => (
                  <span
                    key={idx}
                    className="text-[11px] font-mono text-slate-500 dark:text-slate-400 bg-theme-surface border border-theme-border/60 px-2 py-0.5 rounded"
                  >
                    #{tag}
                  </span>
                ))}
              </div>

              <Link
                href={`/series/${series.id}`}
                className="px-4 py-2 bg-theme-primary hover:bg-theme-primary-hover text-white text-xs font-bold rounded-xl transition-all flex items-center gap-1 shadow-xs border border-white/20"
              >
                <span>阅读专栏</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}

/**
 * 流式专栏列表页
 */
export default function SeriesPage() {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12">
      {/* 头部标题区 */}
      <div className="mb-10">
        <div className="flex items-center gap-2 mb-2">
          <span className="px-2.5 py-1 bg-theme-surface-hover text-theme-primary text-xs font-mono font-bold rounded-lg border border-theme-border/70">
            专题库
          </span>
        </div>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-slate-100 tracking-tight flex items-center gap-3">
          <Layers className="w-7 h-7 text-theme-primary" />
          <span>技术专栏与知识体系</span>
        </h1>
        <p className="mt-2 text-xs sm:text-sm text-slate-500 dark:text-slate-400 max-w-2xl">
          系统化的专题系列文章，帮助你体系化掌握各项核心技术与架构设计。
        </p>
      </div>

      {/* 专题专栏网格：流式加载 */}
      <Suspense
        fallback={
          <TacticalLoader
            title="LOADING..."
            subtitle="正在加载专栏列表..."
            mode="full"
          />
        }
      >
        <SeriesListContentAsync />
      </Suspense>
    </div>
  )
}
