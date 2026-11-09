# CLAUDE.md

This file provides comprehensive guidance for Claude Code (and other AI agents/developers) when working with the `chakra_front_end` codebase.

---

## 1. 项目概述 (Project Overview)

`chakra_front_end` 是个人博客系统的**面向读者的公开展示端**，基于 **Next.js 15 (App Router) + React 19 + TypeScript + Tailwind CSS** 构建。

- **预设主题**：内置高对比度昼夜双色视觉主题（日间金橙暖色 / 夜间青蓝冷色）+ HTML5 Canvas 动态粒子交互微件。
- **内容定位**：.NET 10、云原生微服务、系统架构与 AI 内容辅助相关技术文章。
- **核心架构**：基于 Next.js 15 App Router，支持服务端流式渲染（SSR / Suspense）、动态路由与全量后端真实数据驱动。
- **容器与部署**：配置 `output: 'standalone'` 与 Sharp 图像优化，面向 Docker Compose 交付与云原生容器部署。

---

## 2. 常用命令与开发工作流 (Commands & Workflow)

```bash
# 启动本地开发服务（默认监听 http://localhost:3000）
npm run dev

# 严格 TypeScript 类型检查
npm run typecheck

# 生产优化构建（编译 Next.js 产物与 standalone bundle）
npm run build

# 运行生产构建产物
npm run start
```

---

## 3. 环境变量与网络架构 (Environment & Networking)

### 环境变量对照

| 环境变量 | 作用说明 | 开发环境 (`.env.development`) | 生产环境 (`.env.production` / Compose) |
|---|---|---|---|
| `INTERNAL_API_BASE_URL` | 服务端 (Node.js SSR) 直连后端 WebAPI 根地址 | `http://localhost:5000/api` | `http://blogapi:8080/api` |
| `NEXT_PUBLIC_SITE_URL` | 站点公开主域名（用于 SEO、Sitemap 与 OpenGraph） | `http://localhost:3000` | `http://localhost:3000` (或实际公网域名) |

### 双通道网络与反向代理机制

- **服务端渲染阶段 (Server-Side / Node.js)**：
  - `getArticles`、`getSeriesList`、`getCategories`、`getSystemVisits` 等在 Next.js 服务端直连 `INTERNAL_API_BASE_URL`，走本地端口或 Docker 容器内部网络高速直连。
- **浏览器客户端阶段 (Client-Side / Browser)**：
  - 客户端代码中统一调用相对路径 `/api/*`（如阅读量上报、分页异步加载等）。
  - `next.config.mjs` 中配置了 `rewrites()` 反向代理，将浏览器的 `/api/:path*` 自动重定向代理至 `INTERNAL_API_BASE_URL`，消除浏览器跨域问题且无需向公网直接暴露后端端口。

---

## 4. 架构设计规范 (Design Philosophy)

### 4.1 边界划分规范（专属视觉组件 vs 通用业务页面）

1. **专属主题视觉组件**：
   - 首页角色交互：`NarutoCharacter`、`ChakraJutsuCanvas`（粒子流场渲染引擎）；
   - 回到顶部按钮：`BackToTop`（平滑置顶微件与悬停提示）；
   - 代码块复制代码：`CodeBlock`（基于 Prism.js，复制成功状态动效反馈）；
   - 404 页面：`not-found` 缺省矢量插图与引导导航；
   - 昼夜切换系统：`ThemeToggle` 切换控件与 `NinjaThemeTransition` 全屏矢量转场动效。
2. **通用业务与配置页面**：
   - 文章列表、阅读耗时预估、分类导航、标签体系、目录导览、分页控制、上下篇联动、分享等通用功能，在代码与界面中一律采用标准通用技术名词；
   - 关于页面（`about/page.tsx`）与文章详情（`articles/[id]/page.tsx`）名片直接引用配置，保持通用性。
3. **清晰配置职责**：
   - 配置职责单一，未配置项直接缺省，不引入复杂的隐式多层兜底。

### 4.2 Token 驱动的主题设计系统 (Design Tokens)

