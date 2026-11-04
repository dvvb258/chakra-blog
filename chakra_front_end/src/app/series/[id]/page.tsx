import React, { Suspense } from 'react'
import Link from 'next/link'
import Image from 'next/image'
import { notFound } from 'next/navigation'
import type { Metadata } from 'next'
import { getSeriesById, isExternalUrl } from '@/lib/api'
import TacticalLoader from '@/components/loading/TacticalLoader'
import { Clock, ArrowRight, ArrowLeft, ChevronRight } from 'lucide-react'
import { siteConfig } from '@/config/site'

interface PageProps {
  params: Promise<{ id: string }>
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { id } = await params
  try {
    const series = await getSeriesById(id)
    if (!series) return { title: '专栏未找到' }

    return {
      title: `${series.title} · 专题专栏 · ${siteConfig.name}`,
      description: series.description,
      openGraph: {
        title: series.title,
        description: series.description,
        type: 'website',
        images: [{ url: series.coverImage }],
      },
    }
  } catch {
    return { title: '专题专栏详情' }
  }
}

/**
 * 核心专栏异步流式子组件
 */
async function SeriesMainContentAsync({ id }: { id: string }) {
  const series = await getSeriesById(id)

  if (!series) {
    notFound()
  }

  return (
    <div className="animate-in fade-in duration-300">
      {/* 专栏头部巨幕 */}
      <div className="bg-theme-surface/95 rounded-3xl border border-theme-border/60 shadow-paper overflow-hidden mb-10">
        <div className="relative w-full h-60 sm:h-72 bg-slate-100 dark:bg-slate-800">
          <Image
            src={series.coverImage}
            alt={series.title}
            fill
            priority
            unoptimized={isExternalUrl(series.coverImage)}
            className="object-cover"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-slate-900/80 via-slate-900/30 to-transparent" />

          <div className="absolute bottom-6 left-6 right-6 text-white">
            <div className="flex items-center gap-2 mb-2 font-mono text-xs">
              <span className="px-2.5 py-0.5 bg-theme-primary text-white rounded font-bold">
                专栏索引
              </span>
              <span className="text-slate-200">更新于 {series.updatedAt}</span>
            </div>
            <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight">
              {series.title}
            </h1>
            <p className="mt-2 text-xs sm:text-sm text-slate-200 max-w-2xl">
              {series.description}
            </p>
          </div>
        </div>
      </div>

      {/* 章节路线图列表 */}
      <div className="bg-theme-surface/95 rounded-3xl border border-theme-border/60 shadow-paper p-6 sm:p-10">
        <div className="flex items-center justify-between pb-6 border-b border-theme-border/40">
          <div className="flex items-center gap-2">
            <div className="w-1.5 h-4 bg-theme-primary rounded-xs" />
            <h2 className="text-lg font-bold text-slate-800 dark:text-slate-100">
              专栏篇章路线图
            </h2>
          </div>
          <span className="text-xs font-mono text-slate-400 dark:text-slate-500">
            共收录 {series.articles?.length || 0} 篇文章
          </span>
        </div>

        {/* 路线图时间轴 */}
        <div className="mt-8 space-y-6 relative before:absolute before:inset-0 before:left-4 before:w-0.5 before:bg-theme-border/40">
          {series.articles?.map((art, idx) => (
            <div key={art.id} className="relative flex items-start gap-6 group">
              {/* 序号气泡 */}
              <div className="w-8 h-8 rounded-full bg-theme-surface border-2 border-theme-border hover:border-theme-primary text-theme-primary font-mono font-bold text-xs flex items-center justify-center shadow-xs flex-shrink-0 z-10 group-hover:bg-theme-primary group-hover:text-white transition-all">
                {idx + 1}
              </div>

              {/* 篇章卡片 */}
              <Link
                href={`/articles/${art.id}`}
                className="flex-1 p-4 bg-slate-50 dark:bg-slate-800/60 hover:bg-theme-surface-hover rounded-2xl border border-slate-200/70 dark:border-slate-700 hover:border-theme-primary transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs"
              >
                <div>
                  <span className="text-[11px] font-mono text-theme-primary font-bold block mb-1">
                    第 {String(idx + 1).padStart(2, '0')} 讲
                  </span>
                  <h3 className="text-sm font-bold text-slate-800 dark:text-slate-100 group-hover:text-theme-primary transition-colors">
                    {art.title}
                  </h3>
                </div>

                <div className="flex items-center gap-3 text-xs font-mono text-slate-400 dark:text-slate-500 flex-shrink-0">
                  <span className="flex items-center gap-1">
                    <Clock className="w-3.5 h-3.5 text-theme-primary" />
                    <span>{art.readTime}</span>
                  </span>
                  <span className="px-3 py-1.5 bg-theme-surface rounded-lg border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 group-hover:bg-theme-primary group-hover:text-white group-hover:border-transparent text-xs font-bold transition-all flex items-center gap-1 shadow-xs">
                    <span>开始阅读</span>
                    <ArrowRight className="w-3.5 h-3.5" />
                  </span>
                </div>
              </Link>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

/**
 * 流式专栏详情页
 */
export default async function SeriesDetailPage({ params }: PageProps) {
  const { id } = await params

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12">
      {/* 面包屑 */}
      <nav className="flex items-center gap-2 text-xs text-slate-400 dark:text-slate-500 font-mono mb-6">
        <Link href="/" className="hover:text-theme-primary transition-colors">
          首页
        </Link>
        <ChevronRight className="w-3.5 h-3.5 text-slate-300 dark:text-slate-600" />
        <Link href="/series" className="hover:text-theme-primary transition-colors">
          专题专栏
        </Link>
        <ChevronRight className="w-3.5 h-3.5 text-slate-300 dark:text-slate-600" />
        <span className="text-slate-600 dark:text-slate-300 truncate max-w-xs">专栏详情</span>
      </nav>

      {/* 专栏主体 */}
      <Suspense
        fallback={
          <TacticalLoader
            title="LOADING..."
            subtitle="正在加载专栏内容..."
            mode="full"
          />
        }
      >
        <SeriesMainContentAsync id={id} />
      </Suspense>

      {/* 底部返回全部专栏 */}
      <div className="mt-10 text-center">
        <Link
          href="/series"
          className="inline-flex items-center gap-2 px-5 py-2.5 bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary rounded-xl font-bold text-xs border border-theme-border/60 hover:border-theme-primary transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>返回全部专栏列表</span>
        </Link>
      </div>
    </div>
  )
}
