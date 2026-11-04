import client from './client'
import type { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '@/types'

/**
 * 获取全量文章分类列表（调用公共公开接口，包含文章计数，任何角色及未登录均可调用）
 */
export function getCategories(): Promise<CategoryDto[]> {
  return client.get('/categories')
}

/**
 * 管理端创建文章分类
 */
export function createCategory(data: CreateCategoryRequest): Promise<string | number> {
  return client.post('/admin/categories', data)
}

/**
 * 管理端更新文章分类
 */
export function updateCategory(
  id: string | number,
  data: UpdateCategoryRequest
): Promise<string> {
  return client.put(`/admin/categories/${id}`, data)
}

/**
 * 管理端软删除文章分类
 */
export function deleteCategory(id: string | number): Promise<string> {
  return client.delete(`/admin/categories/${id}`)
}
