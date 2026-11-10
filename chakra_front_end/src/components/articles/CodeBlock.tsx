'use client'

import React, { useState, useMemo } from 'react'
import { Check, Copy } from 'lucide-react'
import Prism from 'prismjs'
import { siteConfig } from '@/config/site'

// 禁用 Prism 自动在客户端扫描并修改 DOM 属性（避免导致 React Hydration 不匹配）
Prism.manual = true

// 导入常用语言定义
import 'prismjs/components/prism-csharp'
import 'prismjs/components/prism-python'
import 'prismjs/components/prism-typescript'
import 'prismjs/components/prism-javascript'
import 'prismjs/components/prism-bash'
import 'prismjs/components/prism-json'
import 'prismjs/components/prism-yaml'
import 'prismjs/components/prism-markdown'
import 'prismjs/components/prism-sql'
import 'prismjs/components/prism-docker'
import 'prismjs/components/prism-diff'

/**
 * 宇智波一族 · 官方正统三勾玉写轮眼（Three Tomoe Sharingan）
 * 基于官方原版矢量：血瞳径向渐变虹膜 + 正统巴纹勾玉 + 角膜高光 + 瞳孔轨道环
 */
function SharinganEyeIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 300 300" fill="none" className={className} aria-hidden="true">
      <defs>
        {/* 血瞳径向多级渐变 */}
        <radialGradient id="codeblockIrisGrad" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#4a0404" />
          <stop offset="35%" stopColor="#b91c1c" />
          <stop offset="70%" stopColor="#dc2626" />
          <stop offset="100%" stopColor="#7f1d1d" />
        </radialGradient>

        {/* 官方正统巴纹勾玉原型 */}
        <g id="codeblockTomoe">
          <circle cx="60" cy="150" r="20" fill="#000000" />
          <path
            d="M 60,170 59.4,152.9 C 43.9,152.9 28.7,154.1 18.8,133.3 22.4,156 32.8,170 60,170 z"
            fill="#000000"
          />
        </g>
      </defs>

      {/* 1. 虹膜眼球底盘与外眶 */}
      <circle
        cx="150"
        cy="150"
        r="144"
        fill="url(#codeblockIrisGrad)"
        stroke="#09090b"
        strokeWidth="12"
      />

      {/* 2. 勾玉同心细轨道环 */}
      <circle
        cx="150"
        cy="150"
        r="90"
        fill="none"
        stroke="#000000"
        strokeWidth="5"
        strokeOpacity="0.4"
      />

      {/* 3. 严格 120° 等距三勾玉 */}
      <use href="#codeblockTomoe" />
      <use href="#codeblockTomoe" transform="rotate(120 150 150)" />
      <use href="#codeblockTomoe" transform="rotate(240 150 150)" />

      {/* 4. 中央深黑瞳孔 */}
      <circle cx="150" cy="150" r="26" fill="#000000" />

      {/* 5. 角膜月牙高光透视 */}
      <path
        d="M 80 50 A 110 110 0 0 1 220 50 A 130 130 0 0 0 80 50 Z"
        fill="#ffffff"
        fillOpacity="0.35"
      />
    </svg>
  )
}

interface CodeBlockProps {
  language?: string
  code: string
}

const LANGUAGE_LABELS: Record<string, string> = {
  csharp: 'C# / .NET',
  cs: 'C# / .NET',
  dotnet: '.NET',
  python: 'Python',
  py: 'Python',
  typescript: 'TypeScript',
  ts: 'TypeScript',
  tsx: 'TSX',
  javascript: 'JavaScript',
  js: 'JavaScript',
  jsx: 'JSX',
  bash: 'Bash / Shell',
  sh: 'Bash / Shell',
  shell: 'Shell',
  json: 'JSON',
  yaml: 'YAML',
  yml: 'YAML',
  sql: 'SQL',
  docker: 'Dockerfile',
  dockerfile: 'Dockerfile',
  diff: 'Diff',
}

export default function CodeBlock({ language = 'text', code }: CodeBlockProps) {
  const [copied, setCopied] = useState(false)

  // 规范化语言别名
  const normalizedLang = language.toLowerCase().trim()
  const displayLabel = LANGUAGE_LABELS[normalizedLang] || normalizedLang.toUpperCase() || 'CODE'

  const highlightedHtml = useMemo(() => {
    const rawCode = code.trim()
    try {
      const grammar = Prism.languages[normalizedLang] || Prism.languages.javascript
      if (grammar) {
        return Prism.highlight(rawCode, grammar, normalizedLang)
      }
    } catch {
      // 降级回退
    }
    // 默认 HTML 转义
    return rawCode
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
  }, [code, normalizedLang])

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(code)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // 降级处理
    }
  }

  return (
    <div className="my-6 rounded-2xl overflow-hidden border border-slate-800/90 shadow-xl bg-[#0b1120] text-slate-100 font-mono text-xs">
      {/* 顶部代码框标题栏 */}
      <div className="flex items-center justify-between px-4 py-2.5 bg-[#0f172a] border-b border-slate-800 select-none">
        {/* 左侧：macOS 风格三色圆点 + 语言标签 */}
        <div className="flex items-center gap-2.5">
          <div className="flex items-center gap-1.5">
            <span className="w-2.5 h-2.5 rounded-full bg-rose-500/80 inline-block" />
            <span className="w-2.5 h-2.5 rounded-full bg-amber-500/80 inline-block" />
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-500/80 inline-block" />
          </div>
          <span className="text-[11px] font-bold text-theme-primary uppercase tracking-wider pl-1">
            {displayLabel}
          </span>
        </div>

        {/* 右侧：复制代码按钮 */}
        <button
          onClick={handleCopy}
          type="button"
          title="复制代码"
          aria-label="复制代码到剪贴板"
          className="flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-semibold text-slate-400 hover:text-white hover:bg-slate-800 transition-all active:scale-95 cursor-pointer group"
        >
          {copied ? (
            <>
              {/* 仅在 anime 预设下展示写轮眼微动效彩蛋 */}
              {siteConfig.themePreset === 'anime' && (
                <SharinganEyeIcon className="w-3.5 h-3.5 animate-[spin_0.8s_ease-out] drop-shadow-[0_0_6px_rgba(239,68,68,0.8)]" />
              )}
              <Check className="w-3.5 h-3.5 text-emerald-400" />
              <span className="text-emerald-400 font-bold">已复制!</span>
            </>
          ) : (
            <>
              <Copy className="w-3.5 h-3.5 text-slate-400 group-hover:text-slate-200 transition-colors" />
              <span>复制代码</span>
            </>
          )}
        </button>
      </div>

      {/* 代码内容区域 */}
      <div className="p-4 overflow-x-auto">
        <pre
          suppressHydrationWarning
          className="!bg-transparent !p-0 !m-0 !border-0 !shadow-none"
        >
          <code
            suppressHydrationWarning
            className={`language-${normalizedLang}`}
            dangerouslySetInnerHTML={{ __html: highlightedHtml }}
          />
        </pre>
      </div>
    </div>
  )
}