- **单一事实来源**：
  - 由 `src/config/theme.ts` 与 `src/app/globals.css` 中的 CSS RGB 通道变量统一驱动（`--theme-primary`, `--theme-secondary`, `--theme-border`, `--theme-surface`, `--theme-text`, `--theme-muted`, `--theme-bg` 等），实体映射变量为 `--theme-color-*`，Tailwind 统一映射 `theme-*` 语义类。
- **双主题调色板**：
  - **日间模式**：主色金橙（`#f59e0b`），辅色深橙（`#ea580c`），背景暖白（`#fffdfa`）；
  - **夜间模式**：主色青蓝（`#06b6d4`），辅色冷紫（`#8b5cf6`），背景深蓝黑（`#080c16`）。
- **样式规范**：
  - 按钮、卡片、徽标、标签采用实体主题色搭配 1px 微边框与微阴影，渐变仅在动态光效与 Canvas 场景使用。
- **文章排版**：
  - 正文排版采用规范的 `.prose-article` 语义类。

### 4.3 流式渲染与动态渲染策略 (Streaming & Dynamic Strategy)

- **防离线死缓存策略**：
  - 首页（`src/app/page.tsx`）与专栏列表（`src/app/series/page.tsx`）显式声明 `export const dynamic = 'force-dynamic'`。避免因构建期后端未就绪导致打包出空白页面。
- **Suspense 与骨架加载**：
  - 核心内容卡片包裹在 React `Suspense` 中，使用统一的 `TacticalLoader` 提供流式加载占位。

---

## 5. 专属视觉与主题元素清单（功能说明与换肤迁移指南）

全站包含火影主题元素的模块清单如下，便于后续更换为其他风格主题时进行替换：

| 序号 | 组件 / 模块路径 | 组件功能说明 | 包含的主题元素 | 更换新主题时的替换方案 |
|:---:|:---|:---|:---|:---|
| **1** | `src/components/home/NarutoCharacter.tsx`<br>& `naruto-visual.module.css` | 首页角色交互立绘：昼夜等比胸像特写，手心带视差追踪，触碰触发屏幕微震颤与粒子释放 | 鸣人与佐助立绘；掌心施术位点（鸣人 `28%, 64%`；佐助 `55%, 70%`） | 替换立绘图片，在 CSS 中微调触碰坐标（`.contact` 的 `top/left`）；或替换为通用产品架构图。 |
| **2** | `src/components/home/ChakraJutsuCanvas.tsx` | 实时粒子 Canvas 渲染引擎：支持待机与触发两种渲染模态 | 向心粒子与分形闪电 | 调整 Canvas 粒子算法（如改为引力微粒或波形）；组件参数 `mode='idle'/'full'` 已解耦。 |
| **3** | `src/components/theme/ThemeToggle.tsx` | 昼夜模式切换按钮：顶部导航栏右侧，点击触发回旋与全屏转场 | 漩涡族徽（深红旋涡）与宇智波团扇（红白圆形）SVG 矢量 | 将内部图标替换为新主题标志，或使用通用的 Sun / Moon 图标。 |
| **4** | `src/components/theme/NinjaThemeTransition.tsx` | 全屏昼夜转场动效：切换主题时屏幕中心扩散并展示对应矢量图腾（650ms 自动卸载） | 昼夜主题对应矢量图腾动画 | 替换转场中央的 SVG 图标，扩散光环颜色已自动关联 CSS 变量。 |
| **5** | `src/components/articles/CodeBlock.tsx` | 文章代码块与复制交互：Prism 高亮代码框，右上角提示“复制代码”，点击复制触发微动效 | 复制代码成功后弹出的写轮眼旋转微动效（`SharinganEyeIcon`） | 将 `SharinganEyeIcon` 替换为标准 Check 勾选图标。 |
| **6** | `src/components/ui/BackToTop.tsx`<br>& `siteConfig.backToTop` | 回到顶部悬浮按钮：滚动超 400px 浮现，点击平滑回顶 | 特制三叉苦无资产（`hiraishin-kunai.webp`）与金色光效 | 在 `siteConfig.backToTop` 中配置新图标路径、alt 与文案，或替换为通用向上箭头。 |
| **7** | `src/app/not-found.tsx`<br>& `siteConfig.notFound` | 404 缺省页：返回首页与归档入口引导 | 卡卡西迷路立绘插图（`kakashi-lost.webp`） | 在 `siteConfig.notFound.image` 中配置新插图路径与 alt。 |
| **8** | `src/components/home/SnakeGameWidget.tsx` | 代码活跃度小微件：20x7 矩阵自动巡航模拟 Git 提交热力 | 随昼夜模式切换蛇头小图标（白天蛤蟆吉 🐸，夜间青蛇 🐍） | 蛇头与目标图标可换为通用字符（如 `●`/`◆`），标题与单位完全由 `siteConfig.hero` 驱动。 |
| **9** | `src/config/site.ts` (`character`) | 首页首屏角色展示名片配置 | 声明鸣人/佐助名字、状态描述与立绘图片路径 | 修改为新主题的角色名、状态与图片路径。 |
| **10** | `src/config/site.ts` (`notFound`) | 404 缺省页文案与插图配置 | 声明台词与插图路径（`kakashi-lost.webp`） | 修改为通用名言或“页面不存在”。 |
| **11** | `src/config/site.ts` (`backToTop`) | 回到顶部按钮图标与文案配置 | 声明苦无图标路径（`hiraishin-kunai.webp`）与文案 | 修改为通用图标与文案。 |
| **12** | `src/config/site.ts` (`about.skills`) | 关于页面技能卡片配置 | 声明各项技能前缀属性 | 可直接去掉前缀保留纯净技术名（如“后端架构”）。 |
| **13** | `src/config/site.ts` (默认资产路径) | 站点默认头像、图标与封面图引用 | `avatar`, `favicon`, `defaultCover` | 替换为自定义博主头像和文章默认封面。 |
| **14** | `public/images/naruto/` 目录 | 静态资源存放目录 | 集中存放火影主题高清 WebP 图片资产 | 存放新主题的静态资源，或替换同名文件。 |

