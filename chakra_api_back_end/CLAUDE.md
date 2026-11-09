# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概览

.NET 10 模块化 Web API 后端，为博客系统提供核心数据与内容服务：
- **chakra_api_back_end**（本仓库）— ASP.NET Core Web API 后端（.NET 10）
- **chakra_front_end** — 公开博客前端（Next.js 15 App Router）
- **chakra_management_front_end** — 博客管理后台（React 19 + Ant Design 5 + Zustand 5）

架构设计：采用**垂直切片架构（Vertical Slice Architecture）**与 **CQRS 模式**，写操作采用 EF Core 维护领域实体，读操作采用 Dapper 进行只读查询，结合雪花 ID、Hangfire 后台调度与非阻塞异步审计日志。

## 构建与运行

```bash
dotnet restore Chakra.slnx
dotnet build Chakra.slnx            # 零警告零错误构建（TreatWarningsAsErrors 开启）
dotnet run --project src/ApiHost/ApiHost.csproj
```

## 解决方案结构 (Chakra.slnx)

```
chakra_api_back_end/
├── src/
│   ├── ApiHost/        # Web API 宿主程序（中间件、Swagger、路由挂载、JWT 鉴权）
│   ├── SharedKernel/   # 跨模块通用基类（Entity、CQRS 接口、SnowflakeId、ApiResult）
│   └── Modules/
│       ├── Content/    # 内容业务模块（文章/分类/系列/统计/AI 文章生成/媒体存储）
│       ├── Identity/   # 身份认证与权限模块（用户/角色/菜单权限/JWT 双 Token/OAuth）
│       └── Logging/    # 系统日志模块（异步内存通道/审计日志/全局未处理异常日志）
```

---

## Web API 设计规范

### 1. 路由与 URL 命名规范
- **全小写与连字符**：URL 路径全小写，多单词采用 kebab-case 连字符（如 `/api/auth/my-menus`），禁止大小写混用或驼峰命名。
- **RESTful 资源命名**：URI 表示资源名词，采用复数形式（如 `/api/articles`、`/api/categories`）。
- **作用域前缀隔离**：
  - `/api/...`：面向读者和展示端的公开/只读接口。
  - `/api/auth/...`：身份认证、当前登录态个人中心与第三方登录接口。
  - `/api/admin/...`：管理控制台接口，全部需要 Bearer Token 鉴权。
- **HTTP 谓词标准**：
  - `GET`：只读查询。
  - `POST`：新增资源或非幂等操作（登录、AI 生成等）。
  - `PUT`：全量更新资源。
  - `PATCH`：部分更新资源。
  - `DELETE`：软删除或解除绑定。

### 2. 统一响应契约规范 (`ApiResult<T>`)
所有 Controller Action 必须返回统一的 JSON 数据结构：
```json
{
  "code": 200,
  "message": "操作成功",
  "data": { ... },
  "traceId": "0HN123456789A:00000001",
  "timestamp": "2026-03-30T12:00:00Z"
}
```
- **成功响应**：统一调用基类 `Success(data)` 或 `Success(data, message)`，`code` 固定为 200。
- **分页列表响应**：分页查询 `data` 统一包装为通用分页模型 `PagedResult<T>`：
  ```json
  {
    "items": [ ... ],
    "total": 100,
    "pageIndex": 1,
    "pageSize": 10,
    "totalPages": 10
  }
  ```

### 3. 错误处理与状态码规范
- **业务异常规范**：业务校验不通过时显式抛出 `BusinessException(message, statusCode, errorCode)`，由全局异常中间件统一捕获并转换为规范的错误响应。
- **HTTP 状态码标准**：
  - `200 OK`：操作成功。
  - `400 Bad Request`：参数校验不通过、业务校验不合法（如名称重复、已收录文章不允许删除等）。
  - `401 Unauthorized`：未携带 Token、Token 已过期或签名无效。
  - `403 Forbidden`：权限不足。
  - `404 Not Found`：目标资源不存在或已被软删除。
  - `409 Conflict`：并发版本冲突（乐观锁 Version 不匹配）。
  - `500 Internal Server Error`：系统未捕获的内部异常，中间件记录日志并隐藏内部敏感堆栈。

### 4. 数据输入校验与契约规范
- **雪花 ID 防截断**：实体 ID、外键 ID、DTO 中的 64 位长整型雪花 ID，在 JSON 序列化中必须强制转换为字符串输出（通过 `LongToStringJsonConverter`），防止前端 JavaScript 精度丢失。
- **参数验证管道**：Command 与 Query 通过 FluentValidation 编写验证规则，由 MediatR `ValidationBehavior` 管道统一拦截校验。
- **时间标准**：全局统一序列化为标准北京时间或带时区的 ISO 8601 格式，数据库统一存 UTC 时间。

### 5. RBAC 细粒度权限控制规范（三级资源树模型）
- **三级资源树结构**（`menus.type`）：
  - `1 - 目录 (Directory)`：侧边栏折叠项。
  - `2 - 菜单/页面 (Menu)`：具有前端路由 `path` 的页面。
  - `3 - 按钮/操作权限 (Button)`：页面内具体操作按钮与后端接口对应权限。
- **权限元数据字典**：
  - 权限常量在 `SharedKernel.Constants.Permissions` 中定义。
  - 暴露 `GET /api/admin/menus/permissions-metadata` 接口提供系统权限字典，前端配置时通过下拉联想输入。
- **基于权限代码的策略鉴权**：
  - 控制器 Action 统一使用 `[RequirePermission(...)]` 守卫，支持多权限 OR 判定。
  - 超级管理员拥有 `*` 通配权限；自定义角色通过 `RoleMenu` 关联动态赋权。
  - 前端配合 `<PermissionGuard code="...">` 组件实现按钮级显隐与禁用。

