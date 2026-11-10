# Skill: 数据库基线与升级迁移规范

本技能指导 AI 编码 Agent 或研发人员在对 Chakra 系统的 PostgreSQL 数据库表结构、字段、索引进行修改时，遵循**版本状态与演进闭环规范**。

---

## 1. 核心准则：预发布阶段 vs 发布阶段

| 项目生命周期阶段 | 结构变更操作准则 | 迁移脚本约定 |
|---|---|---|
| **当前：未正式发布阶段 (Pre-Release)** | 所有结构变更、新表、新字段、新索引**直接维护在基线 `init.sql`** 中。 | **禁止创建零散的 `upgrade/` 增量脚本**，保持 `init.sql` 为最新全量 Single Source of Truth。 |
| **后续：已发布生产迭代阶段 (Post-Release)** | 生产数据库已存在线上用户真实数据，不能轻易删库重建。 | 在 `upgrade/` 目录按照 `001_xxx.sql`、`002_xxx.sql` 版本命名创建增量脚本，由 `DatabaseMigrator` 在系统启动时自动应用。 |

---

## 2. 字段与索引设计规范

1. **命名规范**：
   - 表名全小写、下划线复数（如 `articles`、`audit_logs`、`site_settings`）；
   - 列名全小写、下划线（如 `is_pinned`、`reading_time`、`created_at`）；
   - 索引命名：`ix_{table}_{column}`（如 `ix_articles_is_pinned`）。
2. **主键与雪花 ID**：
   - 业务实体主键统一为 `BIGINT PRIMARY KEY`，严禁使用自增序列（`SERIAL` / `BIGSERIAL`）；
   - 外键字段类型同样为 `BIGINT`，关联关系按需建立索引。
3. **软删除与版本号**：
   - 业务数据表标配：
     - `is_del BOOLEAN NOT NULL DEFAULT FALSE`
     - `version INT NOT NULL DEFAULT 1`
     - `created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP`
     - `updated_at TIMESTAMPTZ`
4. **历史数据膨胀防控**：
   - 高频增长日志表（如 `audit_logs`、`exception_logs`）必须在 `created_at` 字段建立倒序索引（`CREATE INDEX ix_... ON ... (created_at DESC)`），以支持后台清理任务的高效无锁删除。
