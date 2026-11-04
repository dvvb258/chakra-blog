/**
 * 系统权限资源标识代号常量（对应后端 Permissions 常量与 menus 表 permission_code）
 * 包含两层模型：
 * 1. 一级菜单/路由容器标识（用于导航树结构组织）
 * 2. 细粒度业务操作权限（用于 PermissionGuard 按钮守卫与接口校验）
 */
export const PERMISSIONS = {
  // === 一级菜单与路由容器标识 ===
  ARTICLE_MANAGE: 'article:manage',
  CATEGORY_MANAGE: 'category:manage',
  SERIES_MANAGE: 'series:manage',
  LOG_VIEW: 'log:view',
  ROLE_MANAGE: 'role:manage',
  MENU_MANAGE: 'menu:manage',
  USER_MANAGE: 'user:manage',

  // === 细粒度业务操作权限 ===
  // 仪表盘
  DASHBOARD_VIEW: 'dashboard:view',

  // 文章管理
  ARTICLE_VIEW: 'content:article:view',
  ARTICLE_CREATE: 'content:article:create',
  ARTICLE_EDIT: 'content:article:edit',
  ARTICLE_DELETE: 'content:article:delete',
  ARTICLE_APPROVE: 'content:article:approve',
  ARTICLE_SUBMIT: 'content:article:submit',

  // 分类管理
  CATEGORY_VIEW: 'content:category:view',
  CATEGORY_CREATE: 'content:category:create',
  CATEGORY_EDIT: 'content:category:edit',
  CATEGORY_DELETE: 'content:category:delete',

  // 专栏管理
  SERIES_VIEW: 'content:series:view',
  SERIES_CREATE: 'content:series:create',
  SERIES_EDIT: 'content:series:edit',
  SERIES_DELETE: 'content:series:delete',

  // 系统日志
  LOG_AUDIT_VIEW: 'system:log:audit:view',
  LOG_EXCEPTION_VIEW: 'system:log:exception:view',

  // 角色权限
  ROLE_VIEW: 'system:role:view',
  ROLE_CREATE: 'system:role:create',
  ROLE_EDIT: 'system:role:edit',
  ROLE_DELETE: 'system:role:delete',

  // 菜单管理
  MENU_VIEW: 'system:menu:view',
  MENU_EDIT: 'system:menu:edit',

  // 用户管理
  USER_VIEW: 'system:user:view',
  USER_ROLE_UPDATE: 'system:user:role:update',
} as const

export type PermissionCode = (typeof PERMISSIONS)[keyof typeof PERMISSIONS]
