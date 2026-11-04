'use client'

import React, { useEffect, useRef, useState, useCallback, memo } from 'react'
import { Trophy, Compass } from 'lucide-react'
import { useTheme } from 'next-themes'
import { siteConfig } from '@/config/site'

// 贡献图网格参数
const COLS = 20
const ROWS = 7

interface Point {
  x: number
  y: number
}

// 确定性的贡献热力图基底数据
const CONTRIBUTION_MATRIX: number[][] = [
  [0, 1, 2, 0, 1, 0, 2, 3, 1, 0, 1, 2, 0, 1, 0, 2, 1, 0, 2, 1],
  [1, 0, 1, 2, 0, 1, 0, 1, 2, 1, 0, 1, 2, 0, 1, 0, 2, 1, 0, 0],
  [2, 1, 0, 1, 3, 1, 2, 0, 1, 2, 3, 1, 0, 2, 1, 3, 1, 2, 1, 0],
  [0, 2, 1, 0, 1, 2, 1, 0, 2, 1, 0, 2, 1, 0, 1, 2, 0, 1, 2, 1],
  [1, 0, 2, 3, 0, 1, 0, 2, 1, 0, 1, 0, 2, 3, 0, 1, 2, 0, 1, 2],
  [0, 1, 0, 1, 2, 0, 1, 0, 2, 1, 0, 1, 0, 1, 2, 0, 1, 2, 0, 1],
  [1, 2, 1, 0, 1, 2, 0, 1, 0, 2, 1, 2, 1, 0, 1, 2, 0, 1, 0, 0],
]

