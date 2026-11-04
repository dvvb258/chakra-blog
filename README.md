# Chakra - 现代化昼夜双主题全栈个人技术博客系统

<div align="center">

```
   ______   __               __                  ____  __            
  / ____/  / /_  ____ _     / /__  _________ _  / __ )/ /___  ____ _ 
 / /      / __ \/ __ `/    / //_/ / ___/ __ `/ / __  / / __ \/ __ `/ 
/ /___   / / / / /_/ /    / ,<   / /  / /_/ / / /_/ / / /_/ / /_/ /  
\____/  /_/ /_/\__,_/____/_/|_| /_/   \__,_/ /_____/_/\____/\__, /   
                   /_____/                                 /____/    
```

**A modern full-stack blogging system built with .NET 10 & Next.js 15.**  
*昼夜双色主题 · .NET 10 垂直切片后端 · Next.js 15 服务端流式渲染*

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js 15](https://img.shields.io/badge/Next.js-15.0-black?style=flat-square&logo=next.js)](https://nextjs.org/)
[![React 19](https://img.shields.io/badge/React-19.0-61dafb?style=flat-square&logo=react)](https://react.dev/)
[![Tailwind CSS](https://img.shields.io/badge/TailwindCSS-3.4-38bdf8?style=flat-square&logo=tailwind-css)](https://tailwindcss.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ed?style=flat-square&logo=docker)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](./LICENSE)

[快速起步](#-极速起步-quick-start) · [部署指南](./DEPLOYMENT.md) · [系统架构](#️-系统架构设计-architecture)

</div>

---

## 🌟 核心特性 (Key Highlights)

- 🎨 **昼夜双色主题与交互体系 (Dual-Mode Theme)**：
  - 支持高对比度日间暖色与夜间冷色双主题平滑切换，包含全屏转场过渡；
  - 基于 HTML5 Canvas 的动态粒子流场渲染引擎，支持多模态动效；
  - 内置深色命令行交互微件，支持基础系统指令与历史命令回溯；
  - 视觉样式与核心业务解耦，提供便捷的主题换肤与视觉定制机制。
- ⚡ **服务端流式渲染 (Streaming SSR)**：
  - 基于 Next.js 15 App Router 与 React Suspense，提供骨架屏加载与服务端网络高速直连。
- 🛡️ **垂直切片后端架构 (Vertical Slice CQRS)**：
  - 基于 ASP.NET Core (.NET 10) + MediatR 实现命令查询职责分离；
  - EF Core 负责领域实体写入，Dapper 提供高性能只读查询直出；
  - 全局 64 位雪花 ID 自动转换为字符串输出，防止前端 JavaScript 精度丢失。
- 🔐 **细粒度 RBAC 权限控制**：
  - 目录、菜单、按钮三级权限模型；
  - 支持动态侧边栏渲染、自定义角色授权与 `<PermissionGuard>` 按钮级鉴权拦截。
- ✍️ **Markdown 创作工作台**：
  - 分屏实时预览、剪贴板截图直接粘贴（Ctrl+V）、外部图片拖拽上传；
  - 字数与阅读耗时自动预估、分类内联快速新建、乐观并发版本锁（Version）。
- 📊 **ECharts 运营数据看板**：
  - 全站访问量、文章总数、阅读总量与专栏系列统计；
  - 近 7 天发文趋势面积图、技术分类分布环形图与最新文章列表。
- 🔌 **开箱即用与弹性降级 (Pluggable SPI)**：
  - **访问量持久化**：全站访问量基于 PostgreSQL 原子持久化，服务重启不丢失数据；Redis 可选启用，未开启时自动降级为单机进程内互斥锁；
  - **存储双模态**：默认采用本地持久化磁盘存储，支持无缝切换至 S3 兼容对象存储（MinIO / 阿里云 OSS / AWS S3 / Cloudflare R2）；
  - **特性探测**：后端提供公共特性探测接口，未开启第三方登录或 AI 时前端自适应显隐入口。
- 🐳 **Docker Compose 一键启动**：
  - 预置 `docker-compose.yml` 与全量 SQL 初始化脚本，一键构建运行全套容器集群。

---

## 🏗️ 系统架构设计 (Architecture)

```
chakra-blog/
├── chakra_front_end/            # 读者公开展示端 (Next.js 15 App Router + React 19 + Tailwind CSS)
├── chakra_api_back_end/          # Web API 后端服务 (.NET 10 + PostgreSQL + Dapper + EF Core 10)
└── chakra_management_front_end/  # 管理控制台 (React 19 + Vite 6 + Ant Design 5 + Zustand 5)
```

| 子工程 | 技术选型 | 默认端口 | 职责定位 |
|---|---|---|---|
| **chakra_front_end** | Next.js 15, React 19, Tailwind CSS, Prism.js | `3000` | 公开文章展现、专栏路线图、分类导航、昼夜主题切换、统计上报 |
| **chakra_api_back_end** | ASP.NET Core (.NET 10), PostgreSQL, Dapper, MediatR, Hangfire | `5000` | 核心 WebAPI、内容管理、RBAC 鉴权、审计日志、媒体存储服务 |
| **chakra_management_front_end** | React 19, Vite 6, TypeScript, Ant Design 5, Zustand 5 | `3101` | 控制台仪表盘、Markdown 创作工作台、分类维护、系统日志、权限管理 |

---

## 🚀 快速起步 (Quick Start)

### 方式一：Docker Compose 容器化部署（生产推荐）

```bash
# 1. 复制环境变量配置文件（可选，默认配置开箱即用）
cp .env.example .env

# 2. 启动服务（默认自动拉取预构建标准镜像）
docker compose up -d

# 注：若对前端或后端源码进行了二次开发修改，可通过 --build 参数在本地重新构建：
# docker compose up -d --build
```

启动完成后即可访问：
- **博客前台**：`http://localhost:3000`
- **管理后台**：`http://localhost:3101`
- **后端 Swagger 文档**：`http://localhost:5000/swagger`

**初始管理员凭据**：
- 用户名：`admin`
- 密码：`AdminPassword123!`
*(登录后请立即前往【个人中心】修改默认密码)*

更多部署说明与 Nginx 反代配置请参阅 [DEPLOYMENT.md](./DEPLOYMENT.md)。

---

### 方式二：本地研发模式启动

详细本地开发步骤请参阅 [DEPLOYMENT.md#方式二本地研发模式-local-development](./DEPLOYMENT.md)。

---

## 📌 项目说明与维护声明 (Maintenance Status)

- **定位**：本项目主要作为作者个人的全栈技术探索（.NET 10 + Next.js 15 + React 19）与个人博客系统，供技术交流与建站模板参考；
- **维护模式**：项目按“现状（As-Is）”开源。由于作者平时个人精力有限，**不承诺固定的更新频率、不承诺 SLA，也无法保证能够及时响应 Issue、合并 PR 或实现特定功能定制**；
- **欢迎自由 Fork**：基于宽松的 [MIT 许可证](./LICENSE)，非常欢迎您自由 Fork 本项目并根据自己的审美与业务需求随意修改、拓展或自建部署，无需等待上游合并。

---

## 📄 开源许可证

本项目基于 [MIT 许可证](./LICENSE) 开源。
