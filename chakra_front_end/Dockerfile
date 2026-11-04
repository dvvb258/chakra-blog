# ============================================================
# Stage 1: 依赖安装阶段
# ============================================================
FROM node:20-alpine AS deps
RUN apk add --no-cache libc6-compat
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# ============================================================
# Stage 2: 构建阶段
# ============================================================
FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# 禁用 Next.js 遥测以提高构建速度
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_ENV=production

# 传入构建期参数（用于 Next.js 静态内联优化与 rewrites 反向代理规则生成）
ARG NEXT_PUBLIC_SITE_URL=http://localhost:3000
ENV NEXT_PUBLIC_SITE_URL=$NEXT_PUBLIC_SITE_URL
ARG INTERNAL_API_BASE_URL=http://blogapi:8080/api
ENV INTERNAL_API_BASE_URL=$INTERNAL_API_BASE_URL

RUN npm run build

# ============================================================
# Stage 3: 生产运行时镜像
# ============================================================
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

# 创建独立非 root 用户提升安全
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

# 复制公开静态资源
COPY --from=builder /app/public ./public

# 自动利用 output: 'standalone' 仅打包极小体积运行时依赖
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 3000

CMD ["node", "server.js"]
