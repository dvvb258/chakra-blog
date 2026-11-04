/**
 * Markdown 工具函数（可在服务端和客户端使用）
 */

export function slugify(text: string): string {
  return String(text)
    .toLowerCase()
    .replace(/[^\w\u4e00-\u9fa5]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

export interface TocItem {
  id: string
  text: string
  level: number
}

export function extractToc(markdown: string): TocItem[] {
  if (!markdown) return []
  const headingRegex = /^(#{2,3})\s+(.+)$/gm
  const items: TocItem[] = []
  let match: RegExpExecArray | null

  while ((match = headingRegex.exec(markdown)) !== null) {
    const level = match[1].length
    const text = match[2].trim()
    const id = slugify(text)
    items.push({ id, text, level })
  }
  return items
}

export const extractHeadings = extractToc

