'use client'

import React, { useEffect, useState } from 'react'
import Image from 'next/image'
import { siteConfig } from '@/config/site'

/**
 * 回到顶部按钮（波风水门 · 飞雷神瞬身二段术彩蛋）
 * 1. 悬停浮动：三叉苦无浮空微风摇曳，悬停浮现“飞雷神✦”金色符文；
 * 2. 点击瞬身：全屏金色破晓闪光（黄色闪光），苦无带着笔直的金光拖尾射线破空直冲云霄；
 * 3. 着陆归位：到达顶部后苦无自上方轻盈下落弹簧插回底座，随后优雅淡出。
 */
export default function BackToTop() {
  const [visible, setVisible] = useState(false)
  const [isTeleporting, setIsTeleporting] = useState(false)
  const [isLanding, setIsLanding] = useState(false)

  useEffect(() => {
    const handleScroll = () => {
      // 瞬身进行期间锁定可见性，绝不在半空中销毁
      if (isTeleporting || isLanding) return
      setVisible(window.scrollY > 200)
    }

    window.addEventListener('scroll', handleScroll, { passive: true })
    handleScroll()
    return () => window.removeEventListener('scroll', handleScroll)
  }, [isTeleporting, isLanding])

  const handleTeleportTop = () => {
    if (isTeleporting || isLanding) return
    setIsTeleporting(true)

    // 触发平滑回顶
    window.scrollTo({
      top: 0,
      behavior: 'smooth',
    })

    // 420ms 苦无冲上云霄后，进入上方降落着陆阶段
    setTimeout(() => {
      setIsTeleporting(false)
      setIsLanding(true)

      // 600ms 着陆弹簧回弹结束，页面已在顶部，随后平滑淡出
      setTimeout(() => {
        setIsLanding(false)
        if (window.scrollY <= 200) {
          setVisible(false)
        }
      }, 700)
    }, 450)
  }

  // 必须在非可见且不在动画中时才卸载
  if (!visible && !isTeleporting && !isLanding) return null

  return (
    <>
      <style>{`
        /* 1. 四代目 · 全屏黄色闪光瞬身微闪 */
        @keyframes yellowFlashScreen {
          0% {
            opacity: 0;
          }
          20% {
            opacity: 0.35;
          }
          100% {
            opacity: 0;
          }
        }

        /* 2. 底座炸裂的查克拉瞬身能量环 */
        @keyframes hiraishinBurstRing {
          0% {
            transform: scale(0.6);
            opacity: 1;
            filter: drop-shadow(0 0 8px #f59e0b);
          }
          50% {
            transform: scale(2.6);
            opacity: 0.9;
            filter: drop-shadow(0 0 24px #fbbf24);
          }
          100% {
            transform: scale(4.2);
            opacity: 0;
          }
        }

        /* 3. 苦无垂直破空向上极速冲刺 (直上九天) */
        @keyframes kunaiAscendRocket {
          0% {
            transform: translateY(0) scale(1);
            opacity: 1;
          }
          20% {
            transform: translateY(6px) scale(0.92);
            opacity: 1;
          }
          100% {
            transform: translateY(-160px) scale(0.3);
            opacity: 0;
          }
        }

        /* 4. 苦无尾部直冲向上的金色光芒射线拖尾 */
        @keyframes trailBeamShoot {
          0% {
            height: 0px;
            opacity: 0;
            transform: translateY(0);
          }
          30% {
            height: 120px;
            opacity: 1;
          }
          100% {
            height: 200px;
            opacity: 0;
            transform: translateY(-120px);
          }
        }

        /* 5. 苦无自上方轻盈下落着陆回弹 */
        @keyframes kunaiDropBounce {
          0% {
            transform: translateY(-60px) scale(0.5);
            opacity: 0;
          }
          65% {
            transform: translateY(4px) scale(1.12);
            opacity: 1;
          }
          85% {
            transform: translateY(-2px) scale(0.96);
          }
          100% {
            transform: translateY(0) scale(1);
            opacity: 1;
          }
        }

        /* 6. 平时悬停微风物理浮动 */
        @keyframes kunaiHoverFloat {
          0%, 100% {
            transform: translateY(0) rotate(0deg);
          }
          50% {
            transform: translateY(-4px) rotate(-4deg);
          }
        }
      `}</style>

      {/* 点击瞬身时触发的“黄色闪光”全屏微光感知 */}
      {isTeleporting && (
        <div
          className="fixed inset-0 z-50 pointer-events-none"
          style={{
            background:
              'radial-gradient(circle at bottom right, rgba(254, 240, 138, 0.5) 0%, rgba(245, 158, 11, 0.25) 45%, transparent 70%)',
            animation: 'yellowFlashScreen 0.45s ease-out forwards',
          }}
        />
      )}

      <div className="fixed bottom-6 right-6 z-40 pointer-events-auto select-none">
        {/* 苦无向上冲刺时的金色垂直射线拖尾 */}
        {isTeleporting && (
          <div
            className="absolute left-1/2 -translate-x-1/2 bottom-12 w-1.5 rounded-full pointer-events-none"
            style={{
              background: 'linear-gradient(to top, rgba(245, 158, 11, 0.95), rgba(254, 240, 138, 0.8), transparent)',
              boxShadow: '0 0 16px #f59e0b, 0 0 30px #facc15',
              animation: 'trailBeamShoot 0.45s cubic-bezier(0.18, 0.89, 0.32, 1.25) forwards',
            }}
          />
        )}

        {/* 按钮底座向外炸裂的飞雷神金光冲击波 */}
        {isTeleporting && (
          <div
            className="absolute inset-0 rounded-2xl pointer-events-none"
            style={{
              background:
                'radial-gradient(circle, rgba(254, 240, 138, 0.95) 0%, rgba(245, 158, 11, 0.85) 40%, rgba(234, 88, 12, 0.4) 75%, transparent 100%)',
              animation: 'hiraishinBurstRing 0.48s cubic-bezier(0.16, 1, 0.3, 1) forwards',
            }}
          />
        )}

        <button
          onClick={handleTeleportTop}
          type="button"
          aria-label="回到顶部"
          title={siteConfig.backToTop.title}
          className={`group relative w-12 h-12 rounded-2xl bg-theme-surface/95 hover:bg-theme-surface text-slate-600 dark:text-slate-300 border border-theme-border/70 hover:border-amber-400 shadow-paper hover:shadow-paper-hover transition-all duration-300 active:scale-90 flex items-center justify-center backdrop-blur-md cursor-pointer animate-in fade-in zoom-in-90 hover:shadow-[0_0_20px_rgba(245,158,11,0.4)] ${
            isTeleporting
              ? 'border-amber-400 shadow-[0_0_35px_rgba(245,158,11,0.95)] ring-2 ring-amber-400 scale-105'
              : ''
          }`}
        >
          {/* 四代目特制飞雷神三叉苦无 */}
          <div
            className="relative w-7 h-7"
            style={{
              animation: isTeleporting
                ? 'kunaiAscendRocket 0.45s cubic-bezier(0.18, 0.89, 0.32, 1.25) forwards'
                : isLanding
                ? 'kunaiDropBounce 0.48s cubic-bezier(0.18, 0.89, 0.32, 1.25) forwards'
                : 'none',
            }}
          >
            <div className="relative w-full h-full group-hover:[animation:kunaiHoverFloat_1.8s_ease-in-out_infinite]">
              <Image
                src={siteConfig.backToTop.icon}
                alt={siteConfig.backToTop.iconAlt}
                fill
                priority
                className="object-contain drop-shadow-sm transition-transform duration-300 group-hover:scale-110"
              />
            </div>
          </div>

          {/* 悬停微型符文提示 */}
          <span className="absolute -top-7 left-1/2 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap text-[10px] font-mono font-bold px-1.5 py-0.5 rounded bg-theme-surface/95 border border-theme-border/70 text-theme-primary shadow-xs">
            {siteConfig.backToTop.runeText}
          </span>
        </button>
      </div>
    </>
  )
}
