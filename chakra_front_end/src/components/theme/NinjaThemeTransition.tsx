'use client'

import React, { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'

interface NinjaThemeTransitionProps {
  targetTheme: 'light' | 'dark'
  onComplete: () => void
}

/**
 * 昼夜全屏能量转场动画
 * - 纯正火影官方矢量校准：
 *   - 切向 dark (佐助): 官方真实三勾玉写轮眼 (Sharingan) 伴随千鸟雷光开眼
 *   - 切向 light (鸣人): 官方真实木叶隐村火之意志标志 (Konoha) 破晓流转
 */
export default function NinjaThemeTransition({
  targetTheme,
  onComplete,
}: NinjaThemeTransitionProps) {
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
    const timer = setTimeout(() => {
      onComplete()
    }, 700)
    return () => clearTimeout(timer)
  }, [onComplete])

  if (!mounted || typeof document === 'undefined') return null

  const isGoingDark = targetTheme === 'dark'

  return createPortal(
    <div
      className="fixed inset-0 z-[9999] pointer-events-none overflow-hidden flex items-center justify-center select-none"
      aria-hidden="true"
    >
      <style>{`
        @keyframes chakraWave {
          0% {
            transform: scale(0.15);
            opacity: 0.95;
          }
          45% {
            transform: scale(1.6);
            opacity: 1;
          }
          100% {
            transform: scale(4.2);
            opacity: 0;
          }
        }

        @keyframes sharinganAwaken {
          0% {
            transform: scale(0.2) rotate(-180deg);
            opacity: 0;
          }
          40% {
            transform: scale(1.15) rotate(15deg);
            opacity: 1;
          }
          70% {
            transform: scale(1.05) rotate(0deg);
            opacity: 0.95;
          }
          100% {
            transform: scale(1.4) rotate(45deg);
            opacity: 0;
          }
        }

        @keyframes konohaDawn {
          0% {
            transform: scale(0.25) rotate(-30deg);
            opacity: 0;
          }
          35% {
            transform: scale(1.12) rotate(0deg);
            opacity: 1;
          }
          70% {
            transform: scale(1.05) rotate(8deg);
            opacity: 0.9;
          }
          100% {
            transform: scale(1.35) rotate(20deg);
            opacity: 0;
          }
        }

        @keyframes lightningSlash {
          0% {
            opacity: 0;
            transform: scaleX(0.2) rotate(-25deg);
          }
          25% {
            opacity: 1;
            transform: scaleX(1.4) rotate(-25deg);
          }
          60% {
            opacity: 0.8;
          }
          100% {
            opacity: 0;
            transform: scaleX(2.2) rotate(-25deg);
          }
        }
      `}</style>

      {/* 1. 全屏光照底色冲击波 */}
      <div
        className="absolute inset-0"
        style={{
          background: isGoingDark
            ? 'radial-gradient(circle at center, rgba(168, 85, 247, 0.45) 0%, rgba(15, 23, 42, 0.85) 55%, rgba(8, 12, 22, 0.98) 100%)'
            : 'radial-gradient(circle at center, rgba(254, 240, 138, 0.6) 0%, rgba(254, 215, 170, 0.4) 45%, rgba(255, 253, 250, 0.92) 100%)',
          animation: 'chakraWave 0.68s cubic-bezier(0.16, 1, 0.3, 1) forwards',
        }}
      />

      {/* 2. 切向暗色（佐助）：千鸟撕裂暗夜的闪电光轨 */}
      {isGoingDark && (
        <div
          className="absolute w-[180vw] h-1.5 bg-gradient-to-r from-transparent via-cyan-300 to-transparent shadow-[0_0_25px_#67e8f9]"
          style={{
            animation: 'lightningSlash 0.5s ease-out forwards',
          }}
        />
      )}

      {/* 3. 中央图腾 */}
      <div className="relative z-10 flex flex-col items-center justify-center">
        {isGoingDark ? (
          /* 佐助 · 官方原版三勾玉写轮眼（真实血色径向渐变虹膜 + 标准勾玉 + 瞳光溢出） */
          <div
            className="relative flex flex-col items-center"
            style={{
              animation: 'sharinganAwaken 0.68s cubic-bezier(0.18, 0.89, 0.32, 1.25) forwards',
            }}
          >
            <svg
              width="145"
              height="145"
              viewBox="0 0 300 300"
              fill="none"
              className="drop-shadow-[0_0_35px_rgba(220,38,38,0.9)] drop-shadow-[0_0_70px_rgba(168,85,247,0.7)]"
            >
              <defs>
                {/* 官方正统写轮眼血瞳径向渐变 */}
                <radialGradient id="sharinganIrisGrad" cx="50%" cy="50%" r="50%">
                  <stop offset="0%" stopColor="#4a0404" />
                  <stop offset="35%" stopColor="#b91c1c" />
                  <stop offset="70%" stopColor="#dc2626" />
                  <stop offset="100%" stopColor="#7f1d1d" />
                </radialGradient>

                {/* 官方正统勾玉原型 */}
                <g id="officialTomoe">
                  <circle cx="60" cy="150" r="20" fill="#000000" />
                  <path
                    d="M 60,170 59.4,152.9 C 43.9,152.9 28.7,154.1 18.8,133.3 22.4,156 32.8,170 60,170 z"
                    fill="#000000"
                  />
                </g>
              </defs>

              {/* 虹膜眼球底（带外圈深黑轮廓） */}
              <circle
                cx="150"
                cy="150"
                r="144"
                fill="url(#sharinganIrisGrad)"
                stroke="#09090b"
                strokeWidth="12"
              />

              {/* 勾玉同心黑色细轨道环 */}
              <circle
                cx="150"
                cy="150"
                r="90"
                fill="none"
                stroke="#000000"
                strokeWidth="4"
                strokeOpacity="0.4"
              />

              {/* 严格 120° 三勾玉分布 */}
              <use href="#officialTomoe" />
              <use href="#officialTomoe" transform="rotate(120 150 150)" />
              <use href="#officialTomoe" transform="rotate(240 150 150)" />

              {/* 中央深黑瞳孔 */}
              <circle cx="150" cy="150" r="26" fill="#000000" />

              {/* 真实角膜反光 */}
              <path
                d="M 80 50 A 110 110 0 0 1 220 50 A 130 130 0 0 0 80 50 Z"
                fill="#ffffff"
                fillOpacity="0.25"
                filter="blur(2px)"
              />
            </svg>
          </div>
        ) : (
          /* 鸣人 · 官方原版木叶隐村火之意志标志（Konohagakure Symbol） */
          <div
            className="relative flex flex-col items-center"
            style={{
              animation: 'konohaDawn 0.68s cubic-bezier(0.18, 0.89, 0.32, 1.25) forwards',
            }}
          >
            <svg
              width="145"
              height="145"
              viewBox="0 0 100 100"
              fill="none"
              className="drop-shadow-[0_0_35px_rgba(245,158,11,0.95)] drop-shadow-[0_0_65px_rgba(234,88,12,0.5)]"
            >
              {/* 外圈仙术查克拉光环 */}
              <circle
                cx="50"
                cy="50"
                r="46"
                stroke="#f59e0b"
                strokeWidth="2.5"
                strokeDasharray="6 4"
                opacity="0.8"
              />
              <circle
                cx="50"
                cy="50"
                r="40"
                stroke="#38bdf8"
                strokeWidth="1.2"
                opacity="0.75"
              />

              {/* 核心金晨底 */}
              <circle cx="50" cy="50" r="32" fill="#fbbf24" fillOpacity="0.22" />

              {/* 官方正统木叶隐村标志 (72x72 标准坐标系映射至 100x100) */}
              <g
                transform="translate(14, 14) scale(1)"
                stroke="#f59e0b"
                strokeWidth="4.5"
                strokeLinecap="round"
                strokeLinejoin="round"
                fill="none"
              >
                {/* 顺时针木叶主旋涡与上扬外甩尾 */}
                <path d="M 39.58 41.06 C 40.4 43.1 38.5 45.4 36.3 46.16 C 33.8 45.9 31.5 44.5 30.2 41.9 C 29.6 38.6 31 34.5 33.5 32.5 C 37.5 31.1 41.8 31.6 44.7 33 C 48 36.7 49.5 41.6 49.2 45.3 C 48.7 50.4 44 54.3 39 55.6 C 33 55.8 26.5 53.3 22.7 49 C 20.2 43.9 19.8 37.3 22.5 30 C 26 24.3 32.3 21.8 38.7 21.3 C 44.5 21.9 50 24.6 52.2 26.5 L 62.7 16" />
                {/* 左下方木叶三角叶托 */}
                <path d="M 21.09 33.05 L 10 56.16 L 37.12 55.85" />
              </g>
            </svg>
          </div>
        )}
      </div>
    </div>,
    document.body
  )
}