---

## 6. 路由与页面清单 (Routes & Pages)

```
/                     → 首页（Hero 首屏、技术栈标签、代码热力微件、终端微件、立绘与 Canvas、最新文章、专栏列表）
/articles             → 文章归档列表（支持 keyword 模糊搜索、category 分类筛选、服务端分页）
/articles/[id]        → 文章详情（文章大卡片居中、目录粘性导览、Markdown 正文、推荐阅读、上下篇）
/series               → 专题专栏列表（多篇章结构化知识库）
/series/[id]          → 专栏详情（专栏介绍与篇章路线图）
/about                → 关于博主（档案卡片、核心技术栈矩阵）
/not-found            → 404 缺省页
/sitemap.xml          → 动态站点地图（根据全站文章与专栏自动生成）
/robots.txt           → 搜索引擎抓取规则声明
```

---

## 7. 目录结构全貌 (Project Directory Structure)

```
chakra_front_end/
├── src/
│   ├── app/                               Next.js 15 App Router 路由与页面
│   │   ├── layout.tsx                     根布局
│   │   ├── page.tsx                       首页（force-dynamic 渲染）
│   │   ├── globals.css                    全局样式与 CSS 变量 Token（--theme-color-*）
│   │   ├── error.tsx / loading.tsx        全局错误与加载占位
│   │   ├── not-found.tsx                  404 页面
│   │   ├── sitemap.ts / robots.ts         SEO 动态站点地图
│   │   ├── articles/                      文章归档列表与详情
│   │   ├── series/                        专栏列表与详情
│   │   └── about/                         关于博主页面
│   ├── config/
│   │   ├── site.ts                        [配置层] 站点默认配置（可被后台「站点设置」运行期覆盖）
│   │   └── theme.ts                       [主题层] Design Tokens 调色板（Hex 与 RGB 变量映射）
│   ├── components/
│   │   ├── layout/                        Navbar, Footer
│   │   ├── home/                          HeroSection, NarutoCharacter, ChakraJutsuCanvas, SnakeGameWidget, InteractiveTerminalWidget
│   │   ├── articles/                      ArticleCard, ArticleListClient, ArticleNavigation, ArticleInteractions, MarkdownRenderer, CodeBlock, TableOfContents
│   │   ├── theme/                         ThemeProvider, ThemeToggle, NinjaThemeTransition
│   │   ├── loading/                       StartupLoader (开屏动效), TacticalLoader (通用骨架), RouteLoadingIndicator (路由进度条)
│   │   ├── analytics/                     SiteTracker (系统访问量轻量上报)
│   │   └── ui/                            BackToTop, Badge, ReadingProgressBar
│   ├── lib/
│   │   ├── api.ts                         统一 API 请求适配层（数据标准化、封面解析、真实统计直出）
│   │   ├── reading.ts                     文章阅读时长计算工具
│   │   └── markdown.ts                    Markdown 标题 TOC 提取与 slugify 工具
│   └── types/
│       └── index.ts                       数据契约定义
├── public/
│   └── images/
│       └── naruto/                        立绘、苦无、头像、默认封面
├── next.config.mjs                        Next.js 核心配置（standalone 模式、sharp 图像格式、rewrites API 代理）
├── tailwind.config.js                     Tailwind 配置（theme-* 语义类绑定、阴影系统、圆角）
├── tsconfig.json                          TypeScript 配置
└── package.json                           依赖与运行脚本
```

