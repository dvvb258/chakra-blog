import { Article, ArticleQueryParams, Category, PagedList, Series, SeriesQueryParams } from '@/types'
import { siteConfig } from '@/config/site'

/** 站点默认作者名（运行期读取，避免在模块初始化阶段固化为默认值） */
function defaultAuthor(): string {
  return siteConfig.name
}

/**
 * 获取 API 基础地址：
 * - 服务端 (Node.js)：直接连后端 WebAPI 地址（容器网络走内网服务名，本地走 5000）
 * - 浏览器端：永远只用相对路径 '/api'，统一由 Next.js 反向代理转发
 */
export function getApiBaseUrl(): string {
  if (typeof window === 'undefined') {
    return process.env.INTERNAL_API_BASE_URL || 'http://localhost:5000/api'
  }
  return '/api'
}

/** ISR 默认重新验证周期 (秒) */
const DEFAULT_REVALIDATE = 60

/**
 * 统一解析封面图片地址：
 * 1. 优先使用 API 返回的 coverUrl
 * 2. 未提供则使用 siteConfig.defaultCover
 */
export function resolveCoverImage(cover?: string | null): string {
  const trimmed = cover?.trim()
  return trimmed ? trimmed : siteConfig.defaultCover
}

/**
 * 判断是否为外部绝对网络链接（S3 / OSS / 外部图床）
 */
export function isExternalUrl(url?: string | null): boolean {
  if (!url) return false
  const trimmed = url.trim()
  return trimmed.startsWith('http://') || trimmed.startsWith('https://')
}

// ---- 原始后端数据类型 ----

interface RawCategoryData {
  id: string | number
  name: string
  description?: string
  icon?: string
  sort?: number
  articleCount?: number
}

interface RawArticleData {
  id?: string | number
  title?: string
  summary?: string
  content?: string
  coverUrl?: string
  categoryId?: string | number
  categoryName?: string
  category?: string
  tags?: string[]
  author?: string
  readingTime?: number
  views?: number
  likes?: number
  pinned?: boolean
  isPinned?: boolean
  createdAt?: string
  updatedAt?: string
  seriesId?: string | number | null
}

interface RawSeriesData {
  id?: string | number
  title?: string
  name?: string
  description?: string
  coverUrl?: string
  articleCount?: number
  articles?: RawArticleData[]
  updatedAt?: string
  createdAt?: string
  tags?: string[]
}

// ---- Fetch 工具函数 ----

function buildUrl(path: string, params?: Record<string, string | number | undefined>): string {
  const baseUrl = getApiBaseUrl().replace(/\/$/, '')
  const cleanPath = path.startsWith('/') ? path : `/${path}`
  let url = `${baseUrl}${cleanPath}`
  if (params) {
    const searchParams = new URLSearchParams()
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null && value !== '') {
        searchParams.set(key, String(value))
      }
    }
    const qs = searchParams.toString()
    if (qs) url += `?${qs}`
  }
  return url
}

async function apiFetch<T>(url: string, init?: RequestInit, allowNotFound = false): Promise<T | null> {
  const controller = new AbortController()
  const timeoutId = setTimeout(() => controller.abort(), 8000)
  try {
    const res = await fetch(url, {
      ...init,
      signal: controller.signal,
      headers: {
        'Content-Type': 'application/json',
        ...(init?.headers || {}),
      },
    })
    if (allowNotFound && res.status === 404) return null
    if (!res.ok) {
      let serverMessage = `内容服务响应异常：HTTP ${res.status}`
      try {
        const body = (await res.json()) as { message?: string }
        if (body?.message) serverMessage = body.message
      } catch {
        // 非 JSON 格式保持 HTTP 状态码说明
      }
      const apiError = new Error(serverMessage)
      ;(apiError as { status?: number }).status = res.status
      throw apiError
    }
    return (await res.json()) as T
  } catch (error: unknown) {
    console.error('[Content API]', new URL(url, 'http://localhost').pathname, error)
    if (error instanceof Error && error.name !== 'AbortError' && error.message && !error.message.startsWith('Failed to fetch')) {
      throw error
    }
    throw new Error('内容服务暂时不可用，请稍后重试')
  } finally {
    clearTimeout(timeoutId)
  }
}

// ---- 数据标准化 ----

/**
 * 标准化文章数据格式，对齐后端数据与界面渲染规范
 */
export function normalizeArticle(item: RawArticleData): Article {
  if (!item) {
    return {
      id: '0',
      title: '无标题文章',
      summary: '',
      content: '',
      coverImage: resolveCoverImage(null),
      category: 'general',
      categoryName: '通用文章',
      tags: [],
      author: defaultAuthor(),
      readingTime: 0,
      readTime: '0 分钟',
      views: 0,
      likes: 0,
      createdAt: '',
      seriesId: null,
    }
  }

  const readMinutes = item.readingTime || 0
  const formattedReadTime = readMinutes > 0 ? `${readMinutes} 分钟` : '3 分钟'

  return {
    ...item,
    id: String(item.id),
    title: item.title || '无标题文章',
    summary: item.summary || (item.content ? item.content.slice(0, 120) + '...' : ''),
    content: item.content || '',
    coverImage: resolveCoverImage(item.coverUrl),
    category: String(item.categoryId || item.category || '0'),
    categoryName: item.categoryName || '未分类',
    tags: Array.isArray(item.tags) ? item.tags : [],
    author: item.author || defaultAuthor(),
    readingTime: readMinutes,
    readTime: formattedReadTime,
    views: item.views || 0,
    likes: item.likes || 0,
    pinned: Boolean(item.pinned || item.isPinned || false),
    createdAt: typeof item.createdAt === 'string' ? item.createdAt.split('T')[0] : '',
    seriesId: item.seriesId || null,
  }
}

