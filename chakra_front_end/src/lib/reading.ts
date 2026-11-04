/**
 * 文章预计阅读时长通用计算与格式化工具
 *
 * 遵循原则：通用基础功能保持专业严谨的技术规范词，与具体视觉主题彻底解耦。
 */

export interface ReadingTimeInfo {
  minutes: number
  timeText: string
  rank: string
  colorClass: string
  bgClass: string
}

/**
 * 格式化文章预计阅读时长（标准通用技术功能）
 * @param minutes 后端或前台计算的阅读分钟数
 * @returns 格式化字符串，例如："5 分钟阅读"
 */
export function formatReadingTime(minutes?: number | null): string {
  const m = Math.max(1, minutes || 3)
  return `${m} 分钟阅读`
}

/**
 * 获取文章阅读时长元数据与主题通用标签样式
 */
export function getReadingTimeInfo(minutes?: number | null): ReadingTimeInfo {
  const m = Math.max(1, minutes || 3)
  const timeText = `${m} 分钟阅读`

  return {
    minutes: m,
    timeText,
    rank: timeText,
    colorClass: 'text-theme-primary',
    bgClass: 'bg-theme-surface-hover border-theme-border/70',
  }
}
