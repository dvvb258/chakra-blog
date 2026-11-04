import { useAuthStore } from '@/stores/authStore'

/**
 * 细粒度权限判定 Hook
 */
export function usePermission() {
  const user = useAuthStore((state) => state.user)

  /**
   * 判断当前登录人是否拥有指定的系统权限代码
   * 超级管理员 (admin) 拥有天然全量通配特权（Bypass 模式）
   * @param code 权限代码（例如 'article:manage'）
   */
  const hasPermission = (code: string): boolean => {
    if (!user) return false
    // 超级管理员天然放行
    if (user.role?.toLowerCase() === 'admin') return true
    if (!user.permissions || !Array.isArray(user.permissions)) return false
    return user.permissions.includes(code)
  }

  /**
   * 判断是否拥有任一指定权限
   */
  const hasAnyPermission = (codes: string[]): boolean => {
    if (!user) return false
    if (user.role?.toLowerCase() === 'admin') return true
    if (!user.permissions || !Array.isArray(user.permissions)) return false
    return codes.some((code) => user.permissions?.includes(code))
  }

  /**
   * 是否为超级管理员
   */
  const isAdmin = user?.role?.toLowerCase() === 'admin'

  return {
    hasPermission,
    hasAnyPermission,
    isAdmin,
    permissions: user?.permissions ?? [],
  }
}