/**
 * 标准化系列数据格式
 */
export function normalizeSeries(item: RawSeriesData): Series {
  if (!item) {
    return {
      id: '0',
      title: '专栏系列',
      name: '专栏系列',
      description: '',
      coverImage: resolveCoverImage(null),
      articleCount: 0,
      updatedAt: '',
      tags: [],
      articles: [],
    }
  }

  return {
    ...item,
    id: String(item.id),
    title: item.name || item.title || '专栏系列',
    name: item.name || item.title || '专栏系列',
    description: item.description || '',
    coverImage: resolveCoverImage(item.coverUrl),
    articleCount: item.articleCount || (item.articles ? item.articles.length : 0),
    updatedAt: typeof item.updatedAt === 'string' ? item.updatedAt.split('T')[0] : (item.createdAt?.split('T')[0] || ''),
    tags: Array.isArray(item.tags) ? item.tags : [],
    articles: Array.isArray(item.articles) ? item.articles.map(normalizeArticle) : [],
  }
}

// ---- API 请求函数 ----

/**
 * 获取文章列表 (/api/articles)
 */
export async function getArticles(params: ArticleQueryParams = {}): Promise<PagedList<Article>> {
  const { keyword = '', category = '', tag = '', page = 1, pageSize = 6, sortBy } = params

  const url = buildUrl('/articles', {
    keyword: keyword || undefined,
    categoryId: category === 'all' ? undefined : (category || undefined),
    tag: tag || undefined,
    pageIndex: page,
    pageSize: pageSize,
    sortBy: sortBy || undefined,
  })

  const raw = await apiFetch<{ data?: { items?: RawArticleData[]; total?: number }; items?: RawArticleData[]; total?: number }>(url, {
    next: { revalidate: DEFAULT_REVALIDATE },
  })

  const payload = raw?.data || raw
  if (payload && Array.isArray(payload.items)) {
    return {
      items: payload.items.map(normalizeArticle),
      total: payload.total || payload.items.length,
      page,
      pageSize,
    }
  }

  throw new Error('文章列表响应格式不正确')
}

/**
 * 获取单篇文章详情 (/api/articles/:id)
 */
export async function getArticleById(id: string | number): Promise<Article | null> {
  const url = buildUrl(`/articles/${id}`)
  const raw = await apiFetch<{ data?: RawArticleData } & RawArticleData>(url, {
    next: { revalidate: DEFAULT_REVALIDATE },
  }, true)

  const payload = raw?.data || raw
  if (payload && (payload.id || payload.title)) {
    return normalizeArticle(payload)
  }

  return null
}

/**
 * 获取文章的相邻文章（调用专用接口 /api/articles/:id/neighbors）
 */
export async function getArticleNeighbors(id: string | number): Promise<{
  prev: Pick<Article, 'id' | 'title'> | null
  next: Pick<Article, 'id' | 'title'> | null
}> {
  const url = buildUrl(`/articles/${id}/neighbors`)
  const raw = await apiFetch<{ data?: { prev?: RawArticleData; next?: RawArticleData } }>(url, {
    next: { revalidate: DEFAULT_REVALIDATE },
  }, true)

  if (raw?.data) {
    return {
      prev: raw.data.prev ? { id: String(raw.data.prev.id), title: raw.data.prev.title || '' } : null,
      next: raw.data.next ? { id: String(raw.data.next.id), title: raw.data.next.title || '' } : null,
    }
  }

  return { prev: null, next: null }
}

/**
 * 获取系列列表 (/api/series)
 */
export async function getSeriesList(params: SeriesQueryParams = {}): Promise<{ items: Series[]; total: number }> {
  const { keyword = '', page = 1, pageSize = 10 } = params

  const url = buildUrl('/series', {
    keyword: keyword || undefined,
    pageIndex: page,
    pageSize: pageSize,
  })

  const raw = await apiFetch<{ data?: { items?: RawSeriesData[]; total?: number }; items?: RawSeriesData[]; total?: number }>(url, {
    next: { revalidate: DEFAULT_REVALIDATE },
  })

  const payload = raw?.data || raw
  if (payload && Array.isArray(payload.items)) {
    return {
      items: payload.items.map(normalizeSeries),
      total: payload.total || payload.items.length,
    }
  }

  throw new Error('专栏列表响应格式不正确')
}

/**
 * 获取系列详情 (/api/series/:id)
 */
