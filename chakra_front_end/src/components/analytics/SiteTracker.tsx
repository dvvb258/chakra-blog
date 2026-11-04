'use client'

import { useEffect } from 'react'
import { recordSystemVisit } from '@/lib/api'

export default function SiteTracker() {
  useEffect(() => {
    // 单次会话只上报一次系统访问，避免 SPA 内部路由切换刷量
    const hasReported = sessionStorage.getItem('ba_visit_reported')
    if (!hasReported) {
      sessionStorage.setItem('ba_visit_reported', 'pending')
      void recordSystemVisit()
        .then(() => sessionStorage.setItem('ba_visit_reported', '1'))
        .catch((error: unknown) => {
          sessionStorage.removeItem('ba_visit_reported')
          console.error('访问量上报失败', error)
        })
    }
  }, [])

  return null
}
