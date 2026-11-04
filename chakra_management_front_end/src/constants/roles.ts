/**
 * 系统标准角色编码定义（严格与后端 SharedKernel.Constants.RoleNames 对齐）
 */
export const RoleCodes = {
  Admin: 'Admin',
  Editor: 'Editor',
} as const

export type RoleCodeType = typeof RoleCodes[keyof typeof RoleCodes]

/**
 * 角色显示信息映射
 */
export const ROLE_DISPLAY_MAP: Record<string, { label: string; color: string }> = {
  [RoleCodes.Admin]: { label: '超级管理员', color: 'geekblue' },
  [RoleCodes.Editor]: { label: '内容编辑者', color: 'cyan' },
}

/**
 * 角色选择下拉项
 */
export const ROLE_OPTIONS = [
  { value: RoleCodes.Admin, label: '超级管理员 (Admin)' },
  { value: RoleCodes.Editor, label: '内容编辑者 (Editor)' },
]
