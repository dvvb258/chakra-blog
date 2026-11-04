import React from 'react'
import Link from 'next/link'
import { ArrowLeft, ArrowRight } from 'lucide-react'
import { Article } from '@/types'

interface ArticleNavigationProps {
  prevArticle?: Pick<Article, 'id' | 'title'> | null
  nextArticle?: Pick<Article, 'id' | 'title'> | null
}

export default function ArticleNavigation({ prevArticle, nextArticle }: ArticleNavigationProps) {
  if (!prevArticle && !nextArticle) return null

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mt-10 pt-8 border-t border-theme-border/40">
      {/* 上一篇 */}
      {prevArticle ? (
        <Link
          href={`/articles/${prevArticle.id}`}
          className="group flex flex-col p-4 rounded-2xl bg-theme-surface/90 hover:bg-theme-surface-hover border border-theme-border/60 hover:border-theme-primary shadow-paper hover:shadow-paper-hover transition-all text-left"
        >
          <div className="flex items-center gap-1.5 text-xs font-mono text-slate-400 dark:text-slate-500 group-hover:text-theme-primary transition-colors mb-1.5">
            <ArrowLeft className="w-3.5 h-3.5 transition-transform group-hover:-translate-x-1" />
            <span>上一篇</span>
          </div>
          <h4 className="text-sm font-bold text-slate-800 dark:text-slate-100 group-hover:text-theme-primary transition-colors line-clamp-1">
            {prevArticle.title}
          </h4>
        </Link>
      ) : (
        <div className="hidden sm:flex p-4 rounded-2xl border border-dashed border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/30 text-xs font-mono text-slate-400 dark:text-slate-500 items-center justify-center">
          <span>已经是第一篇文章</span>
        </div>
      )}

      {/* 下一篇 */}
      {nextArticle ? (
        <Link
          href={`/articles/${nextArticle.id}`}
          className="group flex flex-col p-4 rounded-2xl bg-theme-surface/90 hover:bg-theme-surface-hover border border-theme-border/60 hover:border-theme-primary shadow-paper hover:shadow-paper-hover transition-all text-right sm:text-right"
        >
          <div className="flex items-center justify-end gap-1.5 text-xs font-mono text-slate-400 dark:text-slate-500 group-hover:text-theme-primary transition-colors mb-1.5">
            <span>下一篇</span>
            <ArrowRight className="w-3.5 h-3.5 transition-transform group-hover:translate-x-1" />
          </div>
          <h4 className="text-sm font-bold text-slate-800 dark:text-slate-100 group-hover:text-theme-primary transition-colors line-clamp-1">
            {nextArticle.title}
          </h4>
        </Link>
      ) : (
        <div className="hidden sm:flex p-4 rounded-2xl border border-dashed border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/30 text-xs font-mono text-slate-400 dark:text-slate-500 items-center justify-center">
          <span>已经是最新一篇文章</span>
        </div>
      )}
    </div>
  )
}
