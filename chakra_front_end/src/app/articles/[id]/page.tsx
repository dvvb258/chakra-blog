import React, { Suspense } from 'react'
import Link from 'next/link'
import Image from 'next/image'
import { notFound } from 'next/navigation'
import type { Metadata } from 'next'
import {
  Calendar,
  Clock,
  Eye,
  User,
  ArrowLeft,
  ChevronRight,
  Sparkles,
} from 'lucide-react'
import { getArticleById, getArticleNeighbors, getArticles, resolveCoverImage, isExternalUrl } from '@/lib/api'
import TacticalLoader from '@/components/loading/TacticalLoader'
import MarkdownRenderer from '@/components/articles/MarkdownRenderer'
import TableOfContents from '@/components/articles/TableOfContents'
import ArticleInteractions from '@/components/articles/ArticleInteractions'
import ArticleNavigation from '@/components/articles/ArticleNavigation'
import ReadingProgressBar from '@/components/ui/ReadingProgressBar'
import { extractHeadings } from '@/lib/markdown'
import { siteConfig } from '@/config/site'
import { getSiteConfig } from '@/lib/site-config'
import { serializeJsonLd } from '@/lib/structuredData'

interface PageProps {
  params: Promise<{ id: string }>
}

// 说明：RootLayout 已声明 force-dynamic（用于运行期站点配置），此处无需再声明 revalidate

/**
 * 动态 SEO 元数据生成
 */
export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { id } = await params
  // 确保站点配置已就绪（命中缓存时无额外网络开销），避免冷启动时读到默认站名/封面
  await getSiteConfig()

  try {
    const article = await getArticleById(id)
    if (!article) return { title: '文章未找到' }

    return {
      title: `${article.title} · ${siteConfig.name}`,
      description: article.summary,
      keywords: article.tags,
      openGraph: {
        title: article.title,
        description: article.summary,
        type: 'article',
        publishedTime: article.createdAt,
        authors: [article.author || siteConfig.name],
        images: [{ url: resolveCoverImage(article.coverImage) }],
      },
    }
  } catch {
    return { title: '文章详情' }
  }
}

/**
 * 底部推荐阅读异步组件
 */