### 6. 第三方供应商可插拔抽象规范 (SPI / Provider Pattern)
- **对象存储**：依赖抽象接口 `IStorageService`。实现类按配置 `Storage:Provider`（`Local` / `S3`）进行动态注册，默认支持本地磁盘存储。
- **AI 服务**：依赖标准 `Microsoft.Extensions.AI.IChatClient`，模型名与端点外部可配置。
- **OAuth 第三方登录**：抽象标准接口，避免与特定平台字段强耦合。

### 7. OpenAPI / Swagger 规范
- 公开控制器类、Action 方法、请求与响应 DTO 包含中文 XML 注释。
- Action 声明 `[ProducesResponseType(typeof(ApiResult<T>), StatusCodes.Status200OK)]`。
- 启用 Swagger JWT 授权配置。

---

## 核心架构约定

### 1. 垂直切片 CQRS（读写分离）与数据持久化
- **写操作 (Command)**：
  - 直接依赖 EF Core `DbContext` 操作领域实体。
  - 采用 **`EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`)** 自动蛇形列名映射。
  - 全局约定雪花 ID（`ValueGeneratedNever()`）与软删除列（`is_del`）。
- **读操作 (Query)**：
  - `Content` 模块 QueryHandler 中直接注入 `IContentDbConnectionFactory`，使用 **Dapper** 执行 SQL 查询直出 DTO；
  - `Identity` 模块 QueryHandler 中直接注入 `IdentityDbContext`，使用 **EF Core `AsNoTracking()`** 执行只读查询直出 DTO。

### 2. 领域模型与实体自洽
- 实体继承自 `SharedKernel.Domain.Entity`（包含 `Id`, `CreatedAt`, `UpdatedAt`, `Version`, `IsDeleted`, `DeletedAt`）。
- 主键采用雪花算法（SnowflakeId）。
- **并发乐观锁机制**：在 `SaveChangesAsync` 中对 `Modified` 实体自动递增 `Version`；文章与专栏更新支持前端传入 `version` 进行版本比对，版本冲突返回 409。

---

## 配置键速查

| 配置键 | 用途说明 |
|--------|------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL 业务主库 |
| `ConnectionStrings:Redis` | 后台定时任务分布式互斥锁与限额控制（可选；未配置时降级为进程内 SemaphoreSlim 互斥锁） |
| `Redis:Enabled` | Redis 功能开关（布尔值） |
| `Storage:Provider` | 媒体存储模式：`Local`（默认本地持久卷）或 `S3`（云端/MinIO） |
| `Storage:Local:UploadDir` | 本地存储物理保存子路径（默认 `uploads`） |
| `Storage:Local:PublicUrl` | 本地静态文件对外访问基地址（若为空则按当前请求动态拼接） |
| `Identity:GitHub:Enabled` | GitHub OAuth 快捷登录总开关 |
| `AISettings:AutoGenerateEnabled` | 是否开启后台 AI 定时撰写调度（默认 false） |
| `AISettings` | AI 文章生成配置（Key / Endpoint / ModelName） |
| `SnowflakeId:WorkerId` | 全局雪花 ID 节点编号（默认 1） |
| `Hangfire:Dashboard:Username` | Hangfire 控制面板登录用户名（默认 admin） |
| `Hangfire:Dashboard:Password` | Hangfire 控制面板登录密码 |
| `JwtSettings:Secret` | JWT 签名密钥（至少 32 字符） |
| `JwtSettings:Issuer` | JWT 颁发者（默认 chakra_api） |
| `JwtSettings:Audience` | JWT 受众（默认 chakra_client） |
| `Storage:S3:Endpoint` (兼容 `S3Storage:ServiceUrl`) | S3 兼容服务端点 |
| `Storage:S3:AccessKey` (兼容 `S3Storage:AccessKey`) | S3 AccessKey |
| `Storage:S3:SecretKey` (兼容 `S3Storage:SecretKey`) | S3 SecretKey |
| `Storage:S3:Bucket` (兼容 `S3Storage:BucketName`) | S3 存储桶名称 |
| `Storage:S3:PublicUrl` (兼容 `S3Storage:PublicUrl`) | 公开访问外链/CDN 域名 |
| `Storage:S3:ForcePathStyle` (兼容 `S3Storage:ForcePathStyle`) | 是否强制路径样式（默认 true） |

---

## 系统设计与可扩展性原则

1. **Redis 可选与单机降级**：
   - 定时任务调度锁与文章源抢占锁在未启用 Redis 时自动回退为单机进程内互斥锁（`SemaphoreSlim`）。
   - 全站访问量统计基于 PostgreSQL 原生原子操作持久化，无需额外缓存依赖，单机低配环境平稳运行。
2. **零外部存储依赖**：
   - 内置 `LocalStorageService` 实现 `IStorageService` 接口，默认将图片保存至本地磁盘（`uploads/`），并通过静态文件中间件直接提供访问。
   - 图片格式自适应转码由展示端 Next.js 15 原生 Sharp 引擎接管。
3. **功能软开关与特性探测**：
   - GitHub OAuth 未配置或关闭时，接口返回规范 400 提示而非 500 异常。
   - 提供公共特性探测接口 `GET /api/system/public-config`，向前端告知当前系统开启的能力（`features: { githubLogin: bool, aiEnabled: bool, storageProvider: string }`）。
   - AI 定时撰写任务仅在 `AutoGenerateEnabled == true` 且配置了有效 API Key 时挂载至 Hangfire。
