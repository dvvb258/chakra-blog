import React from 'react'
import Link from 'next/link'
import Image from 'next/image'
import { Calendar, Eye, Pin, Clock } from 'lucide-react'
import Badge from '@/components/ui/Badge'
import { Article } from '@/types'
import { resolveCoverImage, isExternalUrl } from '@/lib/api'
import { siteConfig } from '@/config/site'
import { formatReadingTime } from '@/lib/reading'

interface ArticleCardProps {
  article: Article
}

export default function ArticleCard({ article }: ArticleCardProps) {
  const {
    id,
    title,
    summary,
    coverImage,
    categoryName,
    tags = [],
    author,
    readingTime,
    createdAt,
    views = 0,
    pinned = false,
  } = article

  const readingTimeText = formatReadingTime(readingTime)

  return (
    <article className="group relative flex flex-col bg-theme-surface/90 backdrop-blur-sm rounded-2xl border border-theme-border/60 shadow-paper hover:shadow-paper-hover hover:border-theme-primary hover:shadow-[0_8px_25px_rgba(var(--theme-shadow-rgb),0.15)] transition-all duration-300 hover:-translate-y-1 overflow-hidden">
      {/* 顶部置顶微标 */}
      {pinned && (
        <div className="absolute top-3 left-3 z-10 flex items-center gap-1 px-2.5 py-1 bg-theme-primary text-white text-[11px] font-bold font-mono rounded-lg shadow-xs border border-white/20">
          <Pin className="w-3 h-3 fill-current" />
          <span>置顶</span>
        </div>
      )}

      {/* 封面图区域 */}
      <Link
        href={`/articles/${id}`}
        className="relative block w-full h-48 sm:h-52 overflow-hidden bg-slate-100 dark:bg-slate-800"
      >
        <Image
          src={resolveCoverImage(coverImage)}
          alt={title}
          fill
          unoptimized={isExternalUrl(resolveCoverImage(coverImage))}
          className="object-cover object-center transition-transform duration-500 group-hover:scale-105"
          sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-slate-950/60 via-transparent to-transparent opacity-60 group-hover:opacity-40 transition-opacity" />

        {/* 右下角分类浮标 */}
        {categoryName && (
          <div className="absolute bottom-3 right-3 z-10">
            <span className="flex items-center gap-1 px-2.5 py-1 bg-theme-surface/95 backdrop-blur-md text-theme-primary text-xs font-semibold font-mono rounded-lg shadow-sm border border-theme-border/80">
              <span>{categoryName}</span>
            </span>
          </div>
        )}
      </Link>

      {/* 内容区域 */}
      <div className="flex-1 p-5 flex flex-col justify-between">
        <div>
          {/* 元信息：发布日期与预计阅读时长 */}
          <div className="flex flex-wrap items-center gap-2.5 text-xs font-mono mb-2.5">
            <span className="flex items-center gap-1 text-slate-400 dark:text-slate-500">
              <Calendar className="w-3.5 h-3.5 text-theme-primary/80" />
              <span>{createdAt}</span>
            </span>
            <span className="text-slate-300 dark:text-slate-700">•</span>
            {/* 阅读时长（标准通用技术功能） */}
            <span className="flex items-center gap-1 px-2 py-0.5 rounded-md border text-[11px] font-semibold text-theme-primary bg-theme-surface-hover border-theme-border/70">
              <Clock className="w-3 h-3 text-theme-primary" />
              <span>{readingTimeText}</span>
            </span>
            <span className="text-slate-300 dark:text-slate-700">•</span>
            <span className="flex items-center gap-1 text-slate-400 dark:text-slate-500">
              <Eye className="w-3.5 h-3.5 text-slate-400 dark:text-slate-500" />
              <span>{views} 阅读</span>
            </span>
          </div>

          {/* 标题 */}
          <h3 className="text-base sm:text-lg font-bold text-slate-800 dark:text-slate-100 group-hover:text-theme-primary transition-colors line-clamp-2 leading-snug">
            <Link href={`/articles/${id}`}>{title}</Link>
          </h3>

          {/* 摘要 */}
          <p className="mt-2 text-xs sm:text-sm text-slate-600 dark:text-slate-400 line-clamp-2 leading-relaxed">
            {summary}
          </p>
        </div>

        {/* 底部：标签与作者 */}
        <div className="mt-4 pt-3 border-t border-theme-border/40 flex items-center justify-between gap-2">
          <div className="flex flex-wrap gap-1.5 overflow-hidden">
            {tags.slice(0, 3).map((tag, idx) => (
              <Badge key={idx} variant={idx % 2 === 0 ? 'theme' : 'slate'}>
                #{tag}
              </Badge>
            ))}
          </div>

          <span className="text-[11px] font-mono text-slate-400 dark:text-slate-500 whitespace-nowrap">
            by {author || siteConfig.name}
          </span>
        </div>
      </div>
    </article>
  )
}
