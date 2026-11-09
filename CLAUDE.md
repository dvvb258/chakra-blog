# CLAUDE.md

本项目集为 **Chakra 现代化个人技术博客全栈系统**，由 3 个独立但紧密协作的子工程构成：
1. **chakra_front_end**：面向读者的公开展示端（Next.js 15 App Router）
2. **chakra_api_back_end**：统一的 Web API 后端服务（.NET 10 模块化 Web API）
3. **chakra_management_front_end**：面向博主的后台管理控制台（React 19 + Vite 6 + Ant Design 5）

---

## 3 大项目全景概览

```
chakra-blog/
├── chakra_front_end/            # 公开展示端 (Next.js 15 App Router)
├── chakra_api_back_end/          # 垂直切片 Web API (.NET 10)
└── chakra_management_front_end/  # 博客管理后台 (React 19 + Ant Design 5)
```

| 项目 | 技术栈 | 默认端口 | 职责定位 |
|---|---|---|---|
| **chakra_front_end** | Next.js 15, React 19, Tailwind CSS, Prism.js | 3000 | 昼夜双主题展示、Canvas 动态粒子交互、仿终端微件、服务端流式渲染与统计上报。 |
| **chakra_api_back_end** | .NET 10, EF Core 10, Dapper, MediatR, PostgreSQL, Hangfire | 5000 / 5001 | 垂直切片 CQRS 架构，雪花 ID 字符串化，三级 RBAC 权限体系，支持 Redis 分布式锁与单机互斥锁降级，支持 Local/S3 存储切换。 |
| **chakra_management_front_end** | React 19, Vite 6, TypeScript, Ant Design 5, Zustand 5 | 3101 | 双 Token 认证、文章与专栏 CRUD、分类维护、系统日志看板、ECharts 统计看板与动态菜单树。 |

---

## 项目一：chakra_front_end（博客展示端）

- **技术栈**：Next.js 15 (App Router), React 19, TypeScript, Tailwind CSS, Lucide React, PrismJS
- **主题与架构设计规范**：
  - **视觉预设系统**：内置一套高对比度的昼夜双色主题（日间金橙暖色 / 夜间青蓝冷色），支持全站动态换肤。
  - **三层架构设计与名词边界规范**：
    1. **关键展示层配置化**：
       - Hero 标语、按钮文案、博主名片、技能矩阵（`about.skills`）、页脚说明与 404 引导文案集中在 `src/config/site.ts` 声明为默认值，管理员可在后台「站点设置」以 JSON 覆盖项运行期覆盖（60 秒内生效，无需重新打包）。
       - 保持配置职责单一、明确，未配置项直接缺省，不引入冗余别名层。
    2. **基础功能层通用化**：
       - 文章列表、阅读耗时预估、分类导航、标签过滤、目录导航、分页组件、上下篇联动、分享与互动等基础功能，在代码与界面中一律采用标准通用技术名词。
    3. **视觉交互层**：
       - 首屏立绘、Canvas 粒子动效、回到顶部图标、昼夜主题切换图标、404 页面插图、复制代码动效等主题视觉资产集中管理，基于标准 SVG 与 WebP 资源渲染。
  - **设计系统 Token 规范**：
    - **单一事实来源**：通过 `src/config/theme.ts` 与 `src/app/globals.css` 中的 CSS RGB 通道变量驱动（`--theme-primary`, `--theme-secondary`, `--theme-border`, `--theme-surface`, `--theme-text`, `--theme-muted`, `--theme-bg` 等），实体映射变量为 `--theme-color-*`，Tailwind 统一映射 `theme-*` 语义类；
    - **双主题调色板**：
      - **日间模式**：主色金橙（`#f59e0b`），辅色深橙（`#ea580c`），背景暖白（`#fffdfa`）；
      - **夜间模式**：主色青蓝（`#06b6d4`），辅色冷紫（`#8b5cf6`），背景深蓝黑（`#080c16`）；
    - **视觉样式规范**：主要按钮、分类高亮、置顶徽标与页码采用实体主题色搭配 1px 微边框与轻量阴影；渐变效果仅保留在动态光效与 Canvas 粒子渲染场景；
    - **全局统一换色**：调整全站色彩仅需在 `theme.ts` 或 `globals.css` 中修改对应的 Hex / RGB 色值，卡片、边框、按钮及悬停状态自动联动生效。
  - **开屏动效感知**：
    - 启动动画感知当前昼夜模式，避免夜间模式初次访问时出现白底闪烁；
  - **正文排版**：
    - Markdown 渲染采用规范的 `.prose-article` 语义类。
