# 数据库脚本

## 初始化

项目使用 PostgreSQL，数据库结构和初始化数据由 [`init.sql`](init.sql) 显式维护，不使用 EF Core Code First、Migration、`EnsureCreated` 或运行时 Seeder。

在空数据库上执行：

```powershell
psql -d <database> -f scripts/sql/init.sql
```

`init.sql` 可重复执行，但只创建缺失的表、索引和初始化记录，不会覆盖已有数据。脚本包含 Content、Identity、Logging 三个模块所需对象；三个模块连接键可以指向同一个数据库：

- `DefaultConnection`：Content
- `UserConnection`：Identity
- `LogConnection`：Logging

## 结构升级

初始化脚本不是数据库升级工具。已部署数据库发生结构变更时，应在此目录新增带顺序编号的显式升级脚本，例如 `001_add_token_hash.sql`，经审核后人工执行。升级脚本不得在应用启动时自动运行。

现有升级脚本：

| 脚本 | 说明 |
|---|---|
| [`upgrade/001_site_settings.sql`](upgrade/001_site_settings.sql) | 新增站点设置表与「站点设置」菜单权限（后台可视化配置站名/SEO/文案） |

执行方式（Docker Compose 部署）：

```bash
# Linux / macOS
docker compose exec -T postgres psql -U postgres -d blog < chakra_api_back_end/scripts/sql/upgrade/001_site_settings.sql

# Windows PowerShell
Get-Content chakra_api_back_end/scripts/sql/upgrade/001_site_settings.sql -Raw | docker compose exec -T postgres psql -U postgres -d blog
```
