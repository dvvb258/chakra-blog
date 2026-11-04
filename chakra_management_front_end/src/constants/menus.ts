/**
 * 系统后台导航菜单统一定义（严格按照显示与访问优先级顺序排列）
 */
export interface MenuItemConfig {
  key: string
  label: string
}

export const SYSTEM_MENU_ORDER: MenuItemConfig[] = [
  { key: '/', label: '仪表盘' },
  { key: '/articles', label: '文章管理' },
  { key: '/categories', label: '分类管理' },
  { key: '/series', label: '专栏管理' },
  { key: '/logs', label: '系统日志' },
  { key: '/roles', label: '角色权限' },
  { key: '/menus', label: '菜单管理' },
  { key: '/users', label: '个人中心' },
]
