import client from './client'
import type { MenuDto, CreateMenuRequest, UpdateMenuRequest, PermissionMetadata } from '@/types'

/**
 * 获取后端权威注册的全量系统权限元数据字典（按模块分组）
 */
export function getPermissionsMetadata(): Promise<PermissionMetadata[]> {
  return client.get('/admin/menus/permissions-metadata')
}

/**
 * 获取全量系统菜单与权限列表
 */
export function getMenus(): Promise<MenuDto[]> {
  return client.get('/admin/menus')
}

/**
 * 创建新菜单项
 */
export function createMenu(payload: CreateMenuRequest): Promise<MenuDto> {
  return client.post('/admin/menus', payload)
}

/**
 * 更新菜单项
 */
export function updateMenu(id: string | number, payload: UpdateMenuRequest): Promise<MenuDto> {
  return client.put(`/admin/menus/${id}`, payload)
}

/**
 * 删除菜单项
 */
export function deleteMenu(id: string | number): Promise<string> {
  return client.delete(`/admin/menus/${id}`)
}
