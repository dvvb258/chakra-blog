'use client'

import React from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import CodeBlock from '@/components/articles/CodeBlock'
import { slugify } from '@/lib/markdown'

interface MarkdownRendererProps {
  content: string
}

/**
 * 递归提取 React 节点树中的纯文本字符串，避免遇到富文本/行内代码时求值为 "[object Object]"
 */
function getNodeText(node: React.ReactNode): string {
  if (node == null || typeof node === 'boolean') {
    return ''
  }
  if (typeof node === 'string' || typeof node === 'number') {
    return String(node)
  }
  if (Array.isArray(node)) {
    return node.map(getNodeText).join('')
  }
  if (React.isValidElement(node)) {
    const props = node.props as { children?: React.ReactNode }
    return getNodeText(props.children)
  }
  return ''
}

export default function MarkdownRenderer({ content }: MarkdownRendererProps) {
  return (
    <div className="prose-article">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          h2: ({ children }) => {
            const text = getNodeText(children)
            const id = slugify(text)
            return (
              <h2 id={id} className="scroll-mt-24 group">
                <a href={`#${id}`} className="no-underline hover:text-theme-primary transition-colors">
                  {children}
                </a>
              </h2>
            )
          },
          h3: ({ children }) => {
            const text = getNodeText(children)
            const id = slugify(text)
            return (
              <h3 id={id} className="scroll-mt-24">
                <a href={`#${id}`} className="no-underline hover:text-theme-primary transition-colors">
                  {children}
                </a>
              </h3>
            )
          },
          // 消除 ReactMarkdown 默认的外层 <pre> 包裹，避免与 CodeBlock 内部的 <pre> 产生非法嵌套
          pre: ({ children }) => <>{children}</>,
          code: ({ className, children, ...props }: React.HTMLAttributes<HTMLElement>) => {
            const match = /language-(\w+)/.exec(className || '')
            const codeText = String(children).replace(/\n$/, '')
            const isInline = !match && !codeText.includes('\n')

            if (!isInline) {
              return <CodeBlock language={match ? match[1] : 'text'} code={codeText} />
            }

            return (
              <code className={className} {...props}>
                {children}
              </code>
            )
          },
          table: ({ children }) => (
            <div className="overflow-x-auto my-6 rounded-xl border border-theme-border/60 shadow-xs">
              <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700 text-sm">
                {children}
              </table>
            </div>
          ),
          a: ({ href, children, ...props }) => {
            const isExternal = href?.startsWith('http')
            return (
              <a
                href={href}
                target={isExternal ? '_blank' : undefined}
                rel={isExternal ? 'noopener noreferrer' : undefined}
                className="text-theme-primary hover:text-theme-secondary underline underline-offset-4 decoration-theme-primary/40 font-semibold"
                {...props}
              >
                {children}
              </a>
            )
          },
        }}
      >
        {content}
      </ReactMarkdown>
    </div>
  )
}
