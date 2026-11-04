import client from './client'
import type { PagedResult, UserListItemDto, RoleDto } from '@/types'

/**
 * 分页获取系统用户列表（仅限超级管理员）
 */
export function getUsers(params?: {
  pageIndex?: number
  pageSize?: number
  keyword?: string
  role?: string
  isActive?: boolean
}): Promise<PagedResult<UserListItemDto>> {
  return client.get('/admin/users', { params })
}

/**
 * 修改指定用户的角色（如 Editor -> Admin，仅限管理员）
 */
export function updateUserRole(userId: string | number, role: string): Promise<string> {
  return client.put(`/admin/users/${userId}/role`, { role })
}

/**
 * 获取系统可用角色列表
 */
export function getRoles(): Promise<RoleDto[]> {
  return client.get('/admin/users/roles')
}