- **前台特色视觉与交互组件清单**：
  - **角色动态交互组件 (`NarutoCharacter`)**：首屏待机粒子循环与触碰事件联动，集成 450ms 视图微震颤动效；
  - **Canvas 粒子渲染引擎 (`ChakraJutsuCanvas`)**：提供向心气旋粒子与分形闪电双模态渲染算法，支持待机低频与触发全量渲染双模态；
  - **回到顶部微件 (`BackToTop`)**：基于平滑滚动（Smooth Scroll）的页面置顶微件，带悬停微动效提示；
  - **主题模式切换 (`ThemeToggle` / `NinjaThemeTransition`)**：昼夜主题切换控件与全屏矢量旋转遮罩过渡动效；
  - **交互式终端模拟器 (`InteractiveTerminalWidget`)**：深色命令行界面微件，支持打字机文本流输出与基础命令解析（`help`, `status`, `whoami`, `skills`, `contact`, `theme`, `clear`），内置命令历史记录（`↑`/`↓`）；
  - **代码块组件 (`CodeBlock`)**：基于 Prism.js 的代码高亮组件，支持语言标识、一键复制与成功状态动效反馈；
  - **404 缺省页 (`not-found`)**：矢量插图、友好引导文案与返回首页/全部文章导航入口；
  - **代码活跃度矩阵 (`SnakeGameWidget`)**：模拟 Git 提交热力方块的自动化漫游巡航微件。
- **主题预设解耦与换肤指南（视觉资源隔离）**：
  系统默认采用一套高对比度的动漫昼夜主题套件。为了便于更换为现代极简、赛博朋克或商务科技风格，所有涉及特定视觉元素的代码均已实现严格解耦，全站仅以下 13 处引用了主题专属资源，其余通用逻辑与业务页面完全无侵入：
  1. `src/config/site.ts`：`character`（角色名片、文案与立绘图片路径）；
  2. `src/config/site.ts`：`notFound`（文案台词与插图路径/alt）；
  3. `src/config/site.ts`：`backToTop`（图标路径与提示文案）；
  4. `src/config/site.ts`：`about.skills`（技术卡片属性前缀）；
  5. `src/config/site.ts`：默认头像与封面（`avatar`, `favicon`, `defaultCover`）；
  6. `src/components/home/NarutoCharacter.tsx`（立绘展示与触碰位点）；
  7. `src/components/home/ChakraJutsuCanvas.tsx`（粒子 Canvas 渲染算法）；
  8. `src/components/theme/ThemeToggle.tsx`（昼夜切换矢量图标）；
  9. `src/components/theme/NinjaThemeTransition.tsx`（全屏转场动画矢量）；
  10. `src/components/articles/CodeBlock.tsx`（复制代码微动效图标）；
  11. `src/components/ui/BackToTop.tsx`（回到顶部交互）；
  12. `src/app/not-found.tsx`（404 插图）；
  13. `public/images/naruto/`（相关静态 WebP 图片资源）。
- **数据流与渲染策略**：
  - **数据驱动渲染**：接口异常或数据为空时展示统一空状态组件。
  - **双通道网络转发**：服务端 SSR（Node.js）直连后端内网 `INTERNAL_API_BASE_URL`；客户端统一请求相对路径 `/api/*`，由 Next.js `rewrites` 代理转发。
  - **动态流式渲染**：首页与专栏页显式声明 `export const dynamic = 'force-dynamic'`，配合 React `Suspense` 与 `TacticalLoader` 骨架占位。
  - **封面地址解析**：`resolveCoverImage` 优先使用接口返回的 `coverUrl`，未提供时平滑回退至 `siteConfig.defaultCover`。
- **前台对接 API 清单**：
  - `GET /api/articles`：分页文章列表（`keyword`, `categoryId`, `tag`, `pageIndex`, `pageSize`）
  - `GET /api/articles/{id}`：文章详情（含 Markdown 正文、阅读时长与元数据）
  - `GET /api/articles/{id}/neighbors`：上一篇 / 下一篇相邻文章
  - `GET /api/series` / `GET /api/series/{id}`：专栏列表与专栏详情
  - `GET /api/categories`：技术分类与收录篇数统计列表
  - `POST /api/stats/visit`：轻量单会话防抖系统总访问量上报
  - `POST /api/stats/articles/{id}/view`：单篇文章阅读量防抖上报（Session 级去重）
  - `GET /api/stats/visits`：全站总访问量查询（ISR 缓存）

