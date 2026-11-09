# Chakra 全栈博客二次开发与垂直切片扩展指南 (Development Guide)

本文档阐述 Chakra 博客的技术架构、分层规范与二次开发操作流程（SOP），用于指导新功能扩展与代码维护。

---

## 目录

1. [架构全景与子工程定位](#1-架构全景与子工程定位)
2. [本地快速起步与调试工作流](#2-本地快速起步与调试工作流)
3. [后端垂直切片 (Vertical Slice CQRS) 新增功能 SOP](#3-后端垂直切片-vertical-slice-cqrs-新增功能-sop)
4. [管理后台 (React 19 + Ant Design 5) 新增功能 SOP](#4-管理后台-react-19--ant-design-5-新增功能-sop)
5. [展示前台 (Next.js 15 App Router) 新增功能 SOP](#5-展示前台-nextjs-15-app-router-新增功能-sop)
6. [雪花 ID 与数据精度安全准则](#6-雪花-id-与数据精度安全准则)
7. [质量门禁与自动化测试规范](#7-质量门禁与自动化测试规范)
8. [代码质量与开发规范](#8-代码质量与开发规范)

---

## 1. 架构全景与子工程定位

```
chakra-blog/
├── chakra_front_end/            # 读者公开展示端 (Next.js 15 App Router + React 19 + Tailwind CSS)
├── chakra_api_back_end/          # WebAPI 后端核心 (.NET 10 + PostgreSQL + MediatR + EF Core + Dapper)
└── chakra_management_front_end/  # 博主管理后台 (React 19 + Vite 6 + Ant Design 5 + Zustand 5)
```

### 网络流向与端口约定

| 服务 | 默认端口 | 生产容器端口 | 通信特征 |
|---|---|---|---|
| **blogapi** (后端) | `5000` | `8080` | 提供全站核心 JSON RESTful 接口与媒体文件直出服务。 |
| **admin** (管理端) | `3101` | `80` | 单页 SPA 控制台，通过 Nginx 反向代理将 `/api/` 转发至后端。 |
| **blog** (展示端) | `3000` | `3000` | Next.js 15 服务端直连后端内网 (`INTERNAL_API_BASE_URL`)，客户端通过 Next.js `rewrites` 代理请求相对路径 `/api/`。 |

---

## 2. 本地快速起步与调试工作流

本地开发环境下已预置默认连接参数，无需创建或修改 `.env` 文件。

### 步骤 1：启动本地 PostgreSQL 数据库
```bash
docker run -d --name local-postgres \
  -e POSTGRES_DB=blog \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -v $(pwd)/chakra_api_back_end/scripts/sql/init.sql:/docker-entrypoint-initdb.d/01_init.sql:ro \
  postgres:16-alpine
```

### 步骤 2：启动后端 WebAPI (.NET 10)
```bash
cd chakra_api_back_end
dotnet restore Chakra.slnx
dotnet run --project src/ApiHost/ApiHost.csproj
# 监听 http://localhost:5000，Swagger 文档访问 http://localhost:5000/swagger
```

### 步骤 3：启动管理控制台 (React 19 + Vite 6)
```bash
cd chakra_management_front_end
npm install
npm run dev
# 默认监听 http://localhost:3101
```

### 步骤 4：启动展示前台 (Next.js 15)
```bash
cd chakra_front_end
npm install
npm run dev
# 默认监听 http://localhost:3000
```

---

## 3. 后端垂直切片 (Vertical Slice CQRS) 新增功能 SOP

在 Chakra 后端中，业务被纵向切分为自包含的功能切片（Vertical Slice），严格遵循 **CQRS 读写职责分离** 原则：

* **写操作 (Command)**：面向领域实体（Domain Entity），通过 EF Core `DbContext` 写入，支持乐观并发锁 `Version` 与雪花 ID；
* **读操作 (Query)**：只读查询，在 `Content` 模块注入 `IContentDbConnectionFactory` 使用 **Dapper** 编写高性能 SQL 直出 DTO；在 `Identity` 模块注入 `IdentityDbContext` 使用 **EF Core `AsNoTracking()`** 查询。

### 示例：如何添加一个“专栏订阅/收藏”切片？

#### 第 1 步：创建命令/查询 DTO (`Application/{Module}/DTOs/`)
```csharp
namespace Content.Application.Series.DTOs;

public sealed record SubscribeSeriesRequest
{
    public string? Remark { get; init; }
}
```

#### 第 2 步：创建 Command 与 Validator (`Application/{Module}/Commands/`)
```csharp
using SharedKernel.Application.Attributes;
using SharedKernel.Application.CQRS;
using SharedKernel.Constants;
using FluentValidation;

namespace Content.Application.Series.Commands;

// 1. 命令声明（通过特性挂载无侵入审计日志）
[AuditLog(AuditModules.Series, "订阅专栏")]
public sealed record SubscribeSeriesCommand(
    long SeriesId,
    long UserId,
    string? Remark = null) : ICommand;

// 2. 校验器声明（由 MediatR 全局 ValidationBehavior 自动拦截）
public sealed class SubscribeSeriesCommandValidator : AbstractValidator<SubscribeSeriesCommand>
{
    public SubscribeSeriesCommandValidator()
    {
        RuleFor(x => x.SeriesId).GreaterThan(0).WithMessage("专栏 ID 无效");
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("用户未登录");
        RuleFor(x => x.Remark).MaximumLength(200).WithMessage("备注长度不能超过 200 字");
    }
}
```

#### 第 3 步：编写 CommandHandler 业务逻辑
```csharp
using Content.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Application.CQRS;
using SharedKernel.Application.Exceptions;

namespace Content.Application.Series.Commands;

public sealed class SubscribeSeriesCommandHandler : ICommandHandler<SubscribeSeriesCommand>
{
    private readonly BlogDbContext _dbContext;

    public SubscribeSeriesCommandHandler(BlogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(SubscribeSeriesCommand command, CancellationToken cancellationToken)
    {
        var series = await _dbContext.Series
            .FirstOrDefaultAsync(s => s.Id == command.SeriesId && !s.IsDeleted, cancellationToken);

        if (series == null)
        {
            throw new NotFoundException("Series", command.SeriesId);
        }

        // 业务逻辑实现...
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

#### 第 4 步：在 Controller 中暴露端点
在对应的控制器类上声明细粒度权限守卫与 Swagger 契约：
```csharp
[HttpPost("{id:long}/subscribe")]
[RequirePermission(Permissions.SeriesEdit)] // 权限代码拦截
[ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
public async Task<IActionResult> SubscribeAsync(long id, [FromBody] SubscribeSeriesRequest request, CancellationToken cancellationToken)
{
    var command = new SubscribeSeriesCommand(id, _currentUser.UserId, request.Remark);
    await _mediator.Send(command, cancellationToken);
    return Success("订阅专栏成功");
}
```

---

## 4. 管理后台 (React 19 + Ant Design 5) 新增功能 SOP

管理端前端统一基于 Ant Design 5 深色调与 Axios 封装，新增功能需遵循以下步骤：

### 第 1 步：在 `src/types/index.ts` 声明对齐接口
> **说明**：雪花 ID 超出 JavaScript 安全整数范围（$2^{53} - 1$），字段须声明为 `string` 或 `string | number`，禁止转换为 `Number(id)`。

```typescript
export interface SubscribeSeriesRequest {
  remark?: string
}
```

### 第 2 步：在 `src/api/` 导出服务函数
```typescript
import client from './client'
import type { SubscribeSeriesRequest } from '@/types'

export function subscribeSeries(id: string | number, data: SubscribeSeriesRequest): Promise<string> {
  return client.post(`/admin/series/${id}/subscribe`, data)
}
```

### 第 3 步：在页面中使用 `<PermissionGuard>` 保护按钮
```tsx
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'

<PermissionGuard code={PERMISSIONS.SERIES_EDIT}>
  <Button type="primary" onClick={handleSubscribe}>
    订阅专栏
  </Button>
</PermissionGuard>
```

---

## 5. 展示前台 (Next.js 15 App Router) 新增功能 SOP

### 1. 服务端网络与双通道请求机制
* **服务端数据拉取 (SSR / Server Component)**：直接在 `page.tsx` 中调用统一适配层 `src/lib/api.ts`。Node.js 内部自动直连 `INTERNAL_API_BASE_URL`；
* **浏览器客户端交互 (Client Component)**：客户端只请求相对路径 `/api/*`，由 `next.config.mjs` 中的 `rewrites()` 反向代理转发至后端，避免跨域限制；
* **缓存策略**：`RootLayout` 已全站声明 `export const dynamic = 'force-dynamic'`，用于保证运行期站点配置与最新文章数据即时生效；
  * 子页面**不要**再声明 `export const revalidate` 期望 ISR —— 它会被子布局的 `force-dynamic` 覆盖，容易造成误解；
  * 数据层的秒级缓存请使用 `fetch(..., { next: { revalidate: N } })`（`src/lib/api.ts` 已统一处理）。

### 2. 站点信息的运行期读取

站点信息采用「代码默认值 + 数据库覆盖项」两层结构，避免修改文案需要重新打包镜像：

1. `src/config/site.ts` 的 `defaultSiteConfig` 为完整默认值，`SiteConfig` 类型由它推导；
2. 后台「站点设置」把覆盖项以 JSON 存入 `site_settings` 表（仅保存被修改的字段）；
3. `src/lib/site-config.ts` 的 `getSiteConfig()` 在服务端读取公开接口并深合并到默认值，进程内缓存 60 秒，接口异常时回退默认值；
4. `getSiteConfig()` 读取后会调用 `applySiteConfig()` 写入服务端当前配置；`RootLayout` 再把它注入 `window.__CHAKRA_SITE__`，客户端组件从该对象读取同一份配置（避免水合不一致）。

新增可配置字段时请遵循：

* 字段加入 `defaultSiteConfig` 即可在后台「高级 JSON」中覆盖；若需要表单入口，在 `SiteSettings.tsx` 增加对应表单项；
* 服务端组件直接使用 `siteConfig.xxx`（由 Layout 在渲染前写入）；独立的路由处理器（`sitemap` / `robots` / `feed.xml`）必须先 `await getSiteConfig()`；
* 不要在模块顶层把 `siteConfig` 的值赋给常量（会在配置写入前固化为默认值）；
* 不要依赖构建期 `ARG` 注入站点信息，否则预构建镜像无法直接复用。

### 3. 双主题与设计 Tokens
所有颜色与视觉微调使用 `theme-*` 语义类（如 `text-theme-primary`, `bg-theme-surface`, `border-theme-border`），确保日间暖橙与夜间青蓝主题切换时自适应。禁止在业务组件中手写会被主题影响的固定 Hex 色值。

仅以下两类允许在组件内出现固定色值，且须集中声明、避免散落：

1. **性能敏感的 Canvas / CSS 关键帧动效**（如 `ChakraJutsuCanvas`、`NinjaThemeTransition`）；
2. **主题无关的局部组件**（如始终保持暗色的终端微件、代码块、角色族徽图标），其固定色应集中定义在组件顶部或 `globals.css` 的局部变量中。

---

## 6. 雪花 ID 与数据精度安全准则

系统主键采用 64 位雪花算法（`SnowflakeId`，由 `Yitter.IdGenerator` 生成），数值最大可达到 19 位整数（超过 JS 最大安全整数 $2^{53} - 1$ = $9,007,199,254,740,991$）。

* **后端序列化规则**：已注册 `LongToStringJsonConverter`，所有对外输出的 64 位 ID 在 JSON 报文中均强制为字符串；
* **前端使用规则**：
  * 禁止使用：`Number(id)`、`parseInt(id)`；
  * 规范使用：保持为 `string` 类型，如需类型转换使用 `String(id)`。

---

## 7. 质量门禁与自动化测试规范

任何代码改动在提交前必须通过以下两重门禁：

### 1. 后端零警告构建与全量测试
```bash
# 1. 运行所有单元测试
dotnet test chakra_api_back_end/Chakra.slnx

# 2. 确保零警告零错误（TreatWarningsAsErrors 严格开启）
dotnet build chakra_api_back_end/Chakra.slnx
```

### 2. 前端严格类型检查与生产编译
```bash
# 管理端校验
cd chakra_management_front_end && npm run build

# 展示端校验
cd chakra_front_end && npm run build
```

---

## 8. 代码质量与开发规范

在仓库中进行代码编写与修改时，须遵循以下规范：

1. **测试覆盖**：新增业务逻辑或修复缺陷时，应在 `tests/Chakra.Tests.Unit` 补充对应单元测试，并确保测试全部通过；涉及权限、文件路径、雪花 ID 的改动须补充测试用例；
2. **雪花 ID 精度保护**：前端组件禁止引入 `Number(id)` 或 `parseInt(id)` 转换逻辑，统一使用字符串传递；
3. **管理员权限放行 (Admin Bypass)**：编写命令处理器（CommandHandler）时，涉及文章、专栏、分类的所有权或审核判断，须校验 `if (!command.IsAdmin && ...)`，确保超级管理员对全站资源具备管理权限；
4. **统一响应契约**：Controller 禁止直接返回未包装数据或原生 `Ok(...)`，须调用 `Success(data, message)` 返回 `ApiResult<T>`；
5. **文件路径安全**：处理本地文件操作（保存、读取、删除）时，须调用 `Path.GetFullPath` 校验路径限制在指定根目录内，防止路径遍历（Path Traversal）。