function SnakeGameWidget() {
  const { resolvedTheme } = useTheme()
  const [mounted, setMounted] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const [, setRenderTick] = useState(0)
  const [isInView, setIsInView] = useState(true)
  const [isPageVisible, setIsPageVisible] = useState(true)

  useEffect(() => {
    setMounted(true)
  }, [])

  const isDark = mounted && resolvedTheme === 'dark'

  const snakeRef = useRef<Point[]>([
    { x: 5, y: 3 },
    { x: 4, y: 3 },
    { x: 3, y: 3 },
  ])
  const foodRef = useRef<Point>({ x: 12, y: 3 })
  const directionRef = useRef<'UP' | 'DOWN' | 'LEFT' | 'RIGHT'>('RIGHT')
  const scoreRef = useRef(16)

  // 1. 监听浏览器标签页前后台切换
  useEffect(() => {
    const handleVisibilityChange = () => {
      setIsPageVisible(document.visibilityState === 'visible')
    }
    document.addEventListener('visibilitychange', handleVisibilityChange)
    return () => document.removeEventListener('visibilitychange', handleVisibilityChange)
  }, [])

  // 2. 监听视口交叉状态（离开 Hero 视口后自动暂停，节省能耗）
  useEffect(() => {
    const el = containerRef.current
    if (!el || typeof IntersectionObserver === 'undefined') return

    const observer = new IntersectionObserver(
      ([entry]) => {
        setIsInView(entry.isIntersecting)
      },
      { threshold: 0.05 }
    )

    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  // 自动巡航算法
  const getNextAutoDirection = useCallback(
    (head: Point, target: Point, currentDir: string): 'UP' | 'DOWN' | 'LEFT' | 'RIGHT' => {
      const candidates: ('UP' | 'DOWN' | 'LEFT' | 'RIGHT')[] = []
      if (target.x > head.x && currentDir !== 'LEFT') candidates.push('RIGHT')
      if (target.x < head.x && currentDir !== 'RIGHT') candidates.push('LEFT')
      if (target.y > head.y && currentDir !== 'UP') candidates.push('DOWN')
      if (target.y < head.y && currentDir !== 'DOWN') candidates.push('UP')

      if (candidates.length > 0) {
        return candidates[Math.floor(Math.random() * candidates.length)]
      }
      const allDirs: ('UP' | 'DOWN' | 'LEFT' | 'RIGHT')[] = ['UP', 'DOWN', 'LEFT', 'RIGHT']
      return allDirs[Math.floor(Math.random() * allDirs.length)]
    },
    []
  )

  // 贪吃蛇运动循环
  useEffect(() => {
    if (!isInView || !isPageVisible) return

    const interval = setInterval(() => {
      const currentSnake = snakeRef.current
      const currentFood = foodRef.current
      const currentDir = directionRef.current

      const nextDir = getNextAutoDirection(currentSnake[0], currentFood, currentDir)
      directionRef.current = nextDir

      let newHeadX = currentSnake[0].x
      let newHeadY = currentSnake[0].y

      if (nextDir === 'RIGHT') newHeadX = (newHeadX + 1) % COLS
      if (nextDir === 'LEFT') newHeadX = (newHeadX - 1 + COLS) % COLS
      if (nextDir === 'DOWN') newHeadY = (newHeadY + 1) % ROWS
      if (nextDir === 'UP') newHeadY = (newHeadY - 1 + ROWS) % ROWS

      const newHead: Point = { x: newHeadX, y: newHeadY }
      const newSnake = [newHead, ...currentSnake]

      // 吃到食物
      if (newHead.x === currentFood.x && newHead.y === currentFood.y) {
        scoreRef.current += 1
        foodRef.current = {
          x: Math.floor(Math.random() * COLS),
          y: Math.floor(Math.random() * ROWS),
        }
      } else {
        newSnake.pop()
      }

      snakeRef.current = newSnake
      setRenderTick((t) => t + 1)
    }, 160)

    return () => clearInterval(interval)
  }, [isInView, isPageVisible, getNextAutoDirection])

  const snake = snakeRef.current
  const food = foodRef.current
  const score = scoreRef.current

  // 渲染单格颜色
  const getCellClassName = (r: number, c: number) => {
    const isHead = snake[0].x === c && snake[0].y === r
    const isBody = snake.slice(1).some((pt) => pt.x === c && pt.y === r)

    if (isHead) {
      return 'bg-theme-primary ring-2 ring-theme-primary/50 scale-110 shadow-xs z-10'
    }
    if (isBody) {
      return 'bg-theme-secondary/80 scale-95 transition-all duration-100'
    }

    const level = CONTRIBUTION_MATRIX[r][c]
    if (level === 3) return 'bg-theme-primary/60'
    if (level === 2) return 'bg-theme-primary/40'
    if (level === 1) return 'bg-theme-primary/20'
    return 'bg-theme-surface/70 border border-theme-border/30'
  }

  const gameTitle = siteConfig.hero.gameTitle
  const gameUnit = siteConfig.hero.gameUnit
  const mascotIcon = isDark ? '🐍' : '🐸'
  const foodIcon = isDark ? '⚡' : '🍃'
  const gameStatus = isDark ? '夜间巡航' : '日间漫游'

  return (
    <div
      ref={containerRef}
      className="relative rounded-2xl bg-theme-surface/85 backdrop-blur-md border border-theme-border/60 shadow-paper p-4 overflow-hidden transition-all hover:shadow-paper-hover"
    >
      {/* 顶部微型状态栏 */}
      <div className="flex items-center justify-between gap-2 pb-2.5 mb-2.5 border-b border-theme-border/40 font-mono text-xs">
        <div className="flex items-center gap-2">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
          </span>
          <span className="text-[11px] text-slate-700 dark:text-slate-200 font-bold flex items-center gap-1.5">
            <span>{gameTitle}</span>
          </span>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-[11px] font-semibold px-2 py-0.5 rounded border flex items-center gap-1 text-theme-primary bg-theme-surface-hover border-theme-border/70">
            <Trophy className="w-3 h-3 text-theme-primary" />
            <span>{score} {gameUnit}</span>
          </span>
        </div>
      </div>

      {/* 贪吃蛇 20x7 贡献图矩阵网格 */}
      <div
        className="grid gap-[3px] p-2 bg-theme-surface-hover/60 rounded-xl border border-theme-border/50"
        style={{
          gridTemplateColumns: `repeat(${COLS}, minmax(0, 1fr))`,
        }}
      >
        {Array.from({ length: ROWS }).map((_, r) =>
          Array.from({ length: COLS }).map((_, c) => {
            const isHead = snake[0].x === c && snake[0].y === r
            const isTarget = food.x === c && food.y === r
            return (
              <div
                key={`${r}-${c}`}
                className={`relative w-full aspect-square rounded-[3px] transition-colors duration-150 flex items-center justify-center ${getCellClassName(
                  r,
                  c
                )}`}
              >
                {/* 蛇头图标 */}
                {isHead && (
                  <span className="text-[10px] select-none absolute z-30 pointer-events-none drop-shadow-xs">
                    {mascotIcon}
                  </span>
                )}
                {/* 目标食物图标 */}
                {isTarget && (
                  <span className="text-[10px] select-none absolute z-20 animate-bounce">
                    {foodIcon}
                  </span>
                )}
              </div>
            )
          })
        )}
      </div>

      {/* 底部副标签 */}
      <div className="mt-2.5 flex items-center justify-between text-[11px] font-mono text-slate-500 dark:text-slate-400">
        <span className="flex items-center gap-1">
          <Compass className="w-3 h-3 text-theme-primary" />
          <span>自动巡航漫游</span>
        </span>
        <span className="text-slate-500 dark:text-slate-400">
          状态：{gameStatus}
        </span>
      </div>
    </div>
  )
}

export default memo(SnakeGameWidget)