---

## 项目二：chakra_api_back_end（WebAPI 服务端）

- **技术栈**：ASP.NET Core (.NET 10), PostgreSQL (Npgsql), Redis, Dapper, EF Core, MediatR, Hangfire
- **架构范式**：垂直切片架构（Vertical Slice CQRS + 读写分离 + 雪花 ID + 自动蛇形命名映射）
- **Web API 设计规范**：
  - **路由规范**：URL 路径小写 + kebab-case（如 `/api/auth/my-menus`）。公开接口前缀 `/api/...`，管理端前缀 `/api/admin/...`，认证端前缀 `/api/auth/...`。
  - **统一响应契约**：成功统一返回 `ApiResult<T>`（`code: 200`），分页列表统一包装为 `PagedResult<T>`（`items, total, pageIndex, pageSize, totalPages`）。
  - **雪花 ID 字符串输出**：64 位 ID 在 JSON 序列化中转换为字符串（`LongToStringJsonConverter`），防止前端 JavaScript 精度丢失。
  - **RBAC 权限体系**：权限资源由 `menus.type` 划分为【目录 / 菜单 / 按钮】三级模型，Controller Action 统一使用 `[RequirePermission(...)]` 策略鉴权，前端配合 `<PermissionGuard>` 实现按钮级显隐与禁用。
  - **第三方依赖 SPI 抽象**：存储、AI、OAuth 抽象为标准接口（`IStorageService`, `IChatClient`, `IOAuthProvider`），支持配置化插拔。
- **统一响应格式 (`ApiResult<T>`)**：
  ```json
  {
    "code": 200,
    "message": "操作成功",
    "data": { ... },
    "traceId": "0HN1234567890",
    "timestamp": "2026-03-30T12:00:00Z"
  }
  ```
- **核心模块端点**：
  - **Identity 认证模块**：
    - `POST /api/auth/login`：账号密码登录（签发 AccessToken + RefreshToken）
    - `POST /api/auth/refresh`：静默续期（令牌轮换）
    - `POST /api/auth/revoke`：撤销令牌注销登录
    - `GET /api/auth/me`：获取当前登录人资料
    - `PUT /api/auth/profile`：修改个人资料
    - `PUT /api/auth/password`：修改登录密码
    - `GET /api/auth/github/url`：获取 GitHub OAuth 跳转链接
    - `POST /api/auth/github`：GitHub 授权码换 Token 与快捷登录/注册
    - `POST /api/auth/github/bind` / `DELETE /api/auth/github/unbind`：GitHub 绑定与解绑
    - `GET /api/auth/my-menus`：获取当前用户授权的菜单与权限资源
    - `GET|POST|PUT|DELETE /api/admin/roles`：管理端角色 CRUD
    - `GET|POST|PUT|DELETE /api/admin/menus`：管理端菜单与权限项 CRUD
  - **Content 内容模块**：
    - `GET /api/system/public-config`：公开系统特性探测接口（探测 GitHub 登录、AI、存储类型开启状态）
    - `GET /api/admin/dashboard/summary`：运营仪表盘统计聚合查询
    - `GET|POST|PUT|DELETE /api/admin/articles`：文章全量 CRUD
    - `GET|POST|PUT|DELETE /api/admin/categories`：技术分类 CRUD（带收录文章防删保护）
    - `GET /api/tags`：热门技术标签查询
    - `GET|POST|PUT|DELETE /api/admin/series`：专栏系列 CRUD
    - `POST /api/admin/media/upload`：媒体文件上传（支持本地持久卷与 S3 协议存储）
  - **Logging 系统日志模块**：
    - `GET /api/admin/logs/audit`：多维过滤查询业务操作审计日志（Channels 内存队列批量刷盘）
    - `GET /api/admin/logs/exceptions`：分页查询系统未处理异常日志
    - `GET /api/admin/logs/exceptions/{id}`：获取异常堆栈追踪详情

---

## 项目三：chakra_management_front_end（博客管理后台）

