# 更新日志 (Changelog)

本文件记录 Chakra 博客系统的重要变更，格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## [Unreleased]

### 安全 (Security)

- Hangfire Basic 认证改为常量时间比较，降低口令时序侧探测风险；
- 保留默认示例密钥/口令以维持开箱即用体验，仅在启动时输出安全告警，公网部署前请自行替换。

### 变更 (Changed)

- 权限校验结果增加 30 秒进程内缓存，将管理端单次请求的多次数据库往返收敛为一次；
- 审计日志改为显式 `[SkipAuditLog]` 标记跳过高频统计命令，移除基于命名空间字符串猜测模块的脆弱逻辑；
- 新增创建/更新分类命令的 FluentValidation 校验器，超长输入统一返回 400 而非数据库异常；
- 明确展示端全站继承 RootLayout 的 `force-dynamic` 动态渲染策略，子页面不重复声明 `revalidate`；
- 新增**站点设置**后台页面（`site_settings` 表单 + 高级 JSON），站点信息改为「代码默认值 + 数据库覆盖项」，保存后最迟 60 秒生效，使用预构建镜像无需重新打包；管理端「前台预览」地址同步从站点设置读取；
- 新增 CI 的 lint 与 typecheck 门禁，补齐管理端 `typecheck` 脚本；
- Docker 镜像内 `uploads` 目录权限由 `777` 收敛为 `750`。

### 文档 (Docs)

- 修正 README 快速起步锚点链接与 CI 状态徽章；
- `.env.example` 移除行内注释，统一 Hangfire/JWT 默认值口径，并注明密钥生成方式；
- `SECURITY.md` 补充实际的私密漏洞反馈渠道；
- `DEVELOPMENT.md` 明确缓存策略与主题色值豁免场景，调整测试要求表述；
- 新增 Issue / PR 模板、Dependabot 配置与本更新日志。

## [1.0.0]

首个公开版本。

### 新增 (Added)

- .NET 10 垂直切片 CQRS 后端（MediatR + EF Core 写入 + Dapper 只读查询）；
- Next.js 15 展示前台与管理控制台（React 19 + Vite 6 + Ant Design 5）；
- 雪花 ID 全链路字符串化、RBAC 三级权限、审计日志、媒体存储（Local / S3 双模态）；
- Docker Compose 一键部署与全量 SQL 初始化脚本。
