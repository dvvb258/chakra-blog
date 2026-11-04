import { ArticleStatus, type ArticleStatusType } from '@/types'

/**
 * 系统预设主题调色板（用于动态分类徽章）
 */
const PRESET_CATEGORY_COLORS = [
  '#0284c7', // 经典天蓝
  '#8b5cf6', // 优雅智紫
  '#10b981', // 清新翠绿
  '#f59e0b', // 暖阳琥珀
  '#06b6d4', // 极客青碧
  '#ec4899', // 霓虹玫粉
  '#f97316', // 活力橙晶
  '#6366f1', // 经典靛蓝
]

/**
 * 根据分类标识计算唯一稳定的主题色
 */
export function getCategoryColor(key?: string): string {
  if (!key) return '#64748b'
  let hash = 0
  for (let i = 0; i < key.length; i++) {
    hash = (hash << 5) - hash + key.charCodeAt(i)
    hash |= 0
  }
  const index = Math.abs(hash) % PRESET_CATEGORY_COLORS.length
  return PRESET_CATEGORY_COLORS[index]
}

/**
 * 常用热门技术标签
 */
export const POPULAR_TAGS = [
  'C#',
  '.NET 10',
  'ASP.NET Core',
  'AI Agent',
  'Next.js',
  'EF Core',
  'PostgreSQL',
  'Redis',
  'Hangfire',
  'Dapper',
]

/**
 * 封面强调色选项
 */
export const COLOR_OPTIONS = [
  { label: '经典天蓝 (#0284c7)', value: '#0284c7' },
  { label: '优雅智紫 (#8b5cf6)', value: '#8b5cf6' },
  { label: '清新翠绿 (#10b981)', value: '#10b981' },
  { label: '暖阳琥珀 (#f59e0b)', value: '#f59e0b' },
  { label: '热烈绯红 (#ef4444)', value: '#ef4444' },
]

/**
 * 文章审核状态映射字典（使用 ArticleStatus 枚举键）
 */
export const ARTICLE_STATUS_MAP: Record<ArticleStatusType, { label: string; color: string }> = {
  [ArticleStatus.Draft]: { label: '草稿', color: 'default' },
  [ArticleStatus.Published]: { label: '已发布', color: 'success' },
  [ArticleStatus.PendingReview]: { label: '待审核', color: 'warning' },
  [ArticleStatus.Rejected]: { label: '已退回', color: 'error' },
}

/**
 * 文章审核状态下拉选项
 */
export const ARTICLE_STATUS_OPTIONS = [
  { value: ArticleStatus.Published, label: '已发布' },
  { value: ArticleStatus.PendingReview, label: '待审核' },
  { value: ArticleStatus.Draft, label: '草稿' },
  { value: ArticleStatus.Rejected, label: '已退回' },
]
