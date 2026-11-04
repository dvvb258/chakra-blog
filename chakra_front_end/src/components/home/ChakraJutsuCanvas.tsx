'use client'

import React, { useEffect, useRef } from 'react'

interface ChakraJutsuCanvasProps {
  jutsuType: 'rasengan' | 'chidori'
  active: boolean // 是否处于点击爆发态
  fading: boolean // 爆发坍缩消散中
  mode?: 'full' | 'idle' // 'full' 完整狂暴释放；'idle' 待机微弱凝聚与消散
  width?: number
  height?: number
  className?: string
}

interface Particle {
  x: number
  y: number
  vx: number
  vy: number
  radius: number
  color: string
  alpha: number
  decay: number
  life: number
  angle?: number
  distance?: number
  speed?: number
}

interface LightningSegment {
  x1: number
  y1: number
  x2: number
  y2: number
  color: string
  width: number
  alpha: number
}

/**
 * 查克拉动力学粒子画布引擎
 * - 待机状态 (mode = 'idle'): 掌心周期性微弱凝聚 (Mini Gathering) -> 盘旋/小电弧微闪 -> 随后轻轻消散
 * - 完整释放 (mode = 'full'): 用户点击掌心，瞬间暴鸣轰击，全能查克拉完全体狂暴爆发
 */
