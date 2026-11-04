import React from 'react'
import Link from 'next/link'
import Image from 'next/image'
import { ArrowUpRight, BookOpen, Eye, Calendar, Clock } from 'lucide-react'
import HeroSection from '@/components/home/HeroSection'
import { getArticles, getCategories, isExternalUrl } from '@/lib/api'
import { siteConfig } from '@/config/site'
import { formatReadingTime } from '@/lib/reading'
import { serializeJsonLd } from '@/lib/structuredData'

// 强制运行时动态渲染，避免构建期因内网不可达打包出空白死页面
export const dynamic = 'force-dynamic'

export default async function HomePage() {
  const [articlesRes, categories] = await Promise.all([
    getArticles({ pageSize: 6 }),
    getCategories(),
  ])

  const latestArticles = articlesRes.items || []

  const websiteJsonLd = {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    name: siteConfig.siteName,
    url: siteConfig.url,
    description: siteConfig.title,
    author: {
      '@type': 'Person',
      name: siteConfig.name,
    },
  }

  const homeConfig = siteConfig.home

  return (
    <div className="ba-home">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: serializeJsonLd(websiteJsonLd) }}
      />
      {/* 1. 首屏展示区 */}
      <HeroSection />

      {/* 2. 最新文章展示区 */}
      <section id="latest-posts" className="ba-journal ba-container mt-14 mb-20">
        <header className="ba-section-heading">
          <h2>{homeConfig.latestArticlesTitle}</h2>
          <Link href="/articles" className="ba-inline-link">
            {homeConfig.allArticlesText} ({articlesRes.total || latestArticles.length}) <ArrowUpRight size={18} />
          </Link>
        </header>

        {/* 分类快捷筛选栏 */}
        <nav className="ba-collection-tabs" aria-label="按分类阅读">
          <Link href="/articles" className="ba-collection-tab active">
            <BookOpen size={16} />
            {homeConfig.allArticlesText}
            <span>{articlesRes.total}</span>
          </Link>
          {categories.filter((c) => c.key !== 'all').map((cat) => (
            <Link
              key={cat.key}
              href={`/articles?category=${cat.key}`}
              className="ba-collection-tab"
            >
              {cat.label}
              <span>{cat.count}</span>
            </Link>
          ))}
        </nav>

        {/* 文章卡片网格 */}
        {latestArticles.length > 0 ? (
          <div className="ba-post-grid">
            {latestArticles.map((post) => {
              const readingTimeText = formatReadingTime(post.readingTime)
              return (
                <article key={post.id} className="ba-post-card">
                  {/* 顶部分类与阅读时间 */}
                  <div className="ba-post-topline">
                    <span className="flex items-center gap-1 font-semibold text-theme-primary">
                      <span>{post.categoryName}</span>
                    </span>
                    <span className="flex items-center gap-1 font-mono text-[11px] px-2 py-0.5 rounded-md border font-bold bg-theme-surface-hover text-theme-primary border-theme-border/70">
                      <Clock size={12} className="text-theme-primary" />
                      <span>{readingTimeText}</span>
                    </span>
                  </div>

                  {/* 封面图 */}
                  <Link
                    href={`/articles/${post.id}`}
                    className="ba-post-image mt-2"
                    tabIndex={-1}
                    aria-hidden="true"
                  >
                    <Image
                      src={post.coverImage}
                      alt={post.title}
                      fill
                      unoptimized={isExternalUrl(post.coverImage)}
                      className="object-cover"
                      sizes="(max-width: 768px) 100vw, 400px"
                    />
                  </Link>

                  {/* 正文摘录 */}
                  <div className="ba-post-copy">
                    <div className="flex items-center gap-2 text-xs text-slate-400 dark:text-slate-500 font-mono mb-2">
                      <Calendar size={13} className="text-theme-primary/80" />
                      <span>{post.createdAt}</span>
                      <span>•</span>
                      <span>by {post.author}</span>
                    </div>

                    <h3>
                      <Link href={`/articles/${post.id}`}>
                        {post.title}
                      </Link>
                    </h3>

                    <p>{post.summary}</p>
                  </div>

                  {/* 底部信息与快速开启 */}
                  <footer className="ba-post-bottom">
                    <div>
                      {post.tags.slice(0, 2).map((tag, idx) => (
                        <span key={idx} className="font-mono text-[11px] text-slate-500 dark:text-slate-400">
                          #{tag}
                        </span>
                      ))}
                    </div>

                    <span>
                      <Eye size={14} />
                      {post.views}
                    </span>

                    <Link
                      href={`/articles/${post.id}`}
                      aria-label={`阅读：${post.title}`}
                      className="ba-post-open"
                    >
                      <ArrowUpRight size={18} />
                    </Link>
                  </footer>
                </article>
              )
            })}
          </div>
        ) : (
          <div className="text-center py-16 bg-theme-surface/90 rounded-3xl border border-theme-border/60 p-8 shadow-paper text-slate-400 dark:text-slate-500 text-xs font-mono">
            {homeConfig.emptyArticlesText}
          </div>
        )}
      </section>
    </div>
  )
}
