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
import { getSiteConfig } from '@/lib/site-config'
import { serializeJsonLd } from '@/lib/structuredData'

export const dynamic = 'force-dynamic'

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#fffdfa' },
    { media: '(prefers-color-scheme: dark)', color: '#080c16' },
  ],
}

/**
 * 站点元数据：从后端「站点设置」读取，后台修改后 60 秒内生效，无需重新打包
 */
export async function generateMetadata(): Promise<Metadata> {
  const config = await getSiteConfig()

  return {
    title: {
      default: config.title,
      template: `%s · ${config.siteName}`,
    },
    description: config.description,
    keywords: config.keywords,
    authors: [{ name: config.name }],
    creator: config.name,
    metadataBase: new URL(config.url),
    alternates: {
      canonical: config.url,
      types: {
        'application/rss+xml': `${config.url.replace(/\/$/, '')}/feed.xml`,
      },
    },
    openGraph: {
      type: 'website',
      locale: 'zh_CN',
      siteName: config.siteName,
      title: config.title,
      description: config.description,
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
        { url: config.favicon, type: 'image/svg+xml' },
      ],
      apple: config.avatar,
    },
  }
}

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const config = await getSiteConfig()

  return (
    <html lang="zh-CN" suppressHydrationWarning>
      <head>
        {/* 运行期站点配置：服务端合并数据库覆盖项后注入，客户端组件据此渲染 */}
        <script
          id="chakra-runtime-site-config"
          dangerouslySetInnerHTML={{
            __html: `window.__CHAKRA_SITE__=${serializeJsonLd(config)}`,
          }}
        />
      </head>
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
