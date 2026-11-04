/**
 * 核心博客数据模型定义
 * 与 .NET 10 Web API 后端 DTO 及渲染呈现规范对齐
 */

export interface Article {
  id: string
  title: string
  summary: string
  content: string
  coverImage: string
  category: string
  categoryName: string
  tags: string[]
  author: string
  readingTime?: number
  readTime?: string
  views: number
  likes: number
  /** 置顶徽章状态（预留展示字段，支持后端 is_pinned / pinned 映射） */
  pinned?: boolean
  createdAt: string
  updatedAt?: string
  seriesId?: string | number | null
}

export interface Series {
  id: string
  title: string
  name?: string
  description: string
  coverImage: string
  articleCount: number
  updatedAt: string
  tags: string[]
  articles?: Article[]
}

export interface Category {
  key: string
  label: string
  icon: string
  count: number
}

export interface ArticleQueryParams {
  keyword?: string
  category?: string
  tag?: string
  page?: number
  pageSize?: number
}

export interface SeriesQueryParams {
  keyword?: string
  page?: number
  pageSize?: number
}

export interface PagedList<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}
