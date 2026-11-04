'use client'

import { useEffect } from 'react'
import Link from 'next/link'
import { AlertTriangle, RotateCcw, Home } from 'lucide-react'

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  useEffect(() => {
    console.error('[Error Boundary]', error)
  }, [error])

  return (
    <div className="min-h-[60vh] flex flex-col items-center justify-center gap-6 px-4">
      <div className="p-4 rounded-full bg-rose-50 dark:bg-rose-950/30">
        <AlertTriangle className="w-10 h-10 text-rose-500" />
      </div>

      <div className="text-center max-w-md">
        <h2 className="text-xl font-extrabold text-slate-800 dark:text-slate-100 mb-2">
          系统遇到了一点小风波
        </h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          页面加载出现异常，可能是网络波动或服务端临时不可用。请稍后重试。
        </p>
        {error.digest && (
          <p className="mt-2 text-[11px] font-mono text-slate-400 dark:text-slate-500">
            错误码: {error.digest}
          </p>
        )}
      </div>

      <div className="flex items-center gap-3">
        <button
          onClick={reset}
          className="px-5 py-2.5 bg-theme-primary hover:bg-theme-primary-hover text-white text-xs font-bold rounded-xl shadow-xs border border-white/20 transition-all active:scale-95 flex items-center gap-1.5 cursor-pointer"
        >
          <RotateCcw className="w-4 h-4" />
          <span>重新加载</span>
        </button>
        <Link
          href="/"
          className="px-5 py-2.5 bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 text-xs font-bold rounded-xl border border-theme-border/60 transition-all flex items-center gap-1.5"
        >
          <Home className="w-4 h-4" />
          <span>返回首页</span>
        </Link>
      </div>
    </div>
  )
}