async function ArticleRecommendationsAsync({ id }: { id: string }) {
  try {
    const recentRes = await getArticles({ pageSize: 4 })
    const otherArticles = (recentRes.items || []).filter((a) => String(a.id) !== String(id)).slice(0, 2)

    if (otherArticles.length === 0) return null

    return (
      <section className="mt-12">
        <div className="flex items-center gap-2 mb-6">
          <Sparkles className="w-5 h-5 text-theme-primary" />
          <h3 className="text-lg font-bold text-slate-800 dark:text-slate-100">
            推荐阅读
          </h3>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {otherArticles.map((item) => (
            <Link
              key={item.id}
              href={`/articles/${item.id}`}
              className="group flex gap-4 p-4 bg-theme-surface/90 rounded-2xl border border-theme-border/60 shadow-paper hover:shadow-paper-hover hover:border-theme-primary transition-all"
            >
              <div className="relative w-28 h-20 rounded-xl overflow-hidden bg-slate-100 dark:bg-slate-800 flex-shrink-0">
                <Image
                  src={resolveCoverImage(item.coverImage)}
                  alt={item.title}
                  fill
                  unoptimized={isExternalUrl(resolveCoverImage(item.coverImage))}
                  className="object-cover group-hover:scale-105 transition-transform"
                  sizes="120px"
                />
              </div>
              <div className="flex-1 flex flex-col justify-between min-w-0">
                <h4 className="text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-100 group-hover:text-theme-primary transition-colors line-clamp-2">
                  {item.title}
                </h4>
                <div className="flex items-center gap-2 text-[11px] font-mono text-slate-400 dark:text-slate-500 mt-1">
                  <span>{item.createdAt}</span>
                  <span>•</span>
                  <span>{item.readTime}</span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      </section>
    )
  } catch {
    return null
  }
}

/**
 * 核心文章正文异步流式组件
 */
async function ArticleMainContentAsync({ id }: { id: string }) {
  const [article, neighbors] = await Promise.all([
    getArticleById(id),
    getArticleNeighbors(id),
  ])

  if (!article) {
    notFound()
  }

  const tocItems = extractHeadings(article.content)

  const articleJsonLd = {
    '@context': 'https://schema.org',
    '@type': 'BlogPosting',
    headline: article.title,
    description: article.summary,
    image: resolveCoverImage(article.coverImage),
    datePublished: article.createdAt,
    author: {
      '@type': 'Person',
      name: article.author || siteConfig.name,
    },
    publisher: {
      '@type': 'Organization',
      name: siteConfig.name,
      logo: {
        '@type': 'ImageObject',
        url: siteConfig.avatar,
      },
    },
  }

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: serializeJsonLd(articleJsonLd) }}
      />
      {/* 核心文章主卡片 */}
      <article className="w-full bg-theme-surface/95 rounded-3xl border border-theme-border/60 shadow-paper overflow-hidden animate-in fade-in duration-300">
        {/* 顶部大幅封面图 */}
        <div className="relative w-full h-64 sm:h-80 md:h-96 bg-slate-100 dark:bg-slate-800">
          <Image
            src={resolveCoverImage(article.coverImage)}
            alt={article.title}
            fill
            priority
            unoptimized={isExternalUrl(resolveCoverImage(article.coverImage))}
            className="object-cover object-center"
            sizes="(max-width: 1200px) 100vw, 1200px"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-slate-950/85 via-slate-950/30 to-transparent" />

          {/* 封面浮动信息 */}
          <div className="absolute bottom-0 left-0 right-0 p-6 sm:p-8 text-white">
            <div className="flex flex-wrap items-center gap-2 mb-3">
              <span className="px-2.5 py-1 bg-theme-primary text-white text-xs font-mono font-bold rounded-lg shadow-xs border border-white/20">
                {article.categoryName}
              </span>
              {article.tags?.map((tag, idx) => (
                <span
                  key={idx}
                  className="px-2 py-0.5 bg-black/40 backdrop-blur-md text-slate-100 text-xs font-mono rounded border border-white/20"
                >
                  #{tag}
                </span>
              ))}
            </div>

            <h1 className="text-2xl sm:text-3xl md:text-4xl font-extrabold tracking-tight drop-shadow-md leading-tight">
              {article.title}
            </h1>

            {/* 统计指标行 */}
            <div className="mt-4 flex flex-wrap items-center gap-4 text-xs text-slate-200 font-mono">
              <span className="flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5 text-theme-primary" />
                <span>{article.createdAt}</span>
              </span>
              <span>•</span>
              <span className="flex items-center gap-1.5">
                <Clock className="w-3.5 h-3.5 text-theme-primary" />
                <span>{article.readTime}</span>
              </span>
              <span>•</span>
              <span className="flex items-center gap-1.5">
                <Eye className="w-3.5 h-3.5 text-slate-300" />
                <span>{article.views} 浏览</span>
              </span>
              <span>•</span>
              <span className="flex items-center gap-1.5">
                <User className="w-3.5 h-3.5 text-emerald-400" />
                <span>{article.author}</span>
              </span>
            </div>
          </div>
        </div>

        {/* 正文区域 */}
        <div className="p-6 sm:p-10 md:p-12">
          {/* 导语引用框 */}
          {article.summary && (
            <div className="p-4 sm:p-5 rounded-2xl bg-theme-surface-hover/80 border border-theme-border/70 mb-8 text-slate-700 dark:text-slate-200 text-sm sm:text-base leading-relaxed flex items-start gap-3 shadow-xs">
              <span className="text-2xl flex-shrink-0">☀️</span>
              <div>
                <span className="text-xs font-bold text-theme-primary block font-mono mb-1">
                  导语摘要
                </span>
                <p className="font-medium text-slate-800 dark:text-slate-200">{article.summary}</p>
              </div>
            </div>
          )}

          {/* 移动端与小宽屏 (< 1440px) 目录折叠框 */}
          {tocItems.length > 0 && (
            <TableOfContents items={tocItems} mode="mobile" />
          )}

          {/* 正文渲染器（支持代码高亮与复制） */}
          <div className="min-w-0">
            <MarkdownRenderer content={article.content} />

            {/* 点赞与交互栏 */}
            <ArticleInteractions
              articleId={article.id}
              initialLikes={article.likes}
              title={article.title}
            />

            {/* 上一篇 / 下一篇 翻阅卡片 */}
            <ArticleNavigation prevArticle={neighbors.prev} nextArticle={neighbors.next} />

            {/* 作者名片 */}
            <div className="mt-10 p-6 bg-theme-surface-hover/60 rounded-2xl border border-theme-border/60 flex flex-col sm:flex-row items-center gap-5">
              <div className="relative w-16 h-16 rounded-full overflow-hidden ring-4 ring-theme-primary/30 shadow-md flex-shrink-0">
                <Image
                  src={siteConfig.avatar}
                  alt={siteConfig.name}
                  fill
                  className="object-cover"
                />
              </div>
              <div className="text-center sm:text-left flex-1">
                <div className="flex items-center justify-center sm:justify-start gap-2">
                  <h4 className="font-extrabold text-slate-800 dark:text-slate-100 text-sm">{siteConfig.name}</h4>
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-theme-surface text-theme-primary font-bold border border-theme-border/60">
                    {siteConfig.authorSubtitle}
                  </span>
                </div>
              </div>
              <Link
                href="/about"
                className="px-4 py-2 bg-theme-surface hover:bg-theme-surface-hover text-theme-primary text-xs font-bold rounded-xl border border-theme-border/70 hover:border-theme-primary transition-colors shadow-xs flex-shrink-0"
              >
                关于博主
              </Link>
            </div>
          </div>
        </div>
      </article>

      {/* 悬浮外挂于居中文章卡片右侧留白处 (≥ 1440px 宽屏展示) */}
      {tocItems.length > 0 && (
        <div className="hidden min-[1440px]:block absolute left-[calc(100%+24px)] top-28 w-72 h-[calc(100%-140px)] pointer-events-none">
          <div className="sticky top-24 pointer-events-auto">
            <TableOfContents items={tocItems} mode="desktop" />
          </div>
        </div>
      )}
    </>
  )
}

