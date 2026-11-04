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

### 1. 配置环境变量
```bash
cp .env.example .env
# 生产环境安全建议：请修改 .env 中的：
# 1. JWT_SECRET（认证签名强密钥）
# 2. POSTGRES_PASSWORD（数据库密码）
# 3. HANGFIRE_PASSWORD（后台任务控制面板 Basic 认证密码）
```

### 2. 构建并启动服务
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
  - *(首次登录后请前往【个人中心】修改默认密码)*
- **Hangfire 控制台凭据**：
  - 用户名：`admin` (可通过 `HANGFIRE_USERNAME` 配置)
  - 密码：可通过 `HANGFIRE_PASSWORD` 配置（默认 `AdminPassword123!`，生产请务必覆盖）

---

## 方式二：本地研发模式 (Local Development)

本地研发模式下，子工程已预置开箱即用开发配置，**无需创建或修改任何 `.env` 文件**。

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

### 为什么推荐“容器内网运行 + 宿主机 Nginx 网关”？
- **域名优雅分流**：前台（3000）、管理后台（3101）和 API（5000）可在单一服务器上通过二级域名统一使用标准 80/443 端口，无需对外暴露奇怪的端口号；
- **统一 HTTPS/SSL 证书管理**：由宿主机 Nginx 集中处理 TLS 终结与 Certbot 自动续期，无需将证书文件分散拷贝至各个 Docker 容器内部；
- **避免端口冲突**：若服务器已安装宝塔面板、1Panel 或已有 Web 站点，容器不会强占 80/443 端口，兼容性最佳。

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

系统采用低外部依赖架构，默认使用单机文件存储与数据库统计，零外部依赖即可完整运行。如需接入外部云服务，直接在根目录 `.env` 中按需取消对应注释并填入凭据即可，无需修改任何子目录代码或 Dockerfile：

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

### 2. 启用 Redis 分布式缓存与分布式任务锁
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

## 常用运维命令

```bash
# 查看所有容器运行状态
docker compose ps

# 查看后端实时日志
docker compose logs -f blogapi

# 查看全部容器日志
docker compose logs -f

# 停止全套服务（保留数据库数据与已上传图片）
docker compose down

# 彻底重置并重新初始化数据库（⚠️ 危险：清空全部业务数据与数据卷，重新执行 init.sql）
docker compose down -v

# 代码更新后重新构建并启动
git pull
docker compose up -d --build
```

### 💡 数据库初始化提示 (`init.sql` 与 `-v` 参数)
PostgreSQL 官方镜像机制决定：`init.sql` **仅在数据库卷初次创建（目录为空）时自动执行一次**。
- **日常重启 / 更新代码**：执行 `docker compose down && docker compose up -d`，PostgreSQL 会保留已有数据，不会重复执行 `init.sql`；
- **重置数据库**：如果您修改了 `init.sql`，或者在测试阶段希望**彻底清空旧数据重新建表**，必须执行 `docker compose down -v`（销毁 volume 数据卷），随后再次 `docker compose up -d` 即可自动重新执行 `init.sql`。
