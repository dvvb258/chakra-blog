# 数据库脚本

## 初始化

项目使用 PostgreSQL，数据库完整基准结构和初始化数据统一由 [`init.sql`](init.sql) 维护：

在空数据库上初始化执行：

```powershell
psql -d <database> -f scripts/sql/init.sql
```

`init.sql` 支持幂等执行（包含 Content、Identity、Logging 模块以及站点设置、权限菜单所需的全部表、索引和初始化记录，不会覆盖已有数据）。

## 自动结构升级（Database Migrator）

后端服务 (`ApiHost`) 内置了轻量级**启动期自动数据库迁移器**（`DatabaseMigrator`），在服务启动时自动检测并应用未执行的增量迁移脚本。

### 工作机制

1. **版本跟踪表**：系统自动在数据库创建 `__schema_migrations` 记录表，维护已执行的迁移版本号、描述与应用时间。
2. **嵌入式资源**：所有放置在 `scripts/sql/upgrade/*.sql` 的增量脚本在编译期自动嵌入 `ApiHost.dll` 中，拉取预构建 Docker 镜像无需额外挂载 SQL 文件即可自动完成升级。
3. **事务安全性**：每个迁移脚本均在独立的原子事务中执行，执行成功后立即登记版本；若脚本执行失败，将自动回滚事务并阻断应用启动，防止数据库处于中间损坏状态。
4. **幂等与兜底**：除了程序集内置资源外，Migrator 还支持本地目录扫描，方便离线开发与排查。

### 当前状态与后续版本迭代

当前项目处于开发基线阶段，所有表结构统一维护于 [`init.sql`](init.sql) 中，因此 `upgrade/` 目录为空。当未来发布新版本需要增量升级数据库时：

1. 在 `chakra_api_back_end/scripts/sql/upgrade/` 目录下创建以三位数字递增命名的 `.sql` 文件，例如 `002_add_user_bio.sql`。
2. 保持 DDL 语句幂等（尽量使用 `IF NOT EXISTS`、`ON CONFLICT DO NOTHING`）。
3. 重新构建或发布 Docker 镜像；当后端容器启动时，`DatabaseMigrator` 将自动检测到新脚本并按序应用。
