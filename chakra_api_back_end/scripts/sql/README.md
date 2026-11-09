# 数据库脚本

## 初始化

项目使用 PostgreSQL，数据库基准结构和初始化数据由 [`init.sql`](init.sql) 维护：

在空数据库上初始化执行：

```powershell
psql -d <database> -f scripts/sql/init.sql
```

`init.sql` 支持幂等执行（只创建缺失的表、索引和初始化记录，不会覆盖已有数据）。脚本包含 Content、Identity、Logging 三个模块所需对象；连接字符串指向 `DefaultConnection`。

## 自动结构升级（Database Migrator）

后端服务 (`ApiHost`) 内置了轻量级**启动期自动数据库迁移器**（`DatabaseMigrator`），在服务启动时自动检测并应用未执行的增量迁移脚本。

### 工作机制

1. **版本跟踪表**：系统自动在数据库创建 `__schema_migrations` 记录表，维护已执行的迁移版本号、描述与应用时间。
2. **嵌入式资源**：所有放置在 `scripts/sql/upgrade/*.sql` 的增量脚本在编译期自动嵌入 `ApiHost.dll` 中，拉取预构建 Docker 镜像无需额外挂载 SQL 文件即可自动完成升级。
3. **事务安全性**：每个迁移脚本均在独立的原子事务中执行，执行成功后立即登记版本；若脚本执行失败，将自动回滚事务并阻断应用启动，防止数据库处于中间损坏状态。
4. **幂等与兜底**：除了程序集内置资源外，Migrator 还支持本地目录扫描，方便离线开发与排查。

### 现有升级脚本

| 脚本 | 说明 | 状态 |
|---|---|---|
| [`upgrade/001_site_settings.sql`](upgrade/001_site_settings.sql) | 新增站点设置表与「站点设置」菜单权限（后台可视化配置站名/SEO/文案） | 自动应用 |

### 如何编写新的升级脚本

当需要升级数据库表结构或初始化数据时：
1. 在 `chakra_api_back_end/scripts/sql/upgrade/` 目录下创建以三位数字递增命名的 `.sql` 文件，例如 `002_add_user_bio.sql`。
2. 保持 DDL 语句幂等（尽量使用 `IF NOT EXISTS`、`ON CONFLICT DO NOTHING`）。
3. 重新构建或发布 Docker 镜像；当后端容器启动时，将自动检测到新脚本并执行应用。

### 手动执行方式（可选）

```bash
# Linux / macOS
docker compose exec -T postgres psql -U postgres -d blog < chakra_api_back_end/scripts/sql/upgrade/001_site_settings.sql

# Windows PowerShell
Get-Content chakra_api_back_end/scripts/sql/upgrade/001_site_settings.sql -Raw | docker compose exec -T postgres psql -U postgres -d blog
```