export async function getSeriesById(id: string | number): Promise<Series | null> {
  const url = buildUrl(`/series/${id}`)
  const raw = await apiFetch<{ data?: RawSeriesData } & RawSeriesData>(url, {
    next: { revalidate: DEFAULT_REVALIDATE },
  }, true)

  const payload = raw?.data || raw
  if (payload && (payload.id || payload.name || payload.title)) {
    return normalizeSeries(payload)
  }

  return null
}

/**
 * 获取全量文章分类列表 (/api/categories)
 */
export async function getCategories(): Promise<Category[]> {
  const url = buildUrl('/categories')
  const raw = await apiFetch<{ data?: RawCategoryData[] } | RawCategoryData[]>(url, {
    next: { revalidate: DEFAULT_REVALIDATE },
  })

  const list = Array.isArray(raw)
    ? raw
    : (raw && Array.isArray((raw as { data?: RawCategoryData[] }).data)
      ? (raw as { data: RawCategoryData[] }).data
      : [])

  if (Array.isArray(raw) || (raw && Array.isArray(raw.data))) {
    return list.map((c) => ({
      key: String(c.id),
      label: c.name,
      icon: c.icon || 'Sparkles',
      count: c.articleCount || 0,
    }))
  }

  throw new Error('分类列表响应格式不正确')
}

async function fetchAllPages<T>(loadPage: (page: number) => Promise<{ items: T[]; total: number }>): Promise<T[]> {
  const items: T[] = []
  for (let page = 1; ; page++) {
    const result = await loadPage(page)
    items.push(...result.items)
    if (items.length >= result.total || result.items.length === 0) return items
  }
}

/**
 * 获取所有文章 ID（用于 generateStaticParams）
 */
export async function getAllArticleIds(): Promise<string[]> {
  return (await getAllArticlesForSitemap()).map(article => article.id)
}

/**
 * 获取用于 Sitemap 的文章条目列表（包含 id 与真实更新日期）
 */
export async function getAllArticlesForSitemap(): Promise<{ id: string; updatedAt: string }[]> {
  const items = await fetchAllPages(page => getArticles({ page, pageSize: 100 }))
  return items.map(article => ({
    id: article.id,
    updatedAt: article.updatedAt || article.createdAt,
  }))
}

/**
 * 获取所有系列 ID（用于 generateStaticParams）
 */
export async function getAllSeriesIds(): Promise<string[]> {
  return (await getAllSeriesForSitemap()).map(series => series.id)
}

/**
 * 获取用于 Sitemap 的系列条目列表（包含 id 与真实更新日期）
 */
export async function getAllSeriesForSitemap(): Promise<{ id: string; updatedAt: string }[]> {
  const items = await fetchAllPages(page => getSeriesList({ page, pageSize: 100 }))
  return items.map(series => ({
    id: series.id,
    updatedAt: series.updatedAt,
  }))
}

/**
 * 获取当前访客的持久化匿名 ID（用于点赞防刷与去重）
 */
export function getVisitorId(): string {
  if (typeof window === 'undefined') return ''
  try {
    let vid = localStorage.getItem('art_visitor_vid')
    if (!vid) {
      vid = 'v_' + Math.random().toString(36).substring(2, 10) + Date.now().toString(36)
      localStorage.setItem('art_visitor_vid', vid)
    }
    return vid
  } catch {
    return ''
  }
}

/**
 * 访问量上报 (/api/stats/visit)
 */
export async function recordSystemVisit(): Promise<void> {
  await apiFetch(buildUrl('/stats/visit'), { method: 'POST', cache: 'no-store' })
}

/**
 * 文章阅读量上报 (/api/stats/articles/:id/view)
 */
export async function recordArticleView(id: string | number): Promise<void> {
  await apiFetch(buildUrl(`/stats/articles/${id}/view`), { method: 'POST', cache: 'no-store' })
}

/**
 * 文章点赞上报 (/api/stats/articles/:id/like)
 */
export async function likeArticle(id: string | number): Promise<void> {
  const visitorId = getVisitorId()
  await apiFetch(buildUrl(`/stats/articles/${id}/like`), {
    method: 'POST',
    cache: 'no-store',
    headers: visitorId ? { 'X-Visitor-Id': visitorId } : undefined,
  })
}

/**
 * 文章取消点赞 (/api/stats/articles/:id/like)
 */
export async function unlikeArticle(id: string | number): Promise<void> {
  const visitorId = getVisitorId()
  await apiFetch(buildUrl(`/stats/articles/${id}/like`), {
    method: 'DELETE',
    cache: 'no-store',
    headers: visitorId ? { 'X-Visitor-Id': visitorId } : undefined,
  })
}

/**
 * 获取系统总访问量 (/api/stats/visits)
 */
export async function getSystemVisits(): Promise<string> {
  const url = buildUrl('/stats/visits')
  const raw = await apiFetch<{ data?: { visits?: string | number }; visits?: string | number }>(url, {
    next: { revalidate: 300 }, // 5 分钟缓存
  })

  const payload = raw?.data || raw
  if (payload?.visits !== undefined && /^\d+$/.test(String(payload.visits))) {
    return String(payload.visits)
  }
  throw new Error('访问量响应格式不正确')
}
