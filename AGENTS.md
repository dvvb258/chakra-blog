# AGENTS.md - Chakra AI Coding Agent 通用指南

> 本文件是面向所有 AI 编码助手（如 Claude Code, Cursor, Copilot, Windsurf, Antigravity, Aider, Roo Code 等）的**全局通用开发规范与架构事实来源**。

---

## 1. 系统全景架构 (System Landscape)

Chakra 是一个高性能全栈个人技术博客系统，由 3 个独立子工程构成：

```
chakra-blog/
├── chakra_api_back_end/          # 后端核心：ASP.NET Core (.NET 10) 垂直切片 CQRS + PostgreSQL
├── chakra_front_end/            # 读者展示前台：Next.js 15 (App Router) + React 19 + Tailwind CSS
└── chakra_management_front_end/  # 博主管理控制台：React 19 + Vite 6 + Ant Design 5 + Zustand 5
```

---

## 2. 五大不可逾越的核心底线 (Non-Negotiable Red Lines)

任何 AI 助手在生成、修改或重构代码时，必须时刻死守以下 5 条红线：

1. **雪花 ID 精度绝对保护**：
   - 系统主键为 64 位雪花算法长整型（Snowflake ID），超出 JavaScript 最大安全整数（$2^{53} - 1$）；
   - **后端规则**：对外输出的 JSON 序列化中强制输出为 `string`（通过 `LongToStringJsonConverter`）；
   - **前端规则**：TypeScript 声明为 `string` 或 `string | number`，**严禁使用 `Number(id)` 或 `parseInt(id)`**，转换时只允许使用 `String(id)`。
2. **零错误与零警告构建门禁**：
   - 后端开启了 `TreatWarningsAsErrors`，任何代码修改必须保持 `dotnet build chakra_api_back_end/Chakra.slnx` 零警告零错误，且 `dotnet test` 全部通过；
   - 前端展示端与管理端必须通过 `npm run build`（包含严格的类型检查与生产构建打包）。
3. **垂直切片模块边界隔离**：
   - 后端划分为 `Identity`、`Content`、`Logging` 三大模块，模块间保持物理和逻辑解耦，禁止跨切片任意直接引用私有实体；
   - 写操作（Command）走 EF Core 领域实体；读操作（Query）在 Content 模块走 Dapper 高性能原生 SQL 直出 DTO，在 Identity 模块走 EF Core `AsNoTracking()`。
4. **统一 API 契约与数据库映射标准**：
   - 控制器 Action 统一返回 `ApiResult<T>`，路径小写连字符（如 `/api/auth/my-menus`）；
   - 数据库使用 PostgreSQL 自动蛇形列名映射（`UseSnakeCaseNamingConvention()`），表全小写复数下划线。
5. **主题风格预设严格解耦 (`themePreset`)**：
   - 前台支持「现代极简技术风 (`tech`)」与「木叶火影特色风 (`anime`)」双模预设，后台「站点设置」可随时无缝切换；
   - **基础功能通用化**：文章、分类、搜索、阅读耗时、代码块等通用功能一律使用标准技术术语，严禁套用动漫特定名词；
   - **特定视觉隔离**：写轮眼、苦无回顶等特色彩蛋必须用 `siteConfig.themePreset === 'anime'` 条件包裹，并为 `tech` 模式提供清爽现代的替代呈现。

---

## 3. 常用开发与质量门禁命令

```bash
# 1. 后端：运行所有测试 (需 100% 通过)
dotnet test chakra_api_back_end/Chakra.slnx

# 2. 后端：构建编译 (零警告零错误)
dotnet build chakra_api_back_end/Chakra.slnx

# 3. 展示前端：生产优化打包与类型检查
npm --prefix chakra_front_end run build

# 4. 管理后台：Vite 打包与 TypeScript 检查
npm --prefix chakra_management_front_end run build
```

---

## 4. 按需技能手册 (Specialized Task Skills)

当 Agent 收到特定领域的开发任务时，请主动查阅 `docs/skills/` 下的对应 SOP 指南：

| 任务场景 | 查阅技能文件 |
|---|---|
| **新增后端 CQRS 接口** | [`docs/skills/01-add-backend-cqrs-slice.md`](file:///e:/code/DvvbCode/DvvbCode/chakra-blog/docs/skills/01-add-backend-cqrs-slice.md) |
| **新增管理端页面与 RBAC 菜单权限** | [`docs/skills/02-add-admin-page-and-rbac.md`](file:///e:/code/DvvbCode/DvvbCode/chakra-blog/docs/skills/02-add-admin-page-and-rbac.md) |
| **新增展示端页面与主题适配** | [`docs/skills/03-add-frontend-page-and-theme.md`](file:///e:/code/DvvbCode/DvvbCode/chakra-blog/docs/skills/03-add-frontend-page-and-theme.md) |
| **数据库表结构、字段与索引变更** | [`docs/skills/04-database-schema-evolution.md`](file:///e:/code/DvvbCode/DvvbCode/chakra-blog/docs/skills/04-database-schema-evolution.md) |

---

## 5. 多 Agent 通用适配指引

- **通用事实来源**：本文件（`AGENTS.md`）作为跨 Agent 开源统一标准，供所有现代 AI 编码助手（Cursor, Antigravity, Windsurf, Aider, Roo Code 等）直接读取，无需在项目根目录维护各类碎片化的厂商私有规则文件；
- **Claude Code**：原生读取项目各层级的 `CLAUDE.md`；
- **任务按需调度**：在向任何 AI 助手派发具体场景任务时，直接在提示词中引用 `docs/skills/` 下的对应技能手册（如：*“请参考 docs/skills/01-add-backend-cqrs-slice.md 完成新功能”*）。
