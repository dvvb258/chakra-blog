import { Navigate } from 'react-router-dom'
import { Spin } from 'antd'
import { useAuthStore } from '@/stores/authStore'
import { getFirstAuthorizedRoute } from '@/utils/permission'
import { RoleCodes } from '@/constants/roles'

interface AuthGuardProps {
  children: React.ReactNode
  requireAdmin?: boolean
  path?: string
}

export default function AuthGuard({ children, requireAdmin = false, path }: AuthGuardProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const initialized = useAuthStore((s) => s.initialized)
  const user = useAuthStore((s) => s.user)

  if (!initialized) {
    return <Spin style={{ display: 'flex', justifyContent: 'center', marginTop: '20vh' }} />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  const fallbackRoute = getFirstAuthorizedRoute(user?.menus, user?.role === RoleCodes.Admin)

  // 1. 若明确要求管理员权限且当前用户并非 Admin，自动重定向到其可访问的首个页面
  if (requireAdmin && user?.role !== RoleCodes.Admin) {
    return <Navigate to={fallbackRoute} replace />
  }

  // 2. 动态菜单路径权限核验：非 Admin 用户核查是否被授权访问该路由
  if (path && user?.role !== RoleCodes.Admin) {
    const hasPermission = user?.menus?.includes(path)
    if (!hasPermission) {
      return <Navigate to={fallbackRoute} replace />
    }
  }

  return <>{children}</>
}
