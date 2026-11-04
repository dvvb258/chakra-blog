import type { MetadataRoute } from 'next'
import { getAllArticlesForSitemap, getAllSeriesForSitemap } from '@/lib/api'
import { siteConfig } from '@/config/site'

const BASE_URL = siteConfig.url.replace(/\/$/, '')

export const dynamic = 'force-dynamic'

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  // 静态路由
  const staticRoutes: MetadataRoute.Sitemap = [
    {
      url: BASE_URL,
      lastModified: new Date(),
      changeFrequency: 'daily',
      priority: 1,
    },
    {
      url: `${BASE_URL}/articles`,
      lastModified: new Date(),
      changeFrequency: 'daily',
      priority: 0.9,
    },
    {
      url: `${BASE_URL}/series`,
      lastModified: new Date(),
      changeFrequency: 'weekly',
      priority: 0.8,
    },
    {
      url: `${BASE_URL}/about`,
      lastModified: new Date(),
      changeFrequency: 'monthly',
      priority: 0.5,
    },
  ]

  // 动态文章路由（使用文章真实的发布或更新日期）
  const [articles, series] = await Promise.all([
    getAllArticlesForSitemap(),
    getAllSeriesForSitemap(),
  ])
  const articleRoutes: MetadataRoute.Sitemap = articles.map((art) => {
    const parsedDate = art.updatedAt ? new Date(art.updatedAt) : new Date()
    const validDate = isNaN(parsedDate.getTime()) ? new Date() : parsedDate
    return {
      url: `${BASE_URL}/articles/${art.id}`,
      lastModified: validDate,
      changeFrequency: 'weekly' as const,
      priority: 0.7,
    }
  })

  // 动态系列路由（使用专栏真实的更新日期）
  const seriesRoutes: MetadataRoute.Sitemap = series.map((s) => {
    const parsedDate = s.updatedAt ? new Date(s.updatedAt) : new Date()
    const validDate = isNaN(parsedDate.getTime()) ? new Date() : parsedDate
    return {
      url: `${BASE_URL}/series/${s.id}`,
      lastModified: validDate,
      changeFrequency: 'weekly' as const,
      priority: 0.6,
    }
  })

  return [...staticRoutes, ...articleRoutes, ...seriesRoutes]
}
