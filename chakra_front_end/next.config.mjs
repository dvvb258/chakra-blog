/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  output: 'standalone',
  images: {
    // 启用 sharp 图片优化（自动压缩、转 WebP/AVIF、按需裁剪）
    formats: ['image/avif', 'image/webp'],
    remotePatterns: [
      {
        protocol: 'https',
        hostname: '**',
      },
      // 本地研发/内部调试允许 HTTP，公网外部资源强制 HTTPS 以规避 SSRF
      {
        protocol: 'http',
        hostname: 'localhost',
      },
      {
        protocol: 'http',
        hostname: '127.0.0.1',
      },
    ],
  },
  // 反向代理：客户端请求 /api/* 与 /uploads/* 时，Next.js 在集群/容器内网直接转发给 .NET WebAPI
  async rewrites() {
    const internalApi =
      process.env.INTERNAL_API_BASE_URL ||
      'http://localhost:5000/api'
    const apiHostBase = internalApi.replace(/\/api\/?$/, '').replace(/\/$/, '')

    return [
      {
        source: '/api/:path*',
        destination: `${internalApi.replace(/\/$/, '')}/:path*`,
      },
      {
        source: '/uploads/:path*',
        destination: `${apiHostBase}/uploads/:path*`,
      },
    ]
  },
}

export default nextConfig
