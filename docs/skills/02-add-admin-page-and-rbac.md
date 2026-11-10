# Skill: 管理后台页面与 RBAC 权限绑定指南

本技能指导 AI 编码 Agent 或研发人员在 `chakra_management_front_end` 中遵循 **React 19 + Vite 6 + Ant Design 5 + Zustand 5** 规范新增后台管理页面与接入 RBAC 权限体系。

---

## 1. 架构核心守则 (Non-Negotiables)

1. **雪花 ID 精度保护（严禁截断）**：
   - 64 位 ID 在 TypeScript 中必须声明为 `string` 或 `string | number`；
   - 严禁在 JavaScript / TypeScript 中使用 `Number(id)` 或 `parseInt(id)`，类型转换使用 `String(id)`。
2. **三级 RBAC 模型 (`menus.type`)**：
   - `1 - 目录 (Directory)`：侧边栏折叠分组（无物理页面，只有图标和排序）；
   - `2 - 菜单/页面 (Menu)`：对应路由 `path` 与视图组件，无权限时自动 403 / 重定向；
   - `3 - 按钮/操作权限 (Button)`：页面内按钮级操作，由 `<PermissionGuard code="...">` 守卫。
3. **网络与接口交互**：
   - 统一在 `src/api/` 中导出服务函数，调用 `src/api/client.ts`；
   - Axios 自动处理双 Token 静默刷新与单并发互斥续期锁；
   - 接口请求自动注入 `X-Trace-Id` 供全链路日志追溯。

---

## 2. 标准落地步骤 (SOP)

### 步骤 1：在 `src/types/index.ts` 声明 DTO 接口
```typescript
export interface FeedbackItemDto {
  id: string // 必须保持为 string，防止 64 位整型溢出
  articleId: string
  content: string
  createdAt: string
}
```

### 步骤 2：在 `src/api/` 中封装 API 请求
路径：`src/api/{feature}.ts`
```typescript
import client from './client'
import type { FeedbackItemDto, PagedResult } from '@/types'

export function getFeedbacks(params: { pageIndex?: number; pageSize?: number }): Promise<PagedResult<FeedbackItemDto>> {
  return client.get('/admin/feedbacks', { params })
}

export function deleteFeedback(id: string): Promise<void> {
  return client.delete(`/admin/feedbacks/${id}`)
}
```

### 步骤 3：在 `src/constants/permissions.ts` 声明权限代码
```typescript
export const PERMISSIONS = {
  // ...已有权限
  FEEDBACK_VIEW: 'feedback:view',
  FEEDBACK_DELETE: 'feedback:delete',
} as const
```

### 步骤 4：编写视图组件与权限守卫
路径：`src/pages/{Feature}List.tsx`
```tsx
import React, { useState, useEffect } from 'react'
import { Card, Table, Button, Space, Popconfirm } from 'antd'
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'
import * as feedbackApi from '@/api/feedback'

export default function FeedbackList() {
  // 页面业务逻辑与表格...
  return (
    <Card title="文章反馈管理">
      {/* 按钮级权限拦截 */}
      <PermissionGuard code={PERMISSIONS.FEEDBACK_DELETE}>
        <Button danger>删除反馈</Button>
      </PermissionGuard>
    </Card>
  )
}
```

### 步骤 5：在路由与菜单树中注册
- 页面路由注册于 `src/routes/index.tsx` 或管理端路由配置中；
- 数据库初始化脚本 `init.sql` 中的 `menus` 表同步插入对应的页面与按钮记录。
- 执行 `npm run build` 确保 TypeScript 类型检查和 Vite 构建零错误通过。
