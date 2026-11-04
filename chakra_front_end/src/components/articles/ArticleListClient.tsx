'use client'

import React, { useState, useTransition, useEffect } from 'react'
import { useRouter } from 'next/navigation'
import ArticleCard from '@/components/articles/ArticleCard'
import { Search, X, ChevronLeft, ChevronRight, FileQuestion } from 'lucide-react'
import { CATEGORIES } from '@/config/site'
import { Article, Category } from '@/types'

interface ArticleListClientProps {
  articles: Article[]
  total: number
  currentPage: number
  pageSize: number
  currentCategory: string
  currentKeyword: string
  categories?: Category[]
  categoryCounts?: Record<string, number>
}

type SortOption = 'newest' | 'views' | 'likes'

export default function ArticleListClient({
  articles = [],
  total = 0,
  currentPage = 1,
  pageSize = 9,
  currentCategory = 'all',
  currentKeyword = '',
  categories,
  categoryCounts = {},
}: ArticleListClientProps) {
  const router = useRouter()
  const [isPending, startTransition] = useTransition()
  const [search, setSearch] = useState<string>(currentKeyword)
  const [sortBy, setSortBy] = useState<SortOption>('newest')

  const categoryList: Category[] = categories && categories.length > 0
    ? [{ key: 'all', label: '全部文章', icon: 'Sparkles', count: total }, ...categories.filter(c => c.key !== 'all')]
    : CATEGORIES

  // 同步外部 keyword 变化
  useEffect(() => {
    setSearch(currentKeyword)
  }, [currentKeyword])

  // 根据当前客户端选中的排序对当前页数据进行简单顺序调整
  const sortedArticles = [...articles].sort((a, b) => {
    if (sortBy === 'views') {
      return (b.views || 0) - (a.views || 0)
    }
    if (sortBy === 'likes') {
      return (b.likes || 0) - (a.likes || 0)
    }
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  })

  // 总页数（基于后端的真实 total）
  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  // 更新路由参数辅助函数
  const updateQuery = (newCategory?: string, newKeyword?: string, newPage?: number) => {
    const params = new URLSearchParams()
    const targetCat = newCategory !== undefined ? newCategory : currentCategory
    const targetKw = newKeyword !== undefined ? newKeyword : currentKeyword
    const targetPage = newPage !== undefined ? newPage : 1

    if (targetCat && targetCat !== 'all') {
      params.set('category', targetCat)
    }
    if (targetKw && targetKw.trim()) {
      params.set('keyword', targetKw.trim())
    }
    if (targetPage > 1) {
      params.set('page', String(targetPage))
    }

    const qs = params.toString()
    startTransition(() => {
      router.push(`/articles${qs ? `?${qs}` : ''}`)
    })
  }

  // 切换分类
  const handleCategoryChange = (key: string) => {
    updateQuery(key, undefined, 1)
  }

  // 提交搜索
  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    updateQuery(undefined, search, 1)
  }

  // 清空搜索
  const handleClearSearch = () => {
    setSearch('')
    updateQuery(undefined, '', 1)
  }

  // 翻页
  const handlePageChange = (page: number) => {
    updateQuery(undefined, undefined, page)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  // 重置所有条件
  const handleResetAll = () => {
    setSearch('')
    updateQuery('all', '', 1)
  }

  return (
    <div>
      {/* 搜索与分类导航栏 */}
      <div className="bg-theme-surface/90 backdrop-blur-sm rounded-2xl border border-theme-border/60 p-5 shadow-paper mb-8 flex flex-col gap-4">
        <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4">
          {/* 分类标签组 */}
          <div className="flex flex-wrap items-center gap-1.5">
            {categoryList.map((cat) => {
              const isActive = (cat.key === 'all' && currentCategory === 'all') || currentCategory === cat.key
              const count = cat.count || categoryCounts[cat.key] || (cat.key === 'all' ? total : 0)
              return (
                <button
                  key={cat.key}
                  onClick={() => handleCategoryChange(cat.key)}
                  className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 ${
                    isActive
                      ? 'bg-theme-primary text-white shadow-xs border border-theme-primary'
                      : 'bg-theme-surface-hover/80 text-slate-700 dark:text-slate-300 hover:text-theme-primary border border-theme-border/50 hover:border-theme-primary'
                  }`}
                >
                  <span>{cat.label}</span>
                  <span
                    className={`text-[10px] font-mono px-1.5 py-0.5 rounded-md ${
                      isActive
                        ? 'bg-white/20 text-white'
                        : 'bg-theme-border/50 text-slate-600 dark:text-slate-300'
                    }`}
                  >
                    {count}
                  </span>
                </button>
              )
            })}
          </div>

          {/* 搜索输入框与排序选择 */}
          <div className="flex items-center gap-2">
            <form onSubmit={handleSearchSubmit} className="relative flex-1 sm:w-64">
              <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="搜索标题、关键词..."
                aria-label="搜索文章标题与关键词"
                className="w-full pl-9 pr-8 py-2 text-xs rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 focus:outline-none focus:ring-2 focus:ring-theme-primary focus:bg-white dark:focus:bg-slate-900 text-slate-800 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 transition-all font-sans"
              />
              {search && (
                <button
                  type="button"
                  onClick={handleClearSearch}
                  aria-label="清空搜索内容"
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 cursor-pointer"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              )}
            </form>

            {/* 排序按钮 */}
            <div className="flex items-center bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 p-0.5">
              {(['newest', 'views', 'likes'] as const).map((key) => {
                const label = key === 'newest' ? '最新' : key === 'views' ? '最热' : '点赞'
                return (
                  <button
                    key={key}
                    onClick={() => setSortBy(key)}
                    className={`px-2.5 py-1.5 text-[11px] font-semibold rounded-lg transition-colors cursor-pointer ${
                      sortBy === key
                        ? 'bg-white dark:bg-slate-700 text-theme-primary shadow-2xs font-bold'
                        : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200'
                    }`}
                  >
                    {label}
                  </button>
                )
              })}
            </div>
          </div>
        </div>

        {/* 检索结果统计 */}
        <div className="pt-3 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between text-xs font-mono">
          <span className="text-slate-400 dark:text-slate-500 flex items-center gap-1.5">
            {isPending && <span className="w-2 h-2 rounded-full bg-theme-primary animate-ping" />}
            <span>文章列表检索</span>
          </span>
          <div className="text-[11px] text-slate-400 dark:text-slate-500">
            共找到 <strong className="text-theme-primary font-bold">{total}</strong> 篇文章
          </div>
        </div>
      </div>

      {/* 文章卡片网格 */}
      {sortedArticles.length > 0 ? (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {sortedArticles.map((article) => (
              <ArticleCard key={article.id} article={article} />
            ))}
          </div>

          {/* 分页控制栏 (智能折叠防移动端溢出) */}
          {totalPages > 1 && (
            <div className="mt-12 flex flex-wrap items-center justify-center gap-1.5 sm:gap-2">
              <button
                onClick={() => handlePageChange(Math.max(1, currentPage - 1))}
                disabled={currentPage === 1 || isPending}
                aria-label="上一页"
                className="px-3 py-2 rounded-xl bg-theme-surface border border-theme-border/60 text-slate-600 dark:text-slate-300 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-theme-surface-hover hover:text-theme-primary transition-colors flex items-center gap-1 text-xs font-bold shadow-xs cursor-pointer"
              >
                <ChevronLeft className="w-4 h-4" />
                <span className="hidden sm:inline">上一页</span>
              </button>

              {/* 动态计算精简页码列表 */}
              {(() => {
                const pages: (number | string)[] = []
                if (totalPages <= 7) {
                  for (let i = 1; i <= totalPages; i++) pages.push(i)
                } else {
                  pages.push(1)
                  if (currentPage > 3) pages.push('ellipsis-left')
                  
                  const start = Math.max(2, currentPage - 1)
                  const end = Math.min(totalPages - 1, currentPage + 1)
                  for (let i = start; i <= end; i++) {
                    pages.push(i)
                  }
                  
                  if (currentPage < totalPages - 2) pages.push('ellipsis-right')
                  pages.push(totalPages)
                }

                return pages.map((p, idx) => {
                  if (typeof p === 'string') {
                    return (
                      <span
                        key={`${p}-${idx}`}
                        className="w-8 h-8 sm:w-9 sm:h-9 flex items-center justify-center text-xs font-mono text-slate-400 select-none"
                      >
                        …
                      </span>
                    )
                  }

                  const isCurrent = currentPage === p
                  return (
                    <button
                      key={p}
                      onClick={() => handlePageChange(p)}
                      disabled={isPending}
                      aria-current={isCurrent ? 'page' : undefined}
                      className={`w-8 h-8 sm:w-9 sm:h-9 rounded-xl text-xs font-mono font-bold transition-all cursor-pointer flex items-center justify-center ${
                        isCurrent
                          ? 'bg-theme-primary text-white shadow-xs border border-theme-primary'
                          : 'bg-theme-surface hover:bg-theme-surface-hover text-slate-600 dark:text-slate-300 border border-theme-border/60 shadow-2xs hover:border-theme-primary'
                      }`}
                    >
                      {p}
                    </button>
                  )
                })
              })()}

              <button
                onClick={() => handlePageChange(Math.min(totalPages, currentPage + 1))}
                disabled={currentPage === totalPages || isPending}
                aria-label="下一页"
                className="px-3 py-2 rounded-xl bg-theme-surface border border-theme-border/60 text-slate-600 dark:text-slate-300 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-theme-surface-hover hover:text-theme-primary transition-colors flex items-center gap-1 text-xs font-bold shadow-xs cursor-pointer"
              >
                <span className="hidden sm:inline">下一页</span>
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          )}
        </>
      ) : (
        /* 空状态提示 */
        <div className="text-center py-20 bg-theme-surface/90 rounded-3xl border border-theme-border/60 p-8 shadow-paper flex flex-col items-center justify-center">
          <div className="w-14 h-14 rounded-2xl bg-theme-surface-hover flex items-center justify-center text-theme-primary mb-2">
            <FileQuestion className="w-7 h-7" />
          </div>
          <h3 className="text-base font-bold text-slate-800 dark:text-slate-100 mt-2">
            暂无匹配的文章
          </h3>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 max-w-sm">
            未检索到与当前关键词或分类相关的文章，您可以尝试更换关键词或清除筛选条件。
          </p>
          {(search || currentCategory !== 'all') && (
            <button
              onClick={handleResetAll}
              className="mt-5 px-5 py-2.5 bg-theme-primary hover:bg-theme-primary-hover text-white text-xs font-bold rounded-xl shadow-xs border border-white/20 transition-all cursor-pointer"
            >
              清空全部筛选条件
            </button>
          )}
        </div>
      )}
    </div>
  )
}
