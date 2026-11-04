import client from './client'
import type {
  PagedResult,
  SeriesListItemDto,
  SeriesDetailDto,
  CreateSeriesRequest,
  UpdateSeriesRequest,
} from '@/types'

export interface GetSeriesListParams {
  keyword?: string
  author?: string
  pageIndex?: number
  pageSize?: number
}

/**
 * 分页获取专栏系列列表
 */
export function getSeriesList(params?: GetSeriesListParams): Promise<PagedResult<SeriesListItemDto>> {
  return client.get('/admin/series', { params })
}

/**
 * 获取指定专栏系列详情（含归属文章列表）
 */
export function getSeriesDetail(id: string | number): Promise<SeriesDetailDto> {
  return client.get(`/admin/series/${id}`)
}

/**
 * 创建新专栏系列
 */
export function createSeries(data: CreateSeriesRequest): Promise<string | number> {
  return client.post('/admin/series', data)
}

/**
 * 更新指定专栏系列
 */
export function updateSeries(id: string | number, data: UpdateSeriesRequest): Promise<string> {
  return client.put(`/admin/series/${id}`, data)
}

/**
 * 软删除指定专栏系列
 */
export function deleteSeries(id: string | number): Promise<string> {
  return client.delete(`/admin/series/${id}`)
}