- **技术栈**：React 19, Vite 6, TypeScript, Ant Design 5, Zustand 5, Axios, React Router 7
- **核心功能体系**：
  1. **双 Token 认证与静默续期**：Axios 拦截器集成单并发互斥续期锁，支持账号密码登录与 GitHub OAuth 快捷登录。
  2. **管理控制台视觉**：Ant Design 5 深色主题，适配暗色灰阶。
  3. **内容创作工作台**：
     - 文章管理：多维筛选、分页、浏览量统计与软删除。超级管理员具备全站文章（含协作者与 AI 生成文章）的直达编辑与免审直接发布上线权。
     - Markdown 创作：分屏实时预览、自动字数与阅读耗时预估、内联分类免跳出快速新建、端到端乐观并发版本锁（Version）。
     - 图片上传：支持剪贴板截图粘贴（`Ctrl+V`）、外部文件拖拽上传与工具栏选择，光标处精准插入 Markdown 语法。
     - 专栏管理：专栏列表维护、主题色选择、封面图上传与文章解绑。
  4. **个人中心与安全设置**：
     - 个人资料与头像上传维护；
     - 登录密码修改与纯第三方账号首次密码设置；
     - GitHub 关联状态展示、绑定与安全解绑。
  5. **系统审计日志**：
     - 非阻塞内存通道入库，MediatR 管道无侵入审计；
     - 操作审计与异常日志双标签页，多维检索与堆栈高亮展开。
  6. **运营数据看板**：
     - 全站访问、文章总数、阅读量与专栏总数 4 项指标；
     - 近 7 天发文趋势面积图与技术分类占比环形图；
     - 最新文章快速编辑与前台预览跳转。
  7. **RBAC 角色权限与动态菜单树**：
     - 内置保留角色 `Admin` 防锁死保护；
     - 自定义角色权限树勾选；
     - 侧边栏根据登录人权限资源动态渲染，未授权路由自动拦截与重定向。
  8. **分类与标签管理**：
     - 分类雪花 ID 强关联体系；
     - 下属文章防误删安全校验；
     - 热门标签池动态同步。

---

## 端口与环境变量对照

| 服务 | 开发地址 | 生产/容器网络变量 |
|---|---|---|
| 后端 API | `http://localhost:5000` / `https://localhost:5001` | 宿主机: `http://localhost:5000`<br>容器内网: `http://blogapi:8080`<br>`ASPNETCORE_ENVIRONMENT=Production` |
| 展示端前端 | `http://localhost:3000` | 开发: `INTERNAL_API_BASE_URL=http://localhost:5000/api`<br>容器内网: `INTERNAL_API_BASE_URL=http://blogapi:8080/api`<br>`NEXT_PUBLIC_SITE_URL=http://localhost:3000` |
| 管理端后台 | `http://localhost:3101` | 开发: `VITE_API_BASE_URL=http://localhost:5000`<br>生产: `VITE_API_BASE_URL=` (走 Nginx 统一反代) |

---

## 代码修改与安全守则

1. 后端改动必须维持零警告零错误构建：`dotnet build Chakra.slnx`。
2. 前端改动必须通过类型安全检查：`npm run build`。
3. 严格遵循现有 API 契约，64 位长整型雪花 ID 在前端一律按字符串处理，防止精度截断。
4. 后端 Identity 与 Content 模块保持物理与逻辑垂直切片独立，避免非必要的跨模块耦合。

---

## 系统架构与扩展性设计 (SPI Pattern)

1. **Redis 可选与降级策略**：通过 `Redis:Enabled` 开关控制。启用时用于后台 AI 任务调度分布式排他锁；未启用时自动降级为单机进程内互斥锁（`SemaphoreSlim`）。全站访问量统计基于 PostgreSQL 原子写入持久化，无强制缓存依赖。
2. **对象存储抽象 (SPI)**：提供 `LocalStorageService`（本地持久化目录 `/uploads`）与 `S3StorageService`（兼容 MinIO / 阿里云 OSS / AWS S3 / Cloudflare R2），通过 `Storage:Provider` 配置切换。
3. **特性探测与按需启用**：未配置第三方登录或 AI 相关参数时，接口返回标准错误提示，并通过 `GET /api/system/public-config` 向前端广播已启用的功能特性，前端自适应调整界面入口。
4. **配置注入规范**：
   - **本地开发**：各子工程已配置默认连接参数，可直接独立启动；
   - **容器化部署**：由根目录 `.env` 文件统一注入环境变量，无需修改子工程配置。
