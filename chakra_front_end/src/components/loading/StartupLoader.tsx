'use client'

import React, { useState, useEffect } from 'react'
import Image from 'next/image'
import styles from './StartupLoader.module.css'
import { siteConfig } from '@/config/site'

/**
 * 开屏启动动效组件 (StartupLoader)
 * 采用原生 CSS 关键帧驱动，动画结束后从 DOM 完全卸载
 */
export default function StartupLoader() {
  const [mounted, setMounted] = useState(false)
  const [shouldShow, setShouldShow] = useState(true)

  useEffect(() => {
    setMounted(true)
    // 检查本会话是否已经展示过启动动画
    const hasShown = sessionStorage.getItem('ba_startup_shown')
    if (hasShown) {
      setShouldShow(false)
      return
    }

    sessionStorage.setItem('ba_startup_shown', 'true')
    // 自动在 850ms 动画结束后从 DOM 完全卸载
    const timer = setTimeout(() => {
      setShouldShow(false)
    }, 900)

    return () => clearTimeout(timer)
  }, [])

  // 如果会话中已展示过，或者动画已播放完毕，直接卸载
  if (mounted && !shouldShow) return null

  const isAnime = siteConfig.themePreset === 'anime'
  const logoSrc = isAnime
    ? (siteConfig.startupLoader.icon || siteConfig.avatar)
    : (siteConfig.avatar || '/favicon.svg')

  return (
    <div
      className={`${styles.startup} ${styles.startupAutoFade}`}
      aria-hidden="true"
    >
      {/* 启动微标 */}
      <div className={styles.avatarContainer}>
        <Image
          src={logoSrc}
          alt={siteConfig.name || 'Site Logo'}
          fill
          priority
          unoptimized={logoSrc.startsWith('http')}
          className={styles.avatarImg}
        />
      </div>

      {/* 连接提示文本 */}
      <div className={styles.connecting}>
        {isAnime ? siteConfig.startupLoader.title : 'LOADING...'}
      </div>

      {/* 倾斜进度条 (原生 CSS 驱动) */}
      <div className={styles.progress}>
        <span className={styles.progressBarAuto} />
      </div>

      {/* 副标题 */}
      <div className={styles.percentAuto}>
        {siteConfig.startupLoader.subtitle}
      </div>
    </div>
  )
}
