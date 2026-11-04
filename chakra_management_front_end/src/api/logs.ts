import client from './client'
import type { PagedResult } from '@/types'

export interface AuditLogDto {
  id: string | number
  userId?: string | number | null
  username?: string | null
  module: string
  action: string
  method: string
  path: string
  parameters?: string | null
  clientIp?: string | null
  durationMs: number
  isSuccess: boolean
  errorMessage?: string | null
  traceId?: string | null
  createdAt: string
}

export interface ExceptionLogDto {
  id: string | number
  exceptionType: string
  message: string
  stackTrace?: string | null
  path: string
  method: string
  queryString?: string | null
  requestBody?: string | null
  clientIp?: string | null
  userId?: string | number | null
  username?: string | null
  traceId?: string | null
  createdAt: string
}

export interface GetAuditLogsParams {
  module?: string
  username?: string
  isSuccess?: boolean
  traceId?: string
  startDate?: string
  endDate?: string
  pageIndex?: number
  pageSize?: number
}

export interface GetExceptionLogsParams {
  keyword?: string
  traceId?: string
  startDate?: string
  endDate?: string
  pageIndex?: number
  pageSize?: number
}

/**
 * 分页查询操作审计日志
 */
export function getAuditLogs(params?: GetAuditLogsParams): Promise<PagedResult<AuditLogDto>> {
  return client.get('/admin/logs/audit', { params })
}

/**
 * 分页查询系统异常日志
 */
export function getExceptionLogs(params?: GetExceptionLogsParams): Promise<PagedResult<ExceptionLogDto>> {
  return client.get('/admin/logs/exceptions', { params })
}

/**
 * 获取指定异常日志详情（含完整堆栈追踪）
 */
export function getExceptionDetail(id: string | number): Promise<ExceptionLogDto> {
  return client.get(`/admin/logs/exceptions/${id}`)
}