/**
 * 流式文章详情页
 */
export default async function ArticleDetailPage({ params }: PageProps) {
  const { id } = await params

  return (
    <>
      {/* 顶部滚动阅读进度条 */}
      <ReadingProgressBar />

      <div className="relative max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 sm:py-12">
        {/* 顶部面包屑 */}
        <nav className="flex items-center gap-2 text-xs text-slate-400 dark:text-slate-500 font-mono mb-6">
          <Link href="/" className="hover:text-theme-primary transition-colors">
            首页
          </Link>
          <ChevronRight className="w-3.5 h-3.5 text-slate-300 dark:text-slate-600" />
          <Link href="/articles" className="hover:text-theme-primary transition-colors">
            文章
          </Link>
          <ChevronRight className="w-3.5 h-3.5 text-slate-300 dark:text-slate-600" />
          <span className="text-slate-600 dark:text-slate-300 truncate max-w-xs sm:max-w-md">
            文章详情
          </span>
        </nav>

        {/* 核心正文区 */}
        <Suspense
          fallback={
            <TacticalLoader
              title="LOADING..."
              subtitle="正在加载文章内容..."
              mode="full"
            />
          }
        >
          <ArticleMainContentAsync id={id} />
        </Suspense>

        {/* 底部推荐阅读 */}
        <Suspense fallback={null}>
          <ArticleRecommendationsAsync id={id} />
        </Suspense>

        {/* 返回列表按钮 */}
        <div className="mt-10 text-center">
          <Link
            href="/articles"
            className="inline-flex items-center gap-2 px-5 py-2.5 bg-theme-surface hover:bg-theme-surface-hover text-slate-700 dark:text-slate-200 hover:text-theme-primary rounded-xl font-bold text-xs border border-theme-border/60 hover:border-theme-primary transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
            <span>返回全部文章列表</span>
          </Link>
        </div>
      </div>
    </>
  )
}
