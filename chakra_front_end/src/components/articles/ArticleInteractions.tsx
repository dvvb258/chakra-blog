'use client'

import React, { useState, useEffect } from 'react'
import { Heart, Share2, Check } from 'lucide-react'
import { recordArticleView, likeArticle } from '@/lib/api'

interface ArticleInteractionsProps {
  articleId?: string
  initialLikes?: number
  title?: string
}

export default function ArticleInteractions({
  articleId,
  initialLikes = 0,
  title,
}: ArticleInteractionsProps) {
  const [likes, setLikes] = useState<number>(initialLikes)
  const [hasLiked, setHasLiked] = useState<boolean>(false)
  const [copied, setCopied] = useState<boolean>(false)

  // 阅读量上报与点赞记忆恢复
  useEffect(() => {
    if (!articleId) return

    const viewKey = `viewed_art_${articleId}`
    if (!sessionStorage.getItem(viewKey)) {
      sessionStorage.setItem(viewKey, 'pending')
      void recordArticleView(articleId)
        .then(() => sessionStorage.setItem(viewKey, '1'))
        .catch((error: unknown) => {
          sessionStorage.removeItem(viewKey)
          console.error('阅读量上报失败', error)
        })
    }

    const likeKey = `liked_art_${articleId}`
    if (localStorage.getItem(likeKey) === 'true') {
      setHasLiked(true)
    }
  }, [articleId])

  const handleLike = () => {
    const nextState = !hasLiked
    setHasLiked(nextState)
    setLikes((prev) => (nextState ? prev + 1 : Math.max(0, prev - 1)))

    if (articleId) {
      const likeKey = `liked_art_${articleId}`
      if (nextState) {
        localStorage.setItem(likeKey, 'true')
        void likeArticle(articleId).catch((err: unknown) => {
          console.error('点赞上报失败', err)
        })
      } else {
        localStorage.removeItem(likeKey)
      }
    }
  }

  const handleCopy = () => {
    if (typeof window !== 'undefined') {
      const shareText = title ? `${title} · ${window.location.href}` : window.location.href
      navigator.clipboard.writeText(shareText)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    }
  }

  return (
    <div className="flex flex-wrap items-center justify-between gap-4 p-5 bg-theme-surface-hover/80 rounded-2xl border border-theme-border/60 mt-10">
      <div className="flex items-center gap-3">
        <button
          onClick={handleLike}
          type="button"
          aria-label={hasLiked ? '取消点赞' : '点赞本文'}
          className={`flex items-center gap-2 px-4 py-2.5 rounded-xl font-bold text-xs transition-all shadow-xs active:scale-95 cursor-pointer ${
            hasLiked
              ? 'bg-theme-primary text-white ring-2 ring-theme-primary/40 shadow-xs'
              : 'bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary border border-theme-border/60'
          }`}
        >
          <Heart className={`w-4 h-4 ${hasLiked ? 'fill-current' : 'text-theme-primary'}`} />
          <span>{hasLiked ? '已点赞' : '点赞支持'}</span>
          <span className="font-mono bg-black/10 dark:bg-white/10 px-1.5 py-0.5 rounded text-[11px]">
            {likes}
          </span>
        </button>

        <button
          onClick={handleCopy}
          type="button"
          aria-label="复制本文链接"
          className="flex items-center gap-2 px-3.5 py-2.5 rounded-xl font-medium text-xs bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary border border-theme-border/60 transition-all active:scale-95 shadow-xs cursor-pointer"
        >
          {copied ? (
            <>
              <Check className="w-4 h-4 text-emerald-500" />
              <span className="text-emerald-600 dark:text-emerald-400 font-bold">已复制链接!</span>
            </>
          ) : (
            <>
              <Share2 className="w-4 h-4 text-slate-400" />
              <span>分享本文</span>
            </>
          )}
        </button>
      </div>
    </div>
  )
}
