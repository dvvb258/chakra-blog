/**
 * Markdown 工具函数（可在服务端和客户端使用）
 */

/**
 * 剥除 Markdown 行内格式标记，提取出纯文本
 */
export function stripMarkdown(text: string): string {
  if (!text) return ''
  return text
    .replace(/`([^`]+)`/g, '$1') // 行内代码 `code`
    .replace(/\*\*\*([^*]+)\*\*\*/g, '$1') // 粗斜体 ***text***
    .replace(/\*\*([^*]+)\*\*/g, '$1') // 粗体 **text**
    .replace(/\*([^*]+)\*/g, '$1') // 斜体 *text*
    .replace(/___([^_]+)___/g, '$1')
    .replace(/__([^_]+)__/g, '$1')
    .replace(/_([^_]+)_/g, '$1')
    .replace(/~~([^~]+)~~/g, '$1') // 删除线 ~~text~~
    .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1') // 链接 [text](url)
    .replace(/<[^>]+>/g, '') // 常见 HTML 标签
    .trim()
}

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
    const rawText = match[2].trim()
    const text = stripMarkdown(rawText)
    const id = slugify(text)
    items.push({ id, text, level })
  }
  return items
}

export const extractHeadings = extractToc
