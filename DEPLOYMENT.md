# 部署与运维指南 (Deployment Guide)

本项目支持 **Docker Compose 一键容器化部署（推荐生产）** 与 **本地多端源码独立研发** 两种运行模式。

---

## 端口与数据卷速查

| 服务名称 | 容器内端口 | 默认宿主机端口 | 对应环境变量 | 说明 |
|---|---|---|---|---|
| **blog** (展示端) | 3000 | `3000` | `BLOG_PORT` | Next.js 15 读者公开前端 |
| **admin** (管理端) | 80 | `3101` | `ADMIN_PORT` | React 19 + Nginx 管理控制台 |
| **blogapi** (后端 API) | 8080 | `5000` | `API_PORT` | .NET 10 WebAPI 核心服务 |
| **postgres** (数据库) | 5432 | `5432` | `POSTGRES_PORT` | PostgreSQL 16 数据库 |

**持久化数据卷**：
- `postgres_data`：PostgreSQL 业务数据；
- `uploads_data`：本地磁盘存储的博文封面与正文插图文件。

---

## 方式一：Docker Compose 一键部署（生产推荐）

### 1. 配置环境变量（可选）
```bash
cp .env.example .env
# 生产环境安全建议：修改 .env 中的以下配置：
# 1. JWT_SECRET（认证签名强密钥，至少 32 字符；生成方式：openssl rand -base64 48）
# 2. POSTGRES_PASSWORD（数据库密码）
# 3. HANGFIRE_PASSWORD（后台任务控制面板 Basic 认证密码）
```

### 2. 启动服务（提供两种运行模式）

#### 模式 A：预构建镜像模式（生产推荐）
直接拉取官方标准镜像运行，无需在宿主机安装开发编译环境：
```bash
docker compose up -d
```

#### 模式 B：本地源码构建模式（二次开发）
站点基础信息（站名、SEO、文案等）可在管理后台「站点设置」中在线维护；当修改前端源码、UI 组件或后端业务代码时，需传入 `--build` 参数在本地重新构建镜像：
```bash
docker compose up -d --build
```

### 3. 服务端点与凭据
- **博客展示端**：`http://服务器IP:3000`
- **管理后台**：`http://服务器IP:3101`
- **后端 Swagger 文档**：`http://服务器IP:5000/swagger`
- **Hangfire 调度看板**：`http://服务器IP:5000/hangfire`
- **系统健康检查**：`http://服务器IP:5000/health`
- **初始超级管理员凭据**：
  - 用户名：`admin`
  - 初始密码：`AdminPassword123!`
  - *(首次登录后请前往「个人中心」修改密码)*
- **Hangfire 控制台凭据**：
  - 用户名：`admin` (可通过 `HANGFIRE_USERNAME` 配置)
  - 密码：可通过 `HANGFIRE_PASSWORD` 配置（默认 `AdminPassword123!`，生产环境须修改）

---

## 方式二：本地研发模式 (Local Development)

本地研发模式下各子工程已配置默认连接参数，无需创建或修改 `.env` 文件。

### 1. 启动本地数据库
```bash
docker run -d --name local-postgres \
  -e POSTGRES_DB=blog \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -v $(pwd)/chakra_api_back_end/scripts/sql/init.sql:/docker-entrypoint-initdb.d/01_init.sql:ro \
  postgres:16-alpine
```

### 2. 启动后端 WebAPI (.NET 10)
```bash
cd chakra_api_back_end
dotnet restore Chakra.slnx
dotnet run --project src/ApiHost/ApiHost.csproj
# 默认监听 http://localhost:5000
```

### 3. 启动管理后台 (React 19 + Vite 6)
```bash
cd chakra_management_front_end
npm install
npm run dev
# 默认监听 http://localhost:3101
```

### 4. 启动展示前台 (Next.js 15 App Router)
```bash
cd chakra_front_end
npm install
npm run dev
# 默认监听 http://localhost:3000
```

---

## 生产域名与 Nginx 反向代理配置

### 架构说明：容器内网运行 + 宿主机 Nginx 反向代理

推荐通过宿主机 Nginx 统一反向代理容器服务：
- **多子域名路由与端口收敛**：前台（3000）、管理后台（3101）与 API（5000）通过独立二级域名映射至标准 80/443 端口，无需对外暴露非常规端口；
- **集中化 TLS/SSL 证书管理**：由宿主机 Nginx 集中处理 TLS 终结与证书续期，避免将证书文件复制至各个容器内部；
- **端口隔离**：容器服务不直接占用宿主机 80/443 端口，避免与现有 Web 站点或管理面板冲突。

若在云服务器上使用独立主域名对外提供服务，推荐在宿主机 Nginx（或宝塔 / 1Panel 反代）中配置如下规则：

