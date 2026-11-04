import { SYSTEM_MENU_ORDER } from '@/constants/menus'

/**
 * 获取当前登录用户在系统菜单序列中能够访问的【第一个菜单】
 * 绝不硬编码任何角色或路径，纯由菜单优先级顺序与实际授权权限动态计算
 * @param authorizedMenus 用户已授权的菜单路由列表
 * @param isAdmin 是否为超级管理员（超管天然拥有全量菜单特权）
 * @returns 目标菜单路由，若无任何菜单权限则兜底跳转登录页
 */
export function getFirstAuthorizedRoute(authorizedMenus?: string[], isAdmin = false): string {
  // 按照系统菜单优先级顺序，寻找该用户具备权限的第一个菜单
  const firstAvailableItem = SYSTEM_MENU_ORDER.find((item) => {
    if (isAdmin) {
      return true
    }
    return authorizedMenus?.includes(item.key)
  })

  return firstAvailableItem ? firstAvailableItem.key : '/login'
}
