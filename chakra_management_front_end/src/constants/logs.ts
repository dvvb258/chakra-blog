/**
 * 审计日志业务模块定义（与后端 SharedKernel.Constants.AuditModules 严格对齐）
 */
export const AuditModules = {
  Article: '文章管理',
  Series: '专栏管理',
  Role: '权限管理',
  User: '用户管理',
  Profile: '个人中心',
  Auth: '身份认证',
} as const

export type AuditModuleType = typeof AuditModules[keyof typeof AuditModules]

/**
 * 审计日志模块色彩映射表
 */
export const AUDIT_MODULE_COLORS: Record<string, string> = {
  [AuditModules.Article]: 'blue',
  [AuditModules.Series]: 'purple',
  [AuditModules.Role]: 'geekblue',
  [AuditModules.User]: 'cyan',
  [AuditModules.Profile]: 'green',
  [AuditModules.Auth]: 'orange',
}

/**
 * 审计日志模块筛选下拉项（包含全量 6 大业务模块）
 */
export const AUDIT_MODULE_OPTIONS = [
  { label: '全部模块', value: '' },
  { label: AuditModules.Article, value: AuditModules.Article },
  { label: AuditModules.Series, value: AuditModules.Series },
  { label: AuditModules.Role, value: AuditModules.Role },
  { label: AuditModules.User, value: AuditModules.User },
  { label: AuditModules.Profile, value: AuditModules.Profile },
  { label: AuditModules.Auth, value: AuditModules.Auth },
]
