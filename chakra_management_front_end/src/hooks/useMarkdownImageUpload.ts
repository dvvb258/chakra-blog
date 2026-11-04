import { useRef, useMemo } from 'react'
import { commands, type ICommand, type TextAreaTextApi } from '@uiw/react-md-editor'
import { uploadMedia } from '@/api/media'

interface MessageApi {
  loading: (content: string, duration?: number) => () => void
  success: (content: string) => void
  error: (content: string) => void
}

interface UseMarkdownImageUploadOptions {
  content: string
  setContent: React.Dispatch<React.SetStateAction<string>>
  message: MessageApi
}

/**
 * 针对 MDEditor 的多维图片上传与精准插入 Hook
 * 支持：
 * 1. 剪贴板截图粘贴（Ctrl+V）并于光标处精准插入
 * 2. 外部图片文件直接拖拽丢入编辑器（Drag & Drop）
 * 3. MDEditor 工具栏“图片”图标点击唤起本地文件选择器上传
 */
export function useMarkdownImageUpload({
  setContent,
  message,
}: UseMarkdownImageUploadOptions) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const textApiRef = useRef<TextAreaTextApi | null>(null)

  // 辅助方法：在光标所在位置精准插入文本（从当前 DOM 实时内容计算，防止异步上传期间丢字）
  const insertTextAtCursor = (
    textToInsert: string,
    targetTextarea?: HTMLTextAreaElement | null
  ) => {
    if (targetTextarea && typeof targetTextarea.selectionStart === 'number') {
      const start = targetTextarea.selectionStart
      const end = targetTextarea.selectionEnd
      const curVal = targetTextarea.value ?? ''
      const before = curVal.substring(0, start)
      const after = curVal.substring(end)
      const newContent = `${before}${textToInsert}${after}`
      setContent(newContent)

      // 异步恢复光标位置
      requestAnimationFrame(() => {
        targetTextarea.selectionStart = targetTextarea.selectionEnd = start + textToInsert.length
        targetTextarea.focus()
      })
    } else if (textApiRef.current) {
      textApiRef.current.replaceSelection(textToInsert)
    } else {
      setContent((prev) => (prev ? `${prev}\n\n${textToInsert}` : textToInsert))
    }
  }

  // 1. 剪贴板截图粘贴事件处理
  const handlePaste = async (e: React.ClipboardEvent) => {
    const items = e.clipboardData?.items
    if (!items) return

    for (let i = 0; i < items.length; i++) {
      const item = items[i]
      if (item.type.startsWith('image/')) {
        const file = item.getAsFile()
        if (!file) continue

        e.preventDefault()
        const target = e.target as HTMLTextAreaElement | null
        const hide = message.loading('正在上传截图至云存储...', 0)

        try {
          const res = await uploadMedia(file)
          const markdown = `\n![${file.name || 'image'}](${res.url})\n`
          insertTextAtCursor(markdown, target)
          message.success('截图已成功上传并插入正文')
        } catch (err) {
          message.error(err instanceof Error ? err.message : '图片上传失败')
        } finally {
          hide()
        }
        break
      }
    }
  }

  // 2. 拖拽文件进入编辑区域处理
  const handleDrop = async (e: React.DragEvent) => {
    const files = Array.from(e.dataTransfer?.files || []).filter((f) =>
      f.type.startsWith('image/')
    )
    if (files.length === 0) return

    e.preventDefault()
    const target = e.target as HTMLTextAreaElement | null
    const hide = message.loading(`正在上传 ${files.length} 张图片...`, 0)

    try {
      const results = await Promise.all(files.map((file) => uploadMedia(file)))
      const markdown = `\n${results
        .map((res, index) => `![${files[index].name || 'image'}](${res.url})`)
        .join('\n\n')}\n`

      insertTextAtCursor(markdown, target)
      message.success(`${files.length} 张图片已成功上传并插入正文`)
    } catch (err) {
      message.error(err instanceof Error ? err.message : '图片上传失败')
    } finally {
      hide()
    }
  }

  const handleDragOver = (e: React.DragEvent) => {
    if (e.dataTransfer?.types?.includes('Files')) {
      e.preventDefault()
      e.dataTransfer.dropEffect = 'copy'
    }
  }

  // 3. 工具栏文件选择器选中图片上传
  const handleToolbarFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || [])
    if (files.length === 0) return

    const hide = message.loading(`正在上传 ${files.length} 张图片...`, 0)
    try {
      const results = await Promise.all(files.map((file) => uploadMedia(file)))
      const markdown = `\n${results
        .map((res, index) => `![${files[index].name || 'image'}](${res.url})`)
        .join('\n\n')}\n`

      insertTextAtCursor(markdown)
      message.success('图片上传成功并已插入正文')
    } catch (err) {
      message.error(err instanceof Error ? err.message : '图片上传失败')
    } finally {
      hide()
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // 自定义 MDEditor 工具栏的 image 命令
  const customImageCommand: ICommand = useMemo(() => {
    return {
      ...commands.image,
      buttonProps: {
        'aria-label': '上传图片并插入正文',
        title: '上传图片并插入正文',
      },
      execute: (_state, api) => {
        textApiRef.current = api
        fileInputRef.current?.click()
      },
    }
  }, [])

  // 替换 MDEditor 的默认 image 命令
  const editorCommands = useMemo(() => {
    return commands.getCommands().map((cmd) => {
      if (cmd && cmd.name === 'image') {
        return customImageCommand
      }
      return cmd
    })
  }, [customImageCommand])

  return {
    fileInputRef,
    handlePaste,
    handleDrop,
    handleDragOver,
    handleToolbarFileSelect,
    editorCommands,
  }
}