### 1. 博客展示端与媒体反代 (`blog.yourdomain.com`)
```nginx
server {
    listen 80;
    server_name blog.yourdomain.com;

    # 媒体文件：反代至后端容器（端口 5000）
    location /uploads/ {
        proxy_pass http://127.0.0.1:5000/uploads/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 前台页面与客户端 API 代理：转发至展示端容器（端口 3000）
    location / {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 2. 管理后台反代 (`admin.yourdomain.com`)
```nginx
server {
    listen 80;
    server_name admin.yourdomain.com;

    client_max_body_size 50m;

    location / {
        proxy_pass http://127.0.0.1:3101;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 3. HTTPS / SSL 生产标准配置参考 (Let's Encrypt / Certbot)
```nginx
# HTTP 自动 301 重定向至 HTTPS
server {
    listen 80;
    server_name blog.yourdomain.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name blog.yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/blog.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/blog.yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    location /uploads/ {
        proxy_pass http://127.0.0.1:5000/uploads/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
    }

    location / {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
    }
}
```

---

## 可选扩展功能按需配置

系统默认采用本地磁盘存储与数据库内统计，无需依赖第三方云服务即可运行。若需接入外部云服务，直接在根目录 `.env` 中按需配置对应参数即可：

### 1. 接入 S3 兼容对象存储 (MinIO / 阿里云 OSS / AWS S3 / Cloudflare R2)
在 `.env` 中设置：
```env
STORAGE_PROVIDER=S3
S3_ENDPOINT=https://s3.amazonaws.com
S3_BUCKET=your-bucket-name
S3_ACCESS_KEY=YOUR_ACCESS_KEY
S3_SECRET_KEY=YOUR_SECRET_KEY
S3_PUBLIC_URL=https://cdn.yourdomain.com
```

### 2. 启用 Redis 分布式任务锁与调度限额 (可选)
在 `.env` 中设置：
```env
REDIS_ENABLED=true
REDIS_CONNECTION_STRING=redis:6379,password=,defaultDatabase=0
```

### 3. 启用 GitHub OAuth 快捷登录
在 GitHub Developer Settings 创建 OAuth App，Callback URL 填写 `http(s)://你的管理后台域名/login`，在 `.env` 中设置：
```env
GITHUB_ENABLED=true
GITHUB_CLIENT_ID=YOUR_CLIENT_ID
GITHUB_CLIENT_SECRET=YOUR_CLIENT_SECRET
GITHUB_REDIRECT_URI=https://admin.yourdomain.com/login
```

### 4. 启用 AI 文章智能生成定时任务与自定义数据源
在 `.env` 中设置：
```env
AI_AUTOGEN_ENABLED=true
AI_KEY=sk-...
AI_ENDPOINT=https://api.openai.com/v1
AI_MODEL_NAME=gpt-4o-mini
# 可选：指定 AI 参考抓取的技术文章 RSS 订阅源（多个源使用英文逗号分隔）
AI_RSS_SOURCES=https://devblogs.microsoft.com/dotnet/feed/,https://devblogs.microsoft.com/aspnet/feed/
```

### 5. 自定义跨域域名白名单 (CORS)
系统默认已将 `NEXT_PUBLIC_SITE_URL` 及本地端口（3000/3101）自动加入跨域白名单。若管理端或第三方客户端使用其他独立公网域名，在 `.env` 中设置：
```env
CORS_ALLOWED_ORIGINS=https://myblog.com,https://admin.myblog.com
```

---

## 站点信息配置（管理后台可视化，无需重新打包）

站名、标题、描述、关键词、作者资料、社交链接、页脚文案等，均可在 **管理后台 →「站点设置」** 中可视化编辑：

- **常用字段表单**：站名、品牌名、标题、描述、关键词、站点地址、头像/默认封面、作者副标题与简介、首屏标语、页脚格言与版权、GitHub/邮箱；
- **高级 JSON**：可覆盖首页按钮文案、关于页技术栈、角色设定等任意层级字段（仅需提交要覆盖的字段）；
- **保存后生效**：配置以 JSON 覆盖项形式存入 `site_settings` 表，前台服务端缓存 60 秒，最迟 1 分钟内生效，**无需重新打包或重启容器**；
- **留空即默认**：未覆盖的字段自动回落 `chakra_front_end/src/config/site.ts` 中的默认值；点击「恢复默认」可清空全部覆盖；
- **站点地址一致性**：后台填写的「站点地址」用于 SEO / RSS / Sitemap，后端 CORS 白名单仍读取 `.env` 的 `NEXT_PUBLIC_SITE_URL`，两者建议保持一致。

### 增量数据库升级机制

后端内置启动期数据库迁移器（`DatabaseMigrator`）：
- 服务启动时自动检测并按序执行未应用的增量 SQL 脚本（如 `001_site_settings.sql`），并在 `__schema_migrations` 表中记录迁移版本；
- 镜像更新重启后自动完成数据库结构同步，无需手动执行 SQL 脚本；
- 如需离线排查或手动执行，可通过以下命令应用：

```bash
# Linux / macOS (可选手动执行)
docker compose exec -T postgres psql -U postgres -d blog < chakra_api_back_end/scripts/sql/upgrade/001_site_settings.sql

# Windows PowerShell (可选手动执行)
Get-Content chakra_api_back_end/scripts/sql/upgrade/001_site_settings.sql -Raw | docker compose exec -T postgres psql -U postgres -d blog
```

脚本会创建 `site_settings` 表，并补充「站点设置」菜单与权限项（可重复执行）。

> 说明：`INTERNAL_API_BASE_URL`（`/api` 反向代理目标）属于构建期规则，预构建镜像默认指向 `http://blogapi:8080/api`；若后端容器名不同，需重新构建或在前置 Nginx 层改写。

---

## 常用运维命令

```bash
# 查看所有容器运行状态
docker compose ps

# 查看后端实时日志
docker compose logs -f blogapi

# 查看全部容器日志
docker compose logs -f

# 停止容器服务（保留数据库数据与持久化文件）
docker compose down

# 停止容器并删除持久化数据卷（清空全部数据库数据，重新执行 init.sql）
docker compose down -v

# 代码更新后重新构建并启动
git pull
docker compose up -d --build
```

### 数据库初始化与重置机制

PostgreSQL 官方容器机制：`/docker-entrypoint-initdb.d/` 目录下的 `init.sql` **仅在数据卷初次创建（目录为空）时执行一次**。
- **日常更新与重启**：执行 `docker compose down && docker compose up -d`，PostgreSQL 保留已有数据，不重复执行 `init.sql`；
- **重置数据库**：若修改了 `init.sql` 或需重置数据库环境，须执行 `docker compose down -v`（删除持久化数据卷），再次启动时将自动重新执行 `init.sql` 进行初始化。
