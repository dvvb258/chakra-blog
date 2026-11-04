'use client'

import React, { useEffect, useRef, useState } from 'react'
import Image from 'next/image'
import { useTheme } from 'next-themes'
import styles from './naruto-visual.module.css'
import { siteConfig } from '@/config/site'
import ChakraJutsuCanvas from './ChakraJutsuCanvas'

export default function NarutoCharacter() {
  const { resolvedTheme } = useTheme()
  const [mounted, setMounted] = useState(false)
  const isDark = mounted && resolvedTheme === 'dark'
  const character = isDark ? 'sasuke' : 'naruto'
  const charConfig = isDark ? siteConfig.character.dark : siteConfig.character.light

  const sceneRef = useRef<HTMLDivElement>(null)
  const jutsuFadeTimerRef = useRef<NodeJS.Timeout | null>(null)
  const jutsuEndTimerRef = useRef<NodeJS.Timeout | null>(null)

  const [, setPulseCount] = useState(0)
  // 忍术生命周期状态：'idle' (待机) -> 'active' (爆发) -> 'fading' (坍缩消散)
  const [jutsuPhase, setJutsuPhase] = useState<'idle' | 'active' | 'fading'>('idle')
  const [isShaking, setIsShaking] = useState(false)
  const [isFlashing, setIsFlashing] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  // 主题切换时清理所有正在进行的忍术计时器
  useEffect(() => {
    setJutsuPhase('idle')
    setIsShaking(false)
    setIsFlashing(false)
    if (jutsuFadeTimerRef.current) clearTimeout(jutsuFadeTimerRef.current)
    if (jutsuEndTimerRef.current) clearTimeout(jutsuEndTimerRef.current)
  }, [resolvedTheme])

  // 鼠标移动 3D 视差追踪跟随
  const handlePointerMove = (e: React.PointerEvent<HTMLDivElement>) => {
    if (e.pointerType !== 'mouse') return
    const bounds = e.currentTarget.getBoundingClientRect()
    const x = (e.clientX - bounds.left) / bounds.width - 0.5
    const y = (e.clientY - bounds.top) / bounds.height - 0.5
    sceneRef.current?.style.setProperty('--look-x', `${x * 12}px`)
    sceneRef.current?.style.setProperty('--look-y', `${y * 8}px`)
  }

  const handlePointerLeave = () => {
    sceneRef.current?.style.setProperty('--look-x', '0px')
    sceneRef.current?.style.setProperty('--look-y', '0px')
  }

  // 点击掌心释放忍术（螺旋丸 / 千鸟）
  const handleTouch = () => {
    setPulseCount((prev) => prev + 1)
    setJutsuPhase('active')
    setIsShaking(true)

    // 暗色佐助触发雷光照亮全屏频闪
    if (isDark) {
      setIsFlashing(true)
      setTimeout(() => setIsFlashing(false), 450)
    }

    // 震动动画 450ms 结束
    setTimeout(() => {
      setIsShaking(false)
    }, 450)

    // 清理之前的计时器
    if (jutsuFadeTimerRef.current) clearTimeout(jutsuFadeTimerRef.current)
    if (jutsuEndTimerRef.current) clearTimeout(jutsuEndTimerRef.current)

    // 爆发 3.2 秒后开始向内收束消散
    jutsuFadeTimerRef.current = setTimeout(() => {
      setJutsuPhase('fading')
    }, 3200)

    // 3.75 秒后彻底复位为 idle
    jutsuEndTimerRef.current = setTimeout(() => {
      setJutsuPhase('idle')
    }, 3750)
  }

  const isJutsuVisible = jutsuPhase === 'active' || jutsuPhase === 'fading'
  const isJutsuFading = jutsuPhase === 'fading'

  const imageSrc = charConfig.src

  return (
    <div
      ref={sceneRef}
      className={styles.scene}
      data-character={character}
      onPointerMove={handlePointerMove}
      onPointerLeave={handlePointerLeave}
    >
      <div className={`${styles.stage} ${isShaking ? styles.stageShake : ''}`}>
        {/* 背景氛围天光 */}
        <div
          className={`${styles.light} ${isJutsuVisible ? styles.lightCharged : ''}`}
          aria-hidden="true"
        />

        {/* 千鸟释放瞬间的闪电照耀频闪 */}
        {isDark && isFlashing && <div className={styles.lightningFlash} aria-hidden="true" />}

        {/* 角色立绘核心层 */}
        <div className={styles.figure}>
          <Image
            unoptimized
            key={imageSrc}
            className={styles.portrait}
            src={imageSrc}
            width={1024}
            height={1024}
            alt={charConfig.name}
            priority
            draggable={false}
          />
        </div>

        {/* 掌心触碰交互区域与忍术释放位点 */}
        <button
          type="button"
          className={styles.contact}
          title={charConfig.actionPrompt}
          aria-label={charConfig.actionPrompt}
          onClick={handleTouch}
        >
          {/* 1. 实时 Canvas 查克拉引擎：待机微凝聚态 vs 点击完全释放态 */}
          <ChakraJutsuCanvas
            jutsuType={isDark ? 'chidori' : 'rasengan'}
            active={true}
            mode={isJutsuVisible ? 'full' : 'idle'}
            fading={isJutsuFading}
            width={isDark ? 360 : 250}
            height={isDark ? 360 : 250}
          />

          {/* 2. 忍术背景外溢查克拉光晕 (仅完整释放时) */}
          {!isDark && isJutsuVisible && (
            <div className={`${styles.rasenganGoldenHalo} ${isJutsuFading ? styles.jutsuFading : ''}`} />
          )}

          {/* 3. 点击瞬时查克拉涟漪 */}
          {jutsuPhase === 'active' && (
            <div className={styles.contactRipple} aria-hidden="true" />
          )}
        </button>

        {/* 底部角色状态胶囊 */}
        <div className="absolute bottom-2 right-4 z-20 flex items-center gap-2 px-3 py-1.5 rounded-full bg-theme-surface/85 backdrop-blur-md border border-theme-border/60 shadow-xs text-[11px] font-mono text-slate-600 dark:text-slate-300 pointer-events-none">
          <span className="w-2 h-2 rounded-full bg-theme-primary shadow-[0_0_8px_rgba(var(--theme-shadow-rgb),0.8)]" />
          <span className="font-bold">
            {jutsuPhase === 'idle' ? charConfig.status : charConfig.activeStatus}
          </span>
        </div>

      </div>
    </div>
  )
}
