import { getArticles } from '@/lib/api'
import { getSiteConfig } from '@/lib/site-config'
import type { Article } from '@/types'

export const dynamic = 'force-dynamic'

function escapeCdata(str: string): string {
  return str.replace(/]]>/g, ']]]]><![CDATA[>')
}

export async function GET() {
  const config = await getSiteConfig()
  const baseUrl = config.url.replace(/\/$/, '')
  let articles: Article[] = []

  try {
    const res = await getArticles({ page: 1, pageSize: 25 })
    articles = res?.items || []
  } catch (error) {
    console.error('[RSS Feed] 拉取文章列表异常，降级为空列表输出:', error)
  }

  const itemsXml = articles
    .map((article) => {
      const articleUrl = `${baseUrl}/articles/${article.id}`
      const pubDate = article.createdAt
        ? new Date(article.createdAt).toUTCString()
        : new Date().toUTCString()
      const title = escapeCdata(article.title || '无标题')
      const summary = escapeCdata(article.summary || article.title || '')
      const author = escapeCdata(article.author || config.name)
      const category = escapeCdata(article.categoryName || '技术文章')

      return `    <item>
      <title><![CDATA[${title}]]></title>
      <link>${articleUrl}</link>
      <guid isPermaLink="true">${articleUrl}</guid>
      <pubDate>${pubDate}</pubDate>
      <author><![CDATA[${author}]]></author>
      <category><![CDATA[${category}]]></category>
      <description><![CDATA[${summary}]]></description>
    </item>`
    })
    .join('\n')

  const rssXml = `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
  <channel>
    <title><![CDATA[${escapeCdata(config.title)}]]></title>
    <link>${baseUrl}</link>
    <description><![CDATA[${escapeCdata(config.description)}]]></description>
    <language>zh-CN</language>
    <lastBuildDate>${new Date().toUTCString()}</lastBuildDate>
    <atom:link href="${baseUrl}/feed.xml" rel="self" type="application/rss+xml"/>
${itemsXml}
  </channel>
</rss>`

  return new Response(rssXml, {
    status: 200,
    headers: {
      'Content-Type': 'application/xml; charset=utf-8',
      'Cache-Control': 'public, s-maxage=3600, stale-while-revalidate=86400',
    },
  })
}
