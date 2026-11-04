'use client'

import React, { useState, useEffect, useRef, memo } from 'react'
import { Terminal as TerminalIcon, Sparkles } from 'lucide-react'
import { useTheme } from 'next-themes'
import { siteConfig } from '@/config/site'

interface CommandOutput {
  id: string
  command?: string
  text?: string | React.ReactNode
  isError?: boolean
}

function InteractiveTerminalWidget() {
  const { resolvedTheme, setTheme } = useTheme()
  const [history, setHistory] = useState<CommandOutput[]>([])
  const [inputVal, setInputVal] = useState('')
  const [cmdHistory, setCmdHistory] = useState<string[]>([])
  const [historyIdx, setHistoryIdx] = useState<number>(-1)
  const [isTypingInit, setIsTypingInit] = useState(true)

  const terminalBodyRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  // 初始自动打字机效果模拟极客系统引导
  useEffect(() => {
    const initLines: { cmd: string; output: string[] }[] = [
      {
        cmd: 'chakra --status',
        output: [
          '[✓] Engine  : Chakra v1.0.0 (Dual-Mode Architecture)',
          '[✓] Runtime : .NET 10 WebAPI + Next.js 15 App Router',
          '[✓] Storage : Local Persistent Storage & S3 Dual-Mode',
          `[✓] Status  : Online (${siteConfig.workshopName})`,
        ],
      },
      {
        cmd: 'chakra --whoami',
        output: [
          `> ${siteConfig.name} (${siteConfig.authorSubtitle} · ${siteConfig.authorIntro})`,
        ],
      },
    ]

    let step = 0
    const timer = setInterval(() => {
      if (step < initLines.length) {
        const item = initLines[step]
        setHistory((prev) => [
          ...prev,
          { id: `init-cmd-${step}`, command: item.cmd },
          ...item.output.map((line, lIdx) => ({
            id: `init-out-${step}-${lIdx}`,
            text: line,
          })),
        ])
        step++
      } else {
        setIsTypingInit(false)
        clearInterval(timer)
      }
    }, 450)

    return () => clearInterval(timer)
  }, [])

  // 保持终端内容自动滚动到底部
  useEffect(() => {
    if (terminalBodyRef.current) {
      terminalBodyRef.current.scrollTop = terminalBodyRef.current.scrollHeight
    }
  }, [history, isTypingInit])

  const handleCommand = (rawCmd: string) => {
    const cmd = rawCmd.trim()
    if (!cmd) return

    setCmdHistory((prev) => [...prev, cmd])
    setHistoryIdx(-1)

    const lower = cmd.toLowerCase()
    let responseNode: React.ReactNode = null
    let isError = false

    switch (lower) {
      case 'help':
        responseNode = (
          <div className="space-y-0.5 text-slate-300">
            <div>可用指令列表 (Available Commands):</div>
            <div>  <span className="text-cyan-400 font-bold">status</span>   - 查看后端系统运行与架构集群状态</div>
            <div>  <span className="text-cyan-400 font-bold">whoami</span>   - 查看博主信息与核心座右铭</div>
            <div>  <span className="text-cyan-400 font-bold">skills</span>   - 打印核心技术栈矩阵</div>
            <div>  <span className="text-cyan-400 font-bold">contact</span>  - 获取 GitHub 与联系邮箱</div>
            <div>  <span className="text-cyan-400 font-bold">theme</span>    - 一键切换白天 / 夜间主题</div>
            <div>  <span className="text-cyan-400 font-bold">clear</span>    - 清空终端屏幕输出</div>
          </div>
        )
        break

      case 'status':
        responseNode = (
          <div className="text-emerald-400 space-y-0.5">
            <div>[✓] Engine    : Chakra Blog Engine v1.0.0</div>
            <div>[✓] WebAPI    : .NET 10 (Vertical Slice CQRS + Dapper + MediatR)</div>
            <div>[✓] Frontend  : Next.js 15 App Router (SSR 骨架流式直出)</div>
            <div>[✓] Database  : PostgreSQL 16 + (可选 Redis 弹性降级)</div>
            <div>[✓] Storage   : 本地磁盘持久卷 / S3 兼容对象存储双模</div>
            <div>[✓] Deploy    : Docker Compose 全自动容器化拉起</div>
          </div>
        )
        break

      case 'whoami':
        responseNode = (
          <div className="text-amber-300 space-y-0.5">
            <div>博主代号: {siteConfig.name} [{siteConfig.authorSubtitle}]</div>
            <div>核心专长: {siteConfig.authorIntro}</div>
            <div>日常箴言: {siteConfig.wisdom}</div>
          </div>
        )
        break

      case 'skills':
        responseNode = (
          <div className="text-sky-300 space-y-0.5">
            <div>技术栈矩阵 (Tech Stack Matrix):</div>
            <div>  • 后端: C# / .NET 10 / ASP.NET Core / Dapper / EF Core</div>
            <div>  • 前端: Next.js 15 / React 19 / TypeScript / Tailwind CSS</div>
            <div>  • 基础设施: Docker Compose / Linux / PostgreSQL / Redis / S3</div>
            <div>  • AI探索: LLM Agent / RAG / 垂直切片自动化</div>
          </div>
        )
        break

      case 'contact':
        responseNode = (
          <div className="text-purple-300 space-y-0.5">
            <div>GitHub: {siteConfig.socials.github}</div>
            <div>Email : {siteConfig.socials.email}</div>
          </div>
        )
        break

      case 'theme':
        const next = resolvedTheme === 'dark' ? 'light' : 'dark'
        setTheme(next)
        responseNode = (
          <span className="text-cyan-300">
            [✓] 主题模式已切换为: {next === 'dark' ? '深色夜间' : '浅色白天'}
          </span>
        )
        break

      case 'clear':
        setHistory([])
        setInputVal('')
        return

      default:
        isError = true
        responseNode = (
          <span className="text-rose-400">
            zsh: command not found: {cmd}. 输入 <span className="underline font-bold text-white">help</span> 查看可用指令。
          </span>
        )
        break
    }

    setHistory((prev) => [
      ...prev,
      { id: `cmd-${Date.now()}`, command: cmd },
      { id: `res-${Date.now()}`, text: responseNode, isError },
    ])
    setInputVal('')
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleCommand(inputVal)
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      if (cmdHistory.length === 0) return
      const nextIdx = historyIdx === -1 ? cmdHistory.length - 1 : Math.max(0, historyIdx - 1)
      setHistoryIdx(nextIdx)
      setInputVal(cmdHistory[nextIdx])
    } else if (e.key === 'ArrowDown') {
      e.preventDefault()
      if (historyIdx === -1) return
      const nextIdx = historyIdx + 1
      if (nextIdx < cmdHistory.length) {
        setHistoryIdx(nextIdx)
        setInputVal(cmdHistory[nextIdx])
      } else {
        setHistoryIdx(-1)
        setInputVal('')
      }
    }
  }

  return (
    <div
      onClick={() => inputRef.current?.focus()}
      className="rounded-2xl bg-[#090d16]/95 backdrop-blur-md border border-slate-800 shadow-xl overflow-hidden font-mono text-xs flex flex-col cursor-text select-text transition-all hover:border-slate-700 hover:shadow-2xl"
    >
      {/* 顶部极客标题栏 */}
      <div className="flex items-center justify-between px-3.5 py-2 bg-[#0d1322] border-b border-slate-800/90 select-none">
        <div className="flex items-center gap-2">
          {/* macOS 风格三色圆点 */}
          <div className="flex items-center gap-1.5">
            <span className="w-2.5 h-2.5 rounded-full bg-rose-500/80 inline-block" />
            <span className="w-2.5 h-2.5 rounded-full bg-amber-500/80 inline-block" />
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-500/80 inline-block" />
          </div>
          <span className="text-[11px] text-slate-400 font-semibold flex items-center gap-1 pl-1">
            <TerminalIcon className="w-3 h-3 text-cyan-400" />
            <span>chakra@terminal: ~</span>
          </span>
        </div>

        <div className="flex items-center gap-2 text-[10px] text-slate-500 font-mono">
          <span className="flex items-center gap-1 text-emerald-400">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
            <span>bash 5.2</span>
          </span>
        </div>
      </div>

      {/* 终端内容区与命令行输入 */}
      <div
        ref={terminalBodyRef}
        className="p-3.5 max-h-[175px] min-h-[145px] overflow-y-auto space-y-1.5 leading-relaxed text-slate-200 scrollbar-thin scrollbar-thumb-slate-800"
      >
        {history.map((item) => (
          <div key={item.id} className="break-all">
            {item.command && (
              <div className="flex items-center gap-1.5 text-slate-400">
                <span className="text-cyan-400 font-bold">$</span>
                <span className="text-white font-bold">{item.command}</span>
              </div>
            )}
            {item.text && (
              <div className={item.isError ? 'text-rose-400 pl-3' : 'pl-3'}>
                {item.text}
              </div>
            )}
          </div>
        ))}

        {/* 交互输入行 */}
        <div className="flex items-center gap-1.5 pt-0.5">
          <span className="text-cyan-400 font-bold select-none">$</span>
          <input
            ref={inputRef}
            type="text"
            value={inputVal}
            onChange={(e) => setInputVal(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={isTypingInit ? '初始化中...' : '输入 help 查看指令...'}
            className="flex-1 bg-transparent text-white font-mono text-xs focus:outline-none placeholder:text-slate-600 caret-cyan-400"
            autoComplete="off"
            spellCheck={false}
          />
        </div>
      </div>

      {/* 底部微提示栏 */}
      <div className="px-3.5 py-1.5 bg-[#0b101c]/80 border-t border-slate-800/60 flex items-center justify-between text-[10px] text-slate-500 select-none">
        <span className="flex items-center gap-1">
          <Sparkles className="w-3 h-3 text-cyan-400" />
          <span>支持实时键入 · 试着输入 help 或 theme</span>
        </span>
        <span className="font-mono text-slate-600">UTF-8</span>
      </div>
    </div>
  )
}

export default memo(InteractiveTerminalWidget)
