import client from './client'
import type { PublicSystemConfigDto } from '@/types'

/**
 * 获取系统公共特性探测配置（公开端点，无需登录）
 */
export function getPublicSystemConfig(): Promise<PublicSystemConfigDto> {
  return client.get('/system/public-config')
}
