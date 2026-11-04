'use client'

import React, { useEffect, useState, useRef } from 'react'
import { ChevronDown, ChevronUp, AlignLeft } from 'lucide-react'

export interface TocItem {
  id: string
  text: string
  level: number
}

interface TableOfContentsProps {
  items: TocItem[]
  mode?: 'desktop' | 'mobile' | 'all'
}

export default function TableOfContents({ items, mode = 'all' }: TableOfContentsProps) {
  const [activeId, setActiveId] = useState<string>('')
  const [isOpenMobile, setIsOpenMobile] = useState<boolean>(false)
  const navContainerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!items || items.length === 0) return

    const headingElements = items
      .map((item) => document.getElementById(item.id))
      .filter((el): el is HTMLElement => el !== null)

    if (headingElements.length === 0) return

    // 创建滚动观察器高亮当前阅读位置
    const observer = new IntersectionObserver(
      (entries) => {
        const visibleEntries = entries.filter((entry) => entry.isIntersecting)
        if (visibleEntries.length > 0) {
          const sorted = visibleEntries.sort(
            (a, b) => a.boundingClientRect.top - b.boundingClientRect.top
          )
          setActiveId(sorted[0].target.id)
        }
      },
      {
        rootMargin: '-80px 0px -70% 0px',
        threshold: 0,
      }
    )

    headingElements.forEach((el) => observer.observe(el))

    return () => {
      headingElements.forEach((el) => observer.unobserve(el))
      observer.disconnect()
    }
  }, [items])

  // 监听 activeId 变动并平滑滚动到视野内
  useEffect(() => {
    if (!activeId || !navContainerRef.current) return
    const activeLink = navContainerRef.current.querySelector<HTMLElement>(
      `[data-toc-id="${activeId}"]`
    )
    if (activeLink) {
      activeLink.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest',
      })
    }
  }, [activeId])

  if (!items || items.length === 0) return null

  const showMobile = mode === 'mobile' || mode === 'all'
  const showDesktop = mode === 'desktop' || mode === 'all'

  const handleLinkClick = (e: React.MouseEvent<HTMLAnchorElement>, id: string) => {
    e.preventDefault()
    const target = document.getElementById(id)
    if (target) {
      const topOffset = target.getBoundingClientRect().top + window.scrollY - 90
      window.scrollTo({ top: topOffset, behavior: 'smooth' })
      setActiveId(id)
      setIsOpenMobile(false)
    }
  }

  return (
    <>
      {/* 移动端：折叠目录 (< 1440px 显示) */}
      {showMobile && (
        <div className={`${mode === 'all' || mode === 'mobile' ? 'min-[1440px]:hidden' : ''} mb-8 bg-theme-surface-hover/80 rounded-2xl border border-theme-border/60 shadow-xs p-4`}>
          <button
            onClick={() => setIsOpenMobile((prev) => !prev)}
            className="w-full flex items-center justify-between text-xs font-bold text-slate-700 dark:text-slate-200 cursor-pointer"
          >
            <span className="flex items-center gap-2">
              <AlignLeft className="w-4 h-4 text-theme-primary" />
              <span>本文目录 ({items.length} 节)</span>
            </span>
            {isOpenMobile ? (
              <ChevronUp className="w-4 h-4 text-slate-400" />
            ) : (
              <ChevronDown className="w-4 h-4 text-slate-400" />
            )}
          </button>

          {isOpenMobile && (
            <nav className="mt-3 pt-3 border-t border-theme-border/50 space-y-1.5 max-h-64 overflow-y-auto">
              {items.map((item) => {
                const isActive = activeId === item.id
                return (
                  <a
                    key={item.id}
                    href={`#${item.id}`}
                    onClick={(e) => handleLinkClick(e, item.id)}
                    className={`block text-xs py-1.5 px-2.5 rounded-lg transition-colors leading-relaxed ${
                      item.level === 3 ? 'pl-6 text-slate-500 dark:text-slate-400' : 'font-medium'
                    } ${
                      isActive
                        ? 'bg-theme-surface text-theme-primary font-bold border-l-2 border-theme-primary'
                        : 'text-slate-600 dark:text-slate-300 hover:text-theme-primary hover:bg-theme-surface/60'
                    }`}
                  >
                    {item.text}
                  </a>
                )
              })}
            </nav>
          )}
        </div>
      )}

      {/* 桌面端吸顶目录 */}
      {showDesktop && (
        <div className="w-72 bg-theme-surface/95 backdrop-blur-md rounded-3xl border border-theme-border/60 shadow-paper p-5 transition-all">
          {/* 标题栏 */}
          <div className="flex items-center justify-between pb-3.5 border-b border-theme-border/40 mb-3.5">
            <div className="flex items-center gap-2">
              <AlignLeft className="w-4 h-4 text-theme-primary" />
              <span className="text-xs font-bold text-slate-800 dark:text-slate-100 tracking-wider">
                文章目录
              </span>
            </div>
            <span className="text-[10px] font-mono font-semibold text-theme-primary bg-theme-surface-hover px-2 py-0.5 rounded-md border border-theme-border/60">
              {items.length} 节
            </span>
          </div>

          {/* 目录列表 */}
          <nav
            ref={navContainerRef}
            className="space-y-1.5 max-h-[calc(100vh-200px)] overflow-y-auto pr-1 scroll-smooth"
          >
            {items.map((item) => {
              const isActive = activeId === item.id
              return (
                <a
                  key={item.id}
                  href={`#${item.id}`}
                  data-toc-id={item.id}
                  onClick={(e) => handleLinkClick(e, item.id)}
                  className={`block text-xs py-2 px-3 rounded-xl transition-all leading-relaxed ${
                    item.level === 3
                      ? 'pl-5 text-slate-500 dark:text-slate-400 text-[11px]'
                      : 'font-semibold text-slate-700 dark:text-slate-200'
                  } ${
                    isActive
                      ? 'bg-theme-surface-hover text-theme-primary font-bold shadow-2xs translate-x-1 border-l-2 border-theme-primary'
                      : 'hover:text-theme-primary hover:bg-theme-surface-hover/50'
                  }`}
                  title={item.text}
                >
                  <span className="line-clamp-2 break-words">{item.text}</span>
                </a>
              )
            })}
          </nav>
        </div>
      )}
    </>
  )
}
