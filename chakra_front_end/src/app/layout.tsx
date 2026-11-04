import React from 'react'
import type { Metadata, Viewport } from 'next'
import './globals.css'
import Navbar from '@/components/layout/Navbar'
import Footer from '@/components/layout/Footer'
import StartupLoader from '@/components/loading/StartupLoader'
import RouteLoadingIndicator from '@/components/loading/RouteLoadingIndicator'
import SiteTracker from '@/components/analytics/SiteTracker'
import BackToTop from '@/components/ui/BackToTop'
import { ThemeProvider } from '@/components/theme/ThemeProvider'
import { siteConfig } from '@/config/site'

export const dynamic = 'force-dynamic'

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#fffdfa' },
    { media: '(prefers-color-scheme: dark)', color: '#080c16' },
  ],
}

export const metadata: Metadata = {
  title: {
    default: siteConfig.title,
    template: `%s · ${siteConfig.siteName}`,
  },
  description: siteConfig.description,
  keywords: siteConfig.keywords,
  authors: [{ name: siteConfig.name }],
  creator: siteConfig.name,
  metadataBase: new URL(siteConfig.url),
  openGraph: {
    type: 'website',
    locale: 'zh_CN',
    siteName: siteConfig.siteName,
    title: siteConfig.title,
    description: siteConfig.description,
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  icons: {
    icon: [
      { url: siteConfig.favicon, type: 'image/svg+xml' },
    ],
    apple: siteConfig.avatar,
  },
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="zh-CN" suppressHydrationWarning>
      <body className="flex flex-col min-h-screen selection:bg-theme-primary selection:text-white transition-colors duration-300">
        <ThemeProvider attribute="class" defaultTheme="light" enableSystem={false}>
          {/* 初始会话启动加载动效 */}
          <StartupLoader />
          {/* 页面路由切换加载进度条指示器 */}
          <RouteLoadingIndicator />
          {/* 会话级系统访问量轻量上报 */}
          <SiteTracker />

          <Navbar />
          <main className="flex-grow">
            {children}
          </main>
          <Footer />
          {/* 全局返回顶部悬浮组件 */}
          <BackToTop />
        </ThemeProvider>
      </body>
    </html>
  )
}
