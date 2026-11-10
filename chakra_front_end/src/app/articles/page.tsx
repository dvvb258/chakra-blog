import React, { Suspense } from 'react'
import type { Metadata } from 'next'
import { getArticles, getCategories } from '@/lib/api'
import ArticleListClient from '@/components/articles/ArticleListClient'
import TacticalLoader from '@/components/loading/TacticalLoader'
import { BookOpen } from 'lucide-react'

export const metadata: Metadata = {
  title: '全部文章归档',
  description: '浏览关于 .NET、架构设计、云原生与AI Agent的全部技术文章与思考。',
}

type ArticlesPageProps = {
  searchParams: Promise<{
    category?: string
    keyword?: string
    page?: string
    sortBy?: string
  }>
}

/**
 * 文章列表数据获取与交互组件
 */
async function ArticlesContentAsync({
  searchParams,
}: {
  searchParams: Promise<{
    category?: string
    keyword?: string
    page?: string
    sortBy?: string
  }>
}) {
  const resolvedParams = await searchParams
  const currentCategory = resolvedParams.category || 'all'
  const currentKeyword = resolvedParams.keyword || ''
  const currentPage = Math.max(1, Number(resolvedParams.page) || 1)
  const rawSort = resolvedParams.sortBy?.toLowerCase()
  const currentSortBy: 'latest' | 'views' | 'likes' =
    rawSort === 'views' ? 'views' : rawSort === 'likes' ? 'likes' : 'latest'
  const pageSize = 9

  // 并行请求：当前筛选条件下的真实文章列表，以及全部分类列表与统计
  const [articlesRes, categories] = await Promise.all([
    getArticles({
      category: currentCategory === 'all' ? undefined : currentCategory,
      keyword: currentKeyword || undefined,
      page: currentPage,
      pageSize,
      sortBy: currentSortBy,
    }),
    getCategories(),
  ])

  return (
    <ArticleListClient
      articles={articlesRes.items}
      total={articlesRes.total}
      currentPage={currentPage}
      pageSize={pageSize}
      currentCategory={currentCategory}
      currentKeyword={currentKeyword}
      currentSortBy={currentSortBy}
      categories={categories}
    />
  )
}

/**
 * 文章归档列表页面
 */
export default function ArticlesPage({ searchParams }: ArticlesPageProps) {
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12">
      {/* 头部标题区 */}
      <div className="mb-8">
        <div className="flex items-center gap-2 mb-2">
          <span className="px-2.5 py-1 bg-theme-surface-hover text-theme-primary text-xs font-mono font-bold rounded-lg border border-theme-border/70">
            全部归档
          </span>
        </div>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-slate-100 tracking-tight flex items-center gap-3">
          <BookOpen className="w-7 h-7 text-theme-primary" />
          <span>技术文章与笔记</span>
        </h1>
        <p className="mt-2 text-xs sm:text-sm text-slate-500 dark:text-slate-400 max-w-2xl">
          记录技术实践与开发心得。涵盖现代化编程、云原生与系统架构设计。
        </p>
      </div>

      {/* 交互式文章列表 */}
      <Suspense
        fallback={
          <TacticalLoader
            mode="inline"
            title="LOADING ARTICLES..."
            subtitle="正在检索文章索引，请稍候..."
          />
        }
      >
        <ArticlesContentAsync searchParams={searchParams} />
      </Suspense>
    </div>
  )
}
