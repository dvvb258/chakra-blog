'use client'

import React, { useState, useEffect, useRef } from 'react'
import { Heart, Share2, Check } from 'lucide-react'
import { recordArticleView, likeArticle, unlikeArticle } from '@/lib/api'

interface ArticleInteractionsProps {
  articleId?: string
  initialLikes?: number
  title?: string
}

function normalizeLikeCount(value: unknown): number {
  const num = Number(value)
  return Number.isFinite(num) && num >= 0 ? Math.floor(num) : 0
}

function readStorage(storage: Storage, key: string): string | null {
  try {
    return storage.getItem(key)
  } catch {
    return null
  }
}

function writeStorage(storage: Storage, key: string, value: string): void {
  try {
    storage.setItem(key, value)
  } catch {
    // 隐私模式或禁用本地存储时静默降级
  }
}

function removeStorage(storage: Storage, key: string): void {
  try {
    storage.removeItem(key)
  } catch {
    // 隐私模式或禁用本地存储时静默降级
  }
}

export default function ArticleInteractions({
  articleId,
  initialLikes = 0,
  title,
}: ArticleInteractionsProps) {
  const safeInitialLikes = normalizeLikeCount(initialLikes)
  const [likes, setLikes] = useState<number>(safeInitialLikes)
  const [hasLiked, setHasLiked] = useState<boolean>(false)
  const [copied, setCopied] = useState<boolean>(false)
  const [likeError, setLikeError] = useState<string | null>(null)

  // 记录初始化/服务端基准点赞状态，用于 500ms 防抖合并及失败回滚
  const initialLikedRef = useRef<boolean>(false)
  const serverLikedRef = useRef<boolean>(false)
  const interactionSeqRef = useRef<number>(0)
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const copyTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const errorTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const mountedRef = useRef<boolean>(true)

  const computeDisplayLikes = (targetLiked: boolean): number => {
    const delta = (targetLiked ? 1 : 0) - (initialLikedRef.current ? 1 : 0)
    return Math.max(0, safeInitialLikes + delta)
  }

  // 阅读量上报与点赞记忆恢复（articleId 或 initialLikes 变化时重置本地基准）
  useEffect(() => {
    setLikes(safeInitialLikes)
    setLikeError(null)
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current)
      debounceTimerRef.current = null
    }

    if (!articleId) return

    const viewKey = `viewed_art_${articleId}`
    if (!readStorage(window.sessionStorage, viewKey)) {
      writeStorage(window.sessionStorage, viewKey, 'pending')
      void recordArticleView(articleId)
        .then(() => writeStorage(window.sessionStorage, viewKey, '1'))
        .catch((error: unknown) => {
          removeStorage(window.sessionStorage, viewKey)
          console.error('阅读量上报失败', error)
        })
    }

    const likeKey = `liked_art_${articleId}`
    const stored = readStorage(window.localStorage, likeKey) === 'true'
    initialLikedRef.current = stored
    serverLikedRef.current = stored
    interactionSeqRef.current = 0
    setHasLiked(stored)
  }, [articleId, safeInitialLikes])

  // 组件卸载时清理定时器与挂载标记
  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current)
      if (copyTimerRef.current) clearTimeout(copyTimerRef.current)
      if (errorTimerRef.current) clearTimeout(errorTimerRef.current)
    }
  }, [])

  const syncLikeStorage = (likeKey: string, liked: boolean) => {
    if (liked) {
      writeStorage(window.localStorage, likeKey, 'true')
    } else {
      removeStorage(window.localStorage, likeKey)
    }
  }

  const handleLike = () => {
    if (!articleId) return

    const nextState = !hasLiked
    const seq = ++interactionSeqRef.current
    setHasLiked(nextState)
    setLikes(computeDisplayLikes(nextState))
    setLikeError(null)

    const likeKey = `liked_art_${articleId}`
    syncLikeStorage(likeKey, nextState)

    // 短视频同款防抖合并：清除前一次未触发的请求
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current)
    }

    // 500ms 内用户若再次点击（如误触立刻取消，或连续狂点），在本地相抵消，不发送任何无意义请求
    debounceTimerRef.current = setTimeout(() => {
      // 只有最终状态与服务端已知状态不一致时，才发送一次网络请求
      if (nextState === serverLikedRef.current) {
        return
      }

      const action = nextState ? likeArticle(articleId) : unlikeArticle(articleId)
      action
        .then(() => {
          serverLikedRef.current = nextState
        })
        .catch((err: unknown) => {
          console.error('点赞/取消点赞同步失败', err)
          if (!mountedRef.current) return

          // 仅当本次请求之后没有更新的用户交互时执行回滚，避免覆盖最新操作
          if (interactionSeqRef.current === seq) {
            const rollbackState = serverLikedRef.current
            setHasLiked(rollbackState)
            setLikes(computeDisplayLikes(rollbackState))
            syncLikeStorage(likeKey, rollbackState)
            setLikeError('操作未同步，请稍后重试')
            if (errorTimerRef.current) clearTimeout(errorTimerRef.current)
            errorTimerRef.current = setTimeout(() => {
              if (mountedRef.current) setLikeError(null)
            }, 3000)
          }
        })
    }, 500)
  }

  const handleCopy = async () => {
    if (typeof window === 'undefined' || !navigator.clipboard?.writeText) {
      return
    }
    const shareText = title ? `${title} · ${window.location.href}` : window.location.href
    try {
      await navigator.clipboard.writeText(shareText)
      if (!mountedRef.current) return
      setCopied(true)
      if (copyTimerRef.current) clearTimeout(copyTimerRef.current)
      copyTimerRef.current = setTimeout(() => {
        if (mountedRef.current) setCopied(false)
      }, 2000)
    } catch (err) {
      console.error('复制链接失败', err)
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

        {likeError && (
          <span role="status" className="text-xs text-rose-500 font-medium">
            {likeError}
          </span>
        )}
      </div>
    </div>
  )
}
