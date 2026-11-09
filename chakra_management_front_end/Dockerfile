# ============================================================
# Stage 1: 构建阶段
# ============================================================
FROM node:20-alpine AS builder
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

COPY . .

# 生产环境构建打包（生成轻量分包静态资源）
ENV NODE_ENV=production
RUN npm run build

# ============================================================
# Stage 2: 生产 Nginx 托管阶段（极小体积 ~20MB，极低内存消耗）
# ============================================================
FROM nginx:alpine AS runner

# 默认后端上游地址（在 Docker Compose 网络中为 http://blogapi:8080；支持 docker run -e API_UPSTREAM=... 自定义覆盖）
ENV API_UPSTREAM=http://blogapi:8080

# 复制自定义 Nginx 模板（利用 Nginx 官方镜像原生内置的 envsubst 机制在启动时动态渲染）
COPY nginx.conf /etc/nginx/templates/default.conf.template

# 复制前端编译打包产物
COPY --from=builder /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
