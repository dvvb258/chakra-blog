# Skill: 展示前台页面开发与双模主题适配指南

本技能指导 AI 编码 Agent 或研发人员在 `chakra_front_end`（Next.js 15 App Router + React 19 + Tailwind CSS）中扩展页面与组件，并遵循**主题预设解耦规范**。

---

## 1. 架构核心守则 (Non-Negotiables)

1. **主题预设完全解耦 (`themePreset`)**：
   - 全站支持两种视觉与彩蛋模式：
     - `tech`：现代极简技术风（适合作为严肃或通用的开源技术博客，无动漫彩蛋，清爽蓝色调与科技卡片）；
     - `anime`：木叶火影特色风（写轮眼动态名片、三叉苦无飞雷神回到顶部、卡卡西 404 彩蛋等）。
   - **基础功能通用化**：文章列表、阅读耗时、分类、标签、翻页、搜索弹窗、代码复制等基础功能，**严禁使用任何特定主题名词**，一律使用标准通用技术词汇；
   - **特定视觉隔离**：特定视觉彩蛋（如写轮眼/苦无/动漫立绘）必须被包裹在 `siteConfig.themePreset === 'anime'` 条件判断中，并为 `tech` 模式提供清爽现代的替代实现。
2. **设计系统 Tokens**：
   - 颜色一律使用 Tailwind 语义类：`text-theme-primary`、`bg-theme-surface`、`border-theme-border`、`shadow-paper`；
   - 严禁在页面或组件内手写固定主题 Hex 色值，保证夜间与日间模式、极简与特色模式自适应切换。
3. **服务端网络直连与双通道机制**：
   - **服务端 (Server Component / SSR)**：直接在 `page.tsx` 中调用 `src/lib/api.ts`（Node.js 内部直连后端内网 `INTERNAL_API_BASE_URL`）；
   - **客户端 (Client Component)**：浏览器统一请求相对路径 `/api/*`，由 Next.js `rewrites()` 统一代理转发。
4. **配置优先原则 (`siteConfig`)**：
   - 站点可变信息（站名、标语、头像、SEO）来自数据库单行覆盖项（`getSiteConfig()` 读取，客户端通过 `window.__CHAKRA_SITE__` 读取同源配置）；
   - 禁止在组件中直接读取环境变量硬编码文案。

---

## 2. 标准落地步骤 (SOP)

### 步骤 1：在 `src/lib/api.ts` 声明请求与标准化转换
```typescript
import { apiFetch, normalizeArticle } from '@/lib/api'

export async function getCustomData(id: string) {
  return await apiFetch<CustomDto>(`/api/custom/${id}`)
}
```

### 步骤 2：创建服务端流式页面 (`src/app/{route}/page.tsx`)
```tsx
import React, { Suspense } from 'react'
import type { Metadata } from 'next'
import { siteConfig } from '@/config/site'
import TacticalLoader from '@/components/loading/TacticalLoader'

export const dynamic = 'force-dynamic'

export async function generateMetadata(): Promise<Metadata> {
  return {
    title: `自定义页面 · ${siteConfig.siteName}`,
  }
}

export default async function CustomPage() {
  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      <Suspense fallback={<TacticalLoader />}>
        <CustomContentAsync />
      </Suspense>
    </div>
  )
}

async function CustomContentAsync() {
  // 服务端异步直接取数渲染
  return (
    <div className="bg-theme-surface/90 rounded-3xl border border-theme-border/60 p-6 shadow-paper">
      <h1 className="text-xl font-bold text-slate-800 dark:text-slate-100">内容标题</h1>
    </div>
  )
}
```

### 步骤 3：主题预设感知适配
如果页面或组件包含主题专属微件：
```tsx
import { siteConfig } from '@/config/site'

export function MyWidget() {
  const isAnime = siteConfig.themePreset === 'anime'

  if (isAnime) {
    return <AnimeSpecialEasterEgg />
  }

  return <ModernTechCard />
}
```

### 步骤 4：门禁编译验证
执行 `npm run build`，确保 Next.js 生产优化构建（静态页面导出与动态路由解析）无任何 TypeScript 报错。
