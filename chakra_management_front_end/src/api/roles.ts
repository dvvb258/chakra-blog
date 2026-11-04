import client from './client'
import type { RoleDto, MenuDto, CreateRoleRequest, UpdateRoleRequest } from '@/types'

/**
 * 获取全部角色列表（含已授权菜单IDs）
 */
export function getRoles(): Promise<RoleDto[]> {
  return client.get('/admin/roles')
}

/**
 * 创建新自定义角色
 */
export function createRole(payload: CreateRoleRequest): Promise<RoleDto> {
  return client.post('/admin/roles', payload)
}

/**
 * 更新角色基本信息与菜单权限
 */
export function updateRole(id: string | number, payload: UpdateRoleRequest): Promise<RoleDto> {
  return client.put(`/admin/roles/${id}`, payload)
}

/**
 * 删除角色
 */
export function deleteRole(id: string | number): Promise<string> {
  return client.delete(`/admin/roles/${id}`)
}

/**
 * 获取系统全量菜单树列表（用于权限分配）
 */
export function getMenus(): Promise<MenuDto[]> {
  return client.get('/admin/menus')
}