export default function ChakraJutsuCanvas({
  jutsuType,
  active,
  fading,
  mode = 'full',
  width = 240,
  height = 240,
  className = '',
}: ChakraJutsuCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const animFrameIdRef = useRef<number | null>(null)
  const particlesRef = useRef<Particle[]>([])
  const boltsRef = useRef<LightningSegment[][]>([])
  const frameCountRef = useRef(0)
  const opacityRef = useRef(1)

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return

    // 高分屏缩放适配 (最大 2x 防止过量消耗)
    const dpr = typeof window !== 'undefined' ? Math.min(window.devicePixelRatio || 1, 2) : 1
    canvas.width = width * dpr
    canvas.height = height * dpr
    ctx.scale(dpr, dpr)

    const centerX = width / 2
    const centerY = height / 2

    // 初始化螺旋丸粒子池
    const initRasenganParticles = (count: number, maxDist: number) => {
      const colors = ['#ffffff', '#bae6fd', '#38bdf8', '#0284c7', '#f59e0b']
      particlesRef.current = Array.from({ length: count }).map((_, i) => ({
        x: centerX,
        y: centerY,
        vx: 0,
        vy: 0,
        radius: Math.random() * 2.0 + 0.8,
        color: colors[i % colors.length],
        alpha: Math.random() * 0.7 + 0.3,
        decay: 0.005,
        life: 1,
        angle: Math.random() * Math.PI * 2,
        distance: Math.random() * maxDist + 8,
        speed: (Math.random() * 0.08 + 0.05) * (Math.random() > 0.15 ? 1 : -1),
      }))
    }

    if (jutsuType === 'rasengan') {
      initRasenganParticles(mode === 'full' ? 90 : 28, mode === 'full' ? 75 : 42)
    }

    // 分形电弧生成函数 (Recursive Midpoint Displacement)
    const generateLightningBranch = (
      x1: number,
      y1: number,
      x2: number,
      y2: number,
      displace: number,
      branchProbability: number
    ): LightningSegment[] => {
      const segments: LightningSegment[] = []

      const divide = (ax: number, ay: number, bx: number, by: number, d: number) => {
        if (d < 4.5) {
          segments.push({
            x1: ax,
            y1: ay,
            x2: bx,
            y2: by,
            color: Math.random() > 0.3 ? '#ffffff' : Math.random() > 0.4 ? '#67e8f9' : '#c084fc',
            width: Math.random() * 2.6 + 1.2,
            alpha: Math.random() * 0.4 + 0.6,
          })
          return
        }

        const midX = (ax + bx) / 2 + (Math.random() - 0.5) * d * 1.35
        const midY = (ay + by) / 2 + (Math.random() - 0.5) * d * 1.35
        divide(ax, ay, midX, midY, d / 2)
        divide(midX, midY, bx, by, d / 2)

        // 随机侧支叉雷电
        if (Math.random() < branchProbability) {
          const subAngle = Math.atan2(by - ay, bx - ax) + (Math.random() - 0.5) * 1.3
          const subLen = Math.random() * 32 + 14
          const subX = midX + Math.cos(subAngle) * subLen
          const subY = midY + Math.sin(subAngle) * subLen
          divide(midX, midY, subX, subY, d / 2.2)
        }
      }

      divide(x1, y1, x2, y2, displace)
      return segments
    }

    // 动画主循环
    const render = () => {
      frameCountRef.current += 1
      const time = frameCountRef.current
      ctx.clearRect(0, 0, width, height)

      const isFull = mode === 'full'

      // 计算当前透明度与待机阶段生命周期
      let currentAlpha = 1
      let isIdleGathering = false

      if (!isFull) {
        // 待机模式：约 6 秒一个呼吸凝聚周期 (360 帧)
        const cycle = time % 360
        if (cycle < 60) {
          // 阶段 0: 静息微芒 (0 ~ 1.0s)
          currentAlpha = 0.18
        } else if (cycle >= 60 && cycle < 130) {
          // 阶段 1: 开始向心微凝聚 (1.0s ~ 2.2s)
          const p = (cycle - 60) / 70
          currentAlpha = 0.18 + p * 0.58 // 升至 0.76
          isIdleGathering = true
        } else if (cycle >= 130 && cycle < 220) {
          // 阶段 2: 保持微弱盘旋 / 电弧微闪 (2.2s ~ 3.7s)
          currentAlpha = 0.72 + Math.sin(time * 0.15) * 0.08
          isIdleGathering = true
        } else if (cycle >= 220 && cycle < 290) {
          // 阶段 3: 向心轻柔消散坍缩 (3.7s ~ 4.8s)
          const p = (cycle - 220) / 70
          currentAlpha = 0.72 * (1 - p) // 衰减至 0
          isIdleGathering = p < 0.6
        } else {
          // 阶段 4: 完全平息 (4.8s ~ 6.0s)
          currentAlpha = 0.05
        }
      } else {
        // 完整爆发模式: 淡入淡出插值
        if (fading) {
          opacityRef.current = Math.max(0, opacityRef.current - 0.045)
        } else {
          opacityRef.current = Math.min(1, opacityRef.current + 0.12)
        }
        currentAlpha = opacityRef.current
      }

      ctx.globalAlpha = Math.max(0, Math.min(1, currentAlpha))

      // ========================================================
      // 1. 鸣人 · 螺旋丸 (Rasengan)
      // ========================================================
      if (jutsuType === 'rasengan') {
        const coreJitterX = isFull ? (Math.random() - 0.5) * 2.5 : 0
        const coreJitterY = isFull ? (Math.random() - 0.5) * 2.5 : 0
        const baseRadius = isFull ? 30 : 22
        const pulseAmp = isFull ? 4 : 3.2
        const corePulse = Math.max(0.1, Math.sin(time * 0.2) * pulseAmp + baseRadius)

        // 核心等离子光核
        const coreGradient = ctx.createRadialGradient(
          centerX + coreJitterX,
          centerY + coreJitterY,
          isFull ? 2 : 1,
          centerX,
          centerY,
          corePulse
        )
        coreGradient.addColorStop(0, '#ffffff')
        coreGradient.addColorStop(0.3, '#e0f2fe')
        coreGradient.addColorStop(0.65, '#38bdf8')
        coreGradient.addColorStop(0.9, '#0284c7')
        coreGradient.addColorStop(1, 'rgba(2, 132, 199, 0)')

        ctx.beginPath()
        ctx.arc(centerX + coreJitterX, centerY + coreJitterY, corePulse, 0, Math.PI * 2)
        ctx.fillStyle = coreGradient
        ctx.fill()

        // 完整释放与待机微凝聚的风刃旋涡
        if (isFull || isIdleGathering) {
          ctx.save()
          ctx.translate(centerX, centerY)
          ctx.rotate(time * (isFull ? 0.14 : 0.08))

          const bladeCount = isFull ? 3 : 2
          const bladeRadius = isFull
            ? Math.max(0.1, 42 + Math.sin(time * 0.15) * 3)
            : Math.max(0.1, 33 + Math.sin(time * 0.12) * 2.5)

          for (let b = 0; b < bladeCount; b++) {
            ctx.beginPath()
            ctx.rotate((Math.PI * 2) / bladeCount)
            ctx.arc(0, 0, bladeRadius, 0, isFull ? Math.PI * 0.8 : Math.PI * 0.65)
            ctx.strokeStyle = isFull ? 'rgba(255, 255, 255, 0.85)' : 'rgba(224, 242, 254, 0.75)'
            ctx.lineWidth = isFull ? 3 : 1.8
            ctx.shadowColor = '#38bdf8'
            ctx.shadowBlur = isFull ? 12 : 8
            ctx.stroke()
          }
          ctx.restore()
        }

        // 完整释放时独有的逆向反转气流外环
        if (isFull) {
          ctx.save()
          ctx.translate(centerX, centerY)
          ctx.rotate(-time * 0.09)
          ctx.beginPath()
          ctx.arc(0, 0, 52, 0, Math.PI * 1.2)
          ctx.strokeStyle = 'rgba(56, 189, 248, 0.65)'
          ctx.lineWidth = 2.2
          ctx.setLineDash([8, 6])
          ctx.stroke()
          ctx.restore()
        }

        // 渲染向心旋转查克拉粒子
        particlesRef.current.forEach((p, idx) => {
          if (!p.angle || p.distance === undefined || !p.speed) return
          // 待机模式保留适量活跃粒子
          if (!isFull && idx > 24) return

          p.angle += p.speed * (isFull ? 1 : 0.7)
          const distOffset = Math.sin(time * 0.1 + p.angle * 2) * (isFull ? 6 : 3.5)
          const curDist = (isFull ? p.distance : p.distance * 0.7) + distOffset
          const px = centerX + Math.cos(p.angle) * curDist
          const py = centerY + Math.sin(p.angle) * curDist
          const pr = Math.max(0.1, isFull ? p.radius : p.radius * 0.85)

          ctx.beginPath()
          ctx.arc(px, py, pr, 0, Math.PI * 2)
          ctx.fillStyle = p.color
          ctx.shadowColor = p.color
          ctx.shadowBlur = isFull ? 8 : 5
          ctx.fill()
        })
      }

      // ========================================================
      // 2. 佐助 · 千鸟 (Chidori) - 纯正高压无规则撕裂电芒
      // 彻底摒弃任何圆形光球，还原动漫原著暴烈撕裂空气的千鸟雷光
      // ========================================================
      if (jutsuType === 'chidori') {
        // 电弧生成逻辑
        if (isFull) {
          // 完整释放：每 2 帧重新生成 12~16 条向外暴烈穿刺的高压闪电束 (射程 75~170px)
          if (time % 2 === 0) {
            const boltCount = Math.floor(Math.random() * 5) + 12
            const newBolts: LightningSegment[][] = []

            // 1. 中心高密集交叉穿刺核心电芒 (替代原先的伪圆球)
            for (let i = 0; i < 6; i++) {
              const ang1 = Math.random() * Math.PI * 2
              const ang2 = ang1 + Math.PI + (Math.random() - 0.5) * 1.2
              const r1 = Math.random() * 18 + 8
              const r2 = Math.random() * 18 + 8
              newBolts.push(generateLightningBranch(
                centerX + Math.cos(ang1) * r1,
                centerY + Math.sin(ang1) * r1,
                centerX + Math.cos(ang2) * r2,
                centerY + Math.sin(ang2) * r2,
                16,
                0.35
              ))
            }

            // 2. 向四周空气剧烈击穿撕裂的放射状主闪电
            for (let i = 0; i < boltCount; i++) {
              const angle = Math.random() * Math.PI * 2
              const reach = Math.random() * 95 + 75
              const targetX = centerX + Math.cos(angle) * reach
              const targetY = centerY + Math.sin(angle) * reach
              newBolts.push(generateLightningBranch(centerX, centerY, targetX, targetY, 42, 0.65))
            }
            boltsRef.current = newBolts
          }
        } else if (isIdleGathering) {
          // 待机微凝聚态：每 4 帧在掌心跳跃 3~5 条微型折线电火花 (射程 35~65px)
          if (time % 4 === 0) {
            const boltCount = Math.floor(Math.random() * 3) + 3
            const newBolts: LightningSegment[][] = []

            // 中心微型电核微闪
            const angCore = Math.random() * Math.PI * 2
            newBolts.push(generateLightningBranch(
              centerX - Math.cos(angCore) * 12,
              centerY - Math.sin(angCore) * 12,
              centerX + Math.cos(angCore) * 12,
              centerY + Math.sin(angCore) * 12,
              10,
              0.2
            ))

            for (let i = 0; i < boltCount; i++) {
              const angle = Math.random() * Math.PI * 2
              const reach = Math.random() * 30 + 32
              const targetX = centerX + Math.cos(angle) * reach
              const targetY = centerY + Math.sin(angle) * reach
              newBolts.push(generateLightningBranch(centerX, centerY, targetX, targetY, 20, 0.35))
            }
            boltsRef.current = newBolts
          }
        } else {
          boltsRef.current = []
        }

        // 掌心高压电离微型白炽辉光 (非实心球，而是不规则电晕)
        if (isFull || isIdleGathering) {
          const sparkSize = isFull ? (Math.random() * 12 + 20) : 8
          ctx.save()
          ctx.shadowColor = '#67e8f9'
          ctx.shadowBlur = isFull ? 25 : 12

          // 绘制四角暴闪电星芒
          ctx.fillStyle = '#ffffff'
          const jitterX = (Math.random() - 0.5) * 4
          const jitterY = (Math.random() - 0.5) * 4
          ctx.beginPath()
          const ox = centerX + jitterX
          const oy = centerY + jitterY
          ctx.moveTo(ox - sparkSize, oy)
          ctx.lineTo(ox, oy - sparkSize * 0.25)
          ctx.lineTo(ox, oy - sparkSize)
          ctx.lineTo(ox + sparkSize * 0.25, oy)
          ctx.lineTo(ox + sparkSize, oy)
          ctx.lineTo(ox, oy + sparkSize * 0.25)
          ctx.lineTo(ox, oy + sparkSize)
          ctx.lineTo(ox - sparkSize * 0.25, oy)
          ctx.closePath()
          ctx.fill()
          ctx.restore()
        }

        // 绘制电弧
        boltsRef.current.forEach((bolt) => {
          bolt.forEach((seg) => {
            ctx.beginPath()
            ctx.moveTo(seg.x1, seg.y1)
            ctx.lineTo(seg.x2, seg.y2)
            ctx.strokeStyle = seg.color
            ctx.lineWidth = isFull ? seg.width : Math.max(1.2, seg.width * 0.75)
            ctx.shadowColor = seg.color
            ctx.shadowBlur = isFull ? 22 : 12
            ctx.stroke()
          })
        })

        // 等离子火花粒子 (完整释放狂暴四射；待机时轻柔飘落)
        const maxSparks = isFull ? 70 : 12
        if (particlesRef.current.length < maxSparks) {
          const sparkAngle = Math.random() * Math.PI * 2
          const sparkSpeed = isFull ? Math.random() * 6 + 2.5 : Math.random() * 2 + 0.8
          particlesRef.current.push({
            x: centerX,
            y: centerY,
            vx: Math.cos(sparkAngle) * sparkSpeed,
            vy: Math.sin(sparkAngle) * sparkSpeed,
            radius: isFull ? Math.random() * 2.6 + 1.2 : Math.random() * 1.6 + 0.8,
            color: Math.random() > 0.35 ? '#ffffff' : Math.random() > 0.45 ? '#67e8f9' : '#e9d5ff',
            alpha: 1,
            decay: isFull ? Math.random() * 0.035 + 0.018 : Math.random() * 0.045 + 0.025,
            life: 1,
          })
        }

        particlesRef.current.forEach((p) => {
          p.x += p.vx
          p.y += p.vy
          p.life -= p.decay

          const sparkRadius = Math.max(0, p.radius * p.life)
          if (sparkRadius <= 0.05) return

          ctx.beginPath()
          ctx.arc(p.x, p.y, sparkRadius, 0, Math.PI * 2)
          ctx.fillStyle = p.color
          ctx.shadowColor = p.color
          ctx.shadowBlur = isFull ? 10 : 5
          ctx.fill()
        })

        particlesRef.current = particlesRef.current.filter((p) => p.life > 0.05)
      }

      animFrameIdRef.current = requestAnimationFrame(render)
    }

    animFrameIdRef.current = requestAnimationFrame(render)

    return () => {
      if (animFrameIdRef.current) {
        cancelAnimationFrame(animFrameIdRef.current)
      }
    }
  }, [jutsuType, active, fading, mode, width, height])

  return (
    <canvas
      ref={canvasRef}
      className={`pointer-events-none absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 ${className}`}
      style={{
        width: `${width}px`,
        height: `${height}px`,
      }}
    />
  )
}
