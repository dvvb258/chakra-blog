'use client'

import React, { useState, useEffect, useRef, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { Search, Loader2, X, Pin, ArrowRight, BookOpen, Clock } from 'lucide-react'
import { getArticles } from '@/lib/api'
import { Article } from '@/types'

interface GlobalSearchModalProps {
  isOpen: boolean
  onClose: () => void
}

export default function GlobalSearchModal({ isOpen, onClose }: GlobalSearchModalProps) {
  const router = useRouter()
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<Article[]>([])
  const [loading, setLoading] = useState(false)
  const [selectedIndex, setSelectedIndex] = useState(0)

  const inputRef = useRef<HTMLInputElement>(null)
  const resultsContainerRef = useRef<HTMLDivElement>(null)

  // 聚焦输入框与锁定背景滚动
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden'
      setTimeout(() => inputRef.current?.focus(), 50)
      // 打开时若无搜索词，默认加载最新文章作为推荐
      setLoading(true)
      getArticles({ pageSize: 5, sortBy: 'latest' })
        .then((res) => {
          setResults(res.items || [])
          setSelectedIndex(0)
        })
        .catch(() => {
          setResults([])
        })
        .finally(() => {
          setLoading(false)
        })
    } else {
      document.body.style.overflow = ''
      setQuery('')
      setResults([])
      setSelectedIndex(0)
    }
    return () => {
      document.body.style.overflow = ''
    }
  }, [isOpen])

  // 防抖搜索
  useEffect(() => {
    if (!isOpen) return
    const trimmed = query.trim()
    if (!trimmed) {
      // 恢复为最新推荐
      setLoading(true)
      getArticles({ pageSize: 5, sortBy: 'latest' })
        .then((res) => {
          setResults(res.items || [])
          setSelectedIndex(0)
        })
        .catch(() => {
          setResults([])
        })
        .finally(() => {
          setLoading(false)
        })
      return
    }

    setLoading(true)
    const timer = setTimeout(() => {
      getArticles({ keyword: trimmed, pageSize: 8 })
        .then((res) => {
          setResults(res.items || [])
          setSelectedIndex(0)
        })
        .catch(() => {
          setResults([])
        })
        .finally(() => {
          setLoading(false)
        })
    }, 250)

    return () => clearTimeout(timer)
  }, [query, isOpen])

  const navigateToArticle = useCallback(
    (article: Article) => {
      onClose()
      router.push(`/articles/${article.id}`)
    },
    [onClose, router]
  )

  // 键盘快捷键导航 (↑, ↓, Enter, Escape)
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      e.preventDefault()
      onClose()
    } else if (e.key === 'ArrowDown') {
      e.preventDefault()
      if (results.length > 0) {
        setSelectedIndex((prev) => (prev + 1) % results.length)
      }
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      if (results.length > 0) {
        setSelectedIndex((prev) => (prev - 1 + results.length) % results.length)
      }
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (results[selectedIndex]) {
        navigateToArticle(results[selectedIndex])
      }
    }
  }

  // 保证选中的结果在可视区域内
  useEffect(() => {
    if (resultsContainerRef.current) {
      const activeEl = resultsContainerRef.current.querySelector(`[data-index="${selectedIndex}"]`)
      if (activeEl) {
        activeEl.scrollIntoView({ block: 'nearest' })
      }
    }
  }, [selectedIndex])

  if (!isOpen) return null

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="全局文章搜索"
      className="fixed inset-0 z-50 flex items-start justify-center pt-16 sm:pt-24 px-4 bg-black/60 backdrop-blur-sm transition-all animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-2xl bg-theme-surface border border-theme-border/80 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[80vh] animate-in zoom-in-95 duration-150"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={handleKeyDown}
      >
        {/* 顶部搜索输入栏 */}
        <div className="flex items-center px-4 py-3.5 border-b border-theme-border/70 bg-theme-surface/90">
          <Search className="w-5 h-5 text-theme-primary shrink-0 mr-3" />
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="搜索全站文章标题、内容关键词..."
            className="flex-1 bg-transparent text-slate-800 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 text-sm sm:text-base outline-none border-none focus:ring-0"
          />

          {loading ? (
            <Loader2 className="w-4 h-4 text-theme-primary animate-spin shrink-0 ml-2" />
          ) : query ? (
            <button
              type="button"
              onClick={() => {
                setQuery('')
                inputRef.current?.focus()
              }}
              className="p-1 rounded-md text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-theme-surface-hover shrink-0 ml-1 transition-colors"
              aria-label="清空搜索"
            >
              <X className="w-4 h-4" />
            </button>
          ) : null}

          <button
            type="button"
            onClick={onClose}
            className="hidden sm:inline-flex items-center ml-2 px-1.5 py-0.5 text-[11px] font-mono text-slate-400 bg-theme-surface-hover border border-theme-border/80 rounded-md"
          >
            ESC
          </button>
        </div>

        {/* 搜索结果或最新推荐列表 */}
        <div ref={resultsContainerRef} className="overflow-y-auto p-2 sm:p-3 divide-y divide-theme-border/30">
          {results.length > 0 ? (
            <>
              <div className="px-3 py-1.5 text-xs font-semibold text-slate-400 dark:text-slate-500 flex items-center justify-between">
                <span>{query.trim() ? `搜索结果 (${results.length})` : '最新发布推荐'}</span>
                <span className="text-[11px] font-normal text-slate-400">使用 ↑ ↓ 键选择，Enter 打开</span>
              </div>
              <ul className="space-y-1 mt-1">
                {results.map((article, idx) => {
                  const isSelected = idx === selectedIndex
                  return (
                    <li key={article.id}>
                      <button
                        type="button"
                        data-index={idx}
                        onClick={() => navigateToArticle(article)}
                        onMouseEnter={() => setSelectedIndex(idx)}
                        className={`w-full text-left p-3 rounded-xl transition-all flex items-start justify-between gap-3 ${
                          isSelected
                            ? 'bg-theme-primary text-white shadow-xs'
                            : 'hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200'
                        }`}
                      >
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 mb-1">
                            {article.pinned && (
                              <span
                                className={`inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-[10px] font-bold font-mono ${
                                  isSelected ? 'bg-white/20 text-white' : 'bg-theme-primary text-white'
                                }`}
                              >
                                <Pin className="w-2.5 h-2.5 fill-current" />
                                置顶
                              </span>
                            )}
                            {article.categoryName && (
                              <span
                                className={`text-[11px] px-2 py-0.5 rounded-full font-medium ${
                                  isSelected
                                    ? 'bg-white/20 text-white'
                                    : 'bg-theme-surface-hover text-theme-primary border border-theme-border/60'
                                }`}
                              >
                                {article.categoryName}
                              </span>
                            )}
                            <h4 className="font-semibold text-sm truncate">{article.title}</h4>
                          </div>

                          {article.summary && (
                            <p
                              className={`text-xs line-clamp-1 ${
                                isSelected ? 'text-white/80' : 'text-slate-500 dark:text-slate-400'
                              }`}
                            >
                              {article.summary}
                            </p>
                          )}
                        </div>

                        <div className="shrink-0 flex items-center gap-2 text-xs pt-0.5">
                          {article.readingTime ? (
                            <span
                              className={`hidden sm:inline-flex items-center gap-1 ${
                                isSelected ? 'text-white/70' : 'text-slate-400'
                              }`}
                            >
                              <Clock className="w-3 h-3" />
                              {article.readingTime} 分钟
                            </span>
                          ) : null}
                          <ArrowRight
                            className={`w-4 h-4 transition-transform ${
                              isSelected ? 'text-white translate-x-0.5' : 'text-slate-400'
                            }`}
                          />
                        </div>
                      </button>
                    </li>
                  )
                })}
              </ul>
            </>
          ) : !loading ? (
            <div className="py-12 px-4 text-center">
              <BookOpen className="w-10 h-10 text-slate-300 dark:text-slate-600 mx-auto mb-3" />
              <p className="text-sm font-medium text-slate-600 dark:text-slate-300">
                {query.trim() ? `未找到与 “${query}” 相关的文章` : '暂无文章数据'}
              </p>
              <p className="text-xs text-slate-400 mt-1">请尝试更换关键词，或者搜索分类/技术标签</p>
            </div>
          ) : null}
        </div>

        {/* 底部快捷键提示栏 */}
        <div className="px-4 py-2.5 border-t border-theme-border/50 bg-theme-surface-hover/50 text-[11px] text-slate-400 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 bg-theme-surface border border-theme-border/80 rounded font-mono shadow-2xs">
                ↑
              </kbd>
              <kbd className="px-1.5 py-0.5 bg-theme-surface border border-theme-border/80 rounded font-mono shadow-2xs">
                ↓
              </kbd>
              <span>选择</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 bg-theme-surface border border-theme-border/80 rounded font-mono shadow-2xs">
                ↵
              </kbd>
              <span>打开</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 bg-theme-surface border border-theme-border/80 rounded font-mono shadow-2xs">
                ESC
              </kbd>
              <span>关闭</span>
            </span>
          </div>

          <Link
            href="/articles"
            onClick={onClose}
            className="hover:text-theme-primary transition-colors flex items-center gap-1"
          >
            <span>进入文章列表页</span>
            <ArrowRight className="w-3 h-3" />
          </Link>
        </div>
      </div>
    </div>
  )
}