---

## 8. API 适配层与数据契约 (`src/lib/api.ts`)

### 8.1 核心数据处理

- **数据驱动**：接口异常或为空时展示规范的空状态组件或友好错误提示。
- **全量字段标准化 (`normalizeArticle` / `normalizeSeries`)**：
  - 抹平后端字段类型（如 `categoryId` 转换为字符串，`views` / `readingTime` 安全兜底）。
  - **规范封面解析 (`resolveCoverImage`)**：优先使用 API 返回的 `coverUrl`/`coverImage`；未提供时平滑回退至 `siteConfig.defaultCover`。

### 8.2 已对接 API 清单

| API 端点 | 方法 | 对应函数 | 用途与参数 |
|---|---|---|---|
| `/api/articles` | GET | `getArticles(params)` | 分页检索文章（入参：`keyword`, `categoryId`, `tag`, `pageIndex`, `pageSize`） |
| `/api/articles/:id` | GET | `getArticleById(id)` | 查询单篇文章详情（包含 Markdown 正文） |
| `/api/articles/:id/neighbors` | GET | `getArticleNeighbors(id)` | 获取当前文章的上一篇与下一篇简要信息 |
| `/api/series` | GET | `getSeriesList(params)` | 分页获取知识专栏列表 |
| `/api/series/:id` | GET | `getSeriesById(id)` | 获取专栏详情及所收录的完整篇章路线图 |
| `/api/categories` | GET | `getCategories()` | 获取技术分类与收录文章数统计 |
| `/api/stats/visit` | POST | `recordSystemVisit()` | 单会话防抖系统总访问量轻量上报 |
| `/api/stats/articles/:id/view` | POST | `recordArticleView(id)` | 单篇文章阅读量防抖上报（sessionStorage 级防重复） |
| `/api/stats/visits` | GET | `getSystemVisits()` | 全站累计访问量查询（带 ISR 缓存） |

---

## 9. 编码安全与改动守则

1. **类型安全检查**：提交修改前执行 `npm run typecheck`，确保 0 错误。
2. **专业命名规范**：编写或调整基础组件（列表、分页、阅读耗时、筛选、分享等）时，采用通用专业技术名词；保持配置与业务逻辑解耦。
3. **色彩与主题 Token**：颜色统一使用 `text-theme-*`、`bg-theme-*`、`border-theme-*` 语义类，保证昼夜模式切换自适应。
4. **雪花 ID 字符串化**：后端传来的 64 位长整型 ID 在前端序列化和组件交互中必须作为 `string` 处理，防止 JavaScript Number 精度截断。

---

## 10. 容器部署规范 (Container Readiness)

- **容器内部 SSR 直连**：在 Docker Compose 网络中，`INTERNAL_API_BASE_URL` 环境变量直接指向后端容器服务（如 `http://blogapi:8080/api`），服务端在内网直接高速通信；
- **客户端代理透传**：浏览器发起的所有异步 `/api/*` 请求通过 Next.js `rewrites()` 统一内网转发，无需在公网向展示端暴露后端 API 端口，避免浏览器跨域问题；
- **媒体文件访问**：文章封面与正文插图访问直连或经反代映射至媒体路径，前台使用 `resolveCoverImage` 保证默认降级正常生效。
