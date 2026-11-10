import client from './client'
import type {
  PagedResult,
  ArticleListItemDto,
  ArticleDto,
  CreateArticleRequest,
  UpdateArticleRequest,
  SeriesListItemDto,
} from '@/types'

/**
 * 获取管理端文章分页列表
 */
export function getArticles(params: {
  pageIndex?: number
  pageSize?: number
  keyword?: string
  categoryId?: string | number
  tag?: string
  seriesId?: string | number
  status?: number
  isPinned?: boolean
}): Promise<PagedResult<ArticleListItemDto>> {
  return client.get('/admin/articles', { params })
}

/**
 * 获取单篇文章详情（包含 Markdown 正文，实时无缓存）
 */
export function getArticle(id: string | number): Promise<ArticleDto> {
  return client.get(`/admin/articles/${id}`)
}

/**
 * 发布新文章
 */
export function createArticle(data: CreateArticleRequest): Promise<string | number> {
  return client.post('/admin/articles', data)
}

/**
 * 更新指定文章
 */
export function updateArticle(
  id: string | number,
  data: UpdateArticleRequest
): Promise<string> {
  return client.put(`/admin/articles/${id}`, data)
}

/**
 * 软删除指定文章
 */
export function deleteArticle(id: string | number): Promise<string> {
  return client.delete(`/admin/articles/${id}`)
}

/**
 * 提交文章管理员审核（编辑者或管理员）
 */
export function submitArticleReview(id: string | number): Promise<string> {
  return client.put(`/admin/articles/${id}/submit-review`)
}

/**
 * 审核通过并发布文章（仅限管理员）
 */
export function approveArticle(id: string | number): Promise<string> {
  return client.put(`/admin/articles/${id}/approve`)
}

/**
 * 驳回文章审核退回草稿（仅限管理员）
 */
export function rejectArticle(id: string | number): Promise<string> {
  return client.put(`/admin/articles/${id}/reject`)
}

/**
 * 切换文章置顶状态（仅限管理员）
 */
export function toggleArticlePin(id: string | number, isPinned: boolean): Promise<string> {
  return client.put(`/admin/articles/${id}/pin`, null, { params: { isPinned } })
}

/**
 * 获取专栏/系列列表（调用公共公开接口，用于下拉选择归属，任何角色均可获取）
 */
export async function getSeriesList(params?: { pageSize?: number }): Promise<SeriesListItemDto[]> {
  const res = await client.get<unknown, PagedResult<SeriesListItemDto> | SeriesListItemDto[]>('/series', {
    params: { pageSize: 100, _t: Date.now(), ...params },
  })
  if (Array.isArray(res)) return res
  if (res && Array.isArray((res as PagedResult<SeriesListItemDto>).items)) {
    return (res as PagedResult<SeriesListItemDto>).items
  }
  return []
}

/**
 * 获取全站热门标签列表（公开接口）
 */
export function getTags(limit: number = 50): Promise<string[]> {
  return client.get('/tags', { params: { limit } })
}
