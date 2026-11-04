import React from 'react'
import { usePermission } from '@/hooks/usePermission'

export interface PermissionGuardProps {
  /** 所需权限代号 */
  code?: string
  /** 候选权限，与 code 合并后满足其一即可 */
  anyCodes?: string[]
  /** 无权限时的替补渲染内容，默认 null（完全隐藏） */
  fallback?: React.ReactNode
  children: React.ReactNode
}

/**
 * 声明式细粒度权限守卫组件
 */
export const PermissionGuard: React.FC<PermissionGuardProps> = ({
  code,
  anyCodes,
  fallback = null,
  children,
}) => {
  const { hasAnyPermission } = usePermission()
  const requiredCodes = [...(code ? [code] : []), ...(anyCodes ?? [])]

  if (requiredCodes.length > 0 && !hasAnyPermission(requiredCodes)) {
    return <>{fallback}</>
  }

  return <>{children}</>
}

export default PermissionGuard
