# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the `chakra_management_front_end` codebase.

---

## 1. 项目概述 (Project Overview)

`chakra_management_front_end` 是面向博客作者与系统管理员的**控制台管理系统**，基于 **React 19 + Vite 6 + TypeScript + Ant Design 5 + Zustand 5** 构建。

- **核心定位**：博客内容创作、专栏组织、文章分类、系统审计与运行监控、RBAC 细粒度权限治理工作台。
- **视觉风格**：基于 Slate 灰阶调色的深色主题，全站组件统一适配。
- **状态与认证**：双 Token（AccessToken + RefreshToken）静默刷新、单并发排队续期锁、GitHub OAuth 登录与账号绑定。
- **特性探测驱动**：根据后端特性探测接口（`GET /api/system/public-config`）动态自适应功能显隐（如 GitHub 快捷登录）。

---

## 2. 常用命令与开发工作流

```bash
# 安装依赖
npm install

# 启动本地开发服务器（默认监听 http://localhost:3101）
npm run dev

# 严格 TypeScript 类型检查 + Vite 生产优化打包
npm run build

# 本地预览生产构建产物
npm run preview
```

---

## 3. 环境变量与网络架构

### 环境变量对照

| 变量 | 用途说明 | 开发环境 (`.env.development`) | 生产环境 (`.env.production`) |
|---|---|---|---|
| `VITE_API_BASE_URL` | 后端 API 服务地址（为空时自动使用相对路径 `/api`，供 Nginx 反向代理） | `http://localhost:5000` | `http://localhost:5000` (或留空走 Nginx) |
| `VITE_BLOG_FRONT_URL` | 前台展示端主页地址的**构建期回退值**；实际地址优先取后台「站点设置」中的 `url` | `http://localhost:3000` | `http://localhost:3000` |

### 网络请求与续期机制 (`src/api/client.ts`)

- **统一契约解包**：自动解包后端 `ApiResult<T>`，业务成功返回 `data`，业务失败抛出带有具体 `message` 的错误。
- **全链路 Trace ID**：前端请求自动注入 `X-Trace-Id` 请求头，便于后端审计日志追踪。
- **401 互斥续期锁**：
  - AccessToken 过期触发 401 时，加单并发锁发起 `POST /api/auth/refresh`；
  - 续期过程中产生的新请求自动推入挂起等待队列；
  - 续期成功后批量重放队列所有请求；若 RefreshToken 失效，则清空队列并重定向至 `/login`。

---

## 4. 系统功能架构与路由全景

### 4.1 路由架构 (React Router 7)

```
/login                 → 登录页（账号密码 / 可选 GitHub OAuth 快捷登录）
/                      → 运营仪表盘（4 项 KPI、ECharts 7日发布趋势与分类分布、最新文章列表）
/articles              → 文章列表管理（多维筛选、分类/专栏联动、浏览量统计、软删除）
/articles/create       → Markdown 创作工作台（分屏预览、字数耗时预估、自动/手动摘要、封面强调色、内联新建分类）
/articles/:id/edit     → 编辑文章（乐观锁版本校验、快捷键 Ctrl+S / Cmd+S、前台一键预览）
/categories            → 技术分类管理（分类 CRUD、排序权重、收录文章数量统计、防误删校验）
/series                → 专栏系列管理（专栏 CRUD、主题色选择器、封面上传、所属文章绑定）
/logs                  → 系统运行日志（业务操作审计日志 + 全局未处理异常堆栈查看器）
/roles                 → RBAC 角色管理（自定义角色、树形权限勾选器、Admin 内置角色保护）
/menus                 → 系统菜单与权限项（目录/菜单/按钮三级资源树全生命周期 CRUD）
/users                 → 个人中心与账号安全（资料维护、头像上传、独立密码设置、GitHub 绑定与解绑）
```

### 4.2 路由守卫与动态侧边栏

- `AuthGuard`：校验会话有效性；进入后台时通过 `restoreSession()` 自动校验 Token 并拉取登录人最新资料与菜单权限树。
- **动态侧边栏与按钮权限**：
  - 超管（Admin）具备全量权限（Bypass 模式）；
  - 普通角色根据后端 `GET /api/auth/my-menus` 动态渲染侧边栏；
  - 页面内部按钮统一使用 `<PermissionGuard code="...">` 组件实现按钮级细粒度显隐控制。

---

## 5. 核心模块与业务特性

### 5.1 Markdown 创作工作台
- **实时分屏预览**：基于 `@uiw/react-md-editor` 适配深色主题样式。
- **图片上传与插入**：
  - 支持剪贴板截图直接粘贴（`Ctrl+V`）；
  - 支持外部图片拖拽上传（Drag & Drop）；
  - 支持工具栏图片图标唤起文件选择器；
  - 上传成功后在光标处插入 `![alt](url)` 语法。
- **内联分类快速新建**：选择分类时支持直接点击“新建分类项”，轻量弹窗录入即可自动创建并选中。
- **乐观并发锁控制**：编辑保存携带 `version`，防止并发覆盖冲突（409 Conflict 提示）。

### 5.2 仪表盘数据大屏 (Dashboard Analytics)
- **数据指标**：对接后端 `GET /api/admin/dashboard/summary` 统计接口。
- **ECharts 可视化**：
  - 近 7 天发布与创作波形图：平滑面积图，带交互十字准星；
  - 分类篇数占比环形图：反映内容技术栈分布；
  - 顶部 4 大 KPI 卡片（全站访问量、文章总量、总阅读量、专栏总量）。

### 5.3 开源特性探测与动态自适应 (Feature Flags)
- 登录页与个人中心根据系统探测接口自适应：
  - 若系统配置中未开启 GitHub OAuth，登录页自动隐藏第三方登录区域；
  - 个人中心根据特性状态动态隐藏或禁用 GitHub 绑定功能。

---

## 6. 代码质量与开发维护守则

1. **类型完备**：所有前后端交互在 `src/types/` 定义严格对齐的 TypeScript 接口，雪花 ID 统一作为 `string` 处理，防止精度截断。
2. **API 统一收敛**：业务组件内不直接调用原生 `axios` 或 `fetch`，所有接口请求收敛至 `src/api/` 对应模块。
3. **零报错构建**：每次功能迭代或修改后，必须运行 `npm run build` 确保 TypeScript 类型与 Vite 打包通过。
4. **容器化兼容**：Vite 项目在打包为生产静态文件时，API 基地址支持相对路径 `/api`，通过生产 Nginx 反向代理至后端，实现容器镜像环境解耦。
