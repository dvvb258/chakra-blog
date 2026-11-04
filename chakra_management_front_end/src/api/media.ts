import client from './client'

/**
 * 上传媒体结果响应
 */
export interface UploadMediaResult {
  url: string
  fileName: string
  size: number
}

/**
 * 上传图片文件至云端对象存储
 * @param file 本地文件对象
 */
export function uploadMedia(file: File): Promise<UploadMediaResult> {
  const formData = new FormData()
  formData.append('file', file)

  return client.post('/admin/media/upload', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  })
}
