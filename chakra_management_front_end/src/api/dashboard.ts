import client from './client'

export interface CategoryStatDto {
  category: string
  count: number
}

export interface RecentArticleDto {
  id: string | number
  title: string
  category: string
  views: number
  author: string
  createdAt: string
}

export interface TrendItemDto {
  date: string
  articleCount: number
}

export interface DashboardSummaryDto {
  totalVisits: number
  totalArticles: number
  totalViews: number
  totalSeries: number
  categoryDistribution: CategoryStatDto[]
  recentArticles: RecentArticleDto[]
  trendData: TrendItemDto[]
}

/**
 * 获取仪表盘核心运营统计汇总数据
 */
export function getDashboardSummary(): Promise<DashboardSummaryDto> {
  return client.get('/admin/dashboard/summary')
}
