import { Category } from '@/types'

/**
 * 站点文章技术分类定义
 */
export const CATEGORIES: Category[] = [
  { key: 'all', label: '全部文章', icon: 'Sparkles', count: 0 },
]

/**
 * 关于页技术栈卡片项定义
 */
export interface SkillMatrixItem {
  element: string
  value: string
  iconName: 'Flame' | 'Waves' | 'Zap' | 'Mountain' | 'Wind' | 'Sparkles'
  color: string
  bg: string
  desc: string
}

/**
 * 站点全局配置
 *
 * 【规范原则】：
 * 1. 主页/Hero 的主题名词、角色设定、标语、台词集中在此配置维护。
 * 2. 基础功能（文章列表、分页、搜索、阅读耗时、分类、上一篇下一篇、代码复制等）一律直接写死通用技术规范词，严禁套用主题特定名词，无需额外配置。
 * 3. 避免过度配置，仅在关键展示位（标语、名片、页脚、彩蛋）提供配置入口。
 */
export const siteConfig = {
  // 站点基础信息与 SEO
  name: 'Chakra',
  siteName: 'chakra.blog',
  url: process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000',
  title: 'Chakra - 个人技术博客',
  description: '技术博客分享',
  keywords: ['.NET', 'C#', '微服务', 'AI Agent'],

  // 头像与默认封面
  avatar: '/images/naruto/akatsuki-avatar.webp',
  favicon: '/favicon.svg',
  defaultCover: '/images/naruto/cover-konoha.webp',

  // 站点展示文案
  workshopName: '技术空间',
  quote: '木叶飞舞之处，火亦生生不息',
  stationStatus: '在线运行',
  footerMotto: '所有的奇迹，都始于日常的专注',
  poweredBy: 'Powered by Chakra (.NET 10 & Next.js 15)',

  // 博主信息
  authorSubtitle: 'Chakra 博主',
  authorIntro: 'C#、微服务、Agent等架构实践。',
  wisdom: '放下助人情结,尊重他人命运',

  // 首页首屏文案
  hero: {
    readButtonText: '浏览文章',
    seriesButtonText: '专题专栏',
    scrollTipText: '向下滚动',
    gameTitle: '代码活跃',
    gameUnit: '次提交',
  },

  // 首页文章区域文案
  home: {
    latestArticlesTitle: '最新文章',
    allArticlesText: '全部文章',
    emptyArticlesText: '暂无最新文章',
  },

  // 关于页面配置
  about: {
    cardTitle: '博主介绍',
    regNumber: 'chakra-2026',
    matrixTitle: '核心技术栈介绍',
    systemAssurance: 'AI 辅助撰写为可选功能，默认关闭',
    skills: [
      {
        element: '火遁 · 核心架构',
        value: '.NET 10 / C#',
        iconName: 'Flame',
        color: 'text-amber-500',
        bg: 'bg-amber-500/10',
        desc: 'Vertical Slice CQRS + MediatR 读写分离架构',
      },
      {
        element: '水遁 · 前端架构',
        value: 'Next.js 15 & React 19',
        iconName: 'Waves',
        color: 'text-sky-500',
        bg: 'bg-sky-500/10',
        desc: 'App Router 流式渲染 + 响应式现代界面交互',
      },
      {
        element: '雷遁 · 部署方式',
        value: 'Docker Compose',
        iconName: 'Zap',
        color: 'text-cyan-500',
        bg: 'bg-cyan-500/10',
        desc: '全栈容器化部署，资源需求以实际负载测试为准',
      },
      {
        element: '土遁 · 存储方式',
        value: 'PostgreSQL & Redis',
        iconName: 'Mountain',
        color: 'text-yellow-600',
        bg: 'bg-yellow-600/10',
        desc: 'ACID 关系型权威存储 + 高性能分布式缓存',
      },
      {
        element: '风遁 · 通信协议',
        value: 'HTTP & JSON API',
        iconName: 'Wind',
        color: 'text-emerald-500',
        bg: 'bg-emerald-500/10',
        desc: '统一响应契约与服务端、浏览器双通道请求',
      },
      {
        element: '阴阳遁 · AI生态',
        value: 'LLM 内容辅助',
        iconName: 'Sparkles',
        color: 'text-purple-500',
        bg: 'bg-purple-500/10',
        desc: 'OpenAI 兼容接口与可选 RSS 技术文章辅助撰写',
      },
    ] as SkillMatrixItem[],
  },

  // 首屏加载动效配置
  startupLoader: {
    title: 'CONNECTING...',
    subtitle: '加载中..',
    icon: '/images/naruto/akatsuki-avatar.webp',
  },

  // 首页角色交互配置
  character: {
    // 白天模式
    light: {
      name: '漩涡鸣人',
      status: '鸣人 · 九尾仙术查克拉就绪',
      activeStatus: '螺旋查克拉 · 暴风凝聚',
      actionPrompt: '点击掌心释放螺旋丸',
      src: '/images/naruto/naruto.webp',
    },
    // 夜晚模式 
    dark: {
      name: '宇智波佐助',
      status: '佐助 · 须佐千鸟雷遁就绪',
      activeStatus: '雷遁千鸟 · 电光激荡',
      actionPrompt: '点击掌心释放千鸟',
      src: '/images/naruto/sasuke.webp',
    },
  },

  // 404 页面配置（卡卡西经典彩蛋）
  notFound: {
    badge: 'STATUS: 404',
    title: '页面未找到',
    quote: '抱歉，我在人生的道路上迷失了方向…… —— 旗木卡卡西',
    description: '在这条未知的道路上迷路了。或许前方并没有隐藏的秘卷，不如瞬身返回木叶村吧！',
    backHomeText: '返回首页',
    viewArticlesText: '浏览全部文章',
    image: '/images/naruto/kakashi-lost.webp',
    imageAlt: '旗木卡卡西 · 迷失方向',
  },

  // 回到顶部按钮配置（波风水门飞雷神彩蛋）
  backToTop: {
    icon: '/images/naruto/hiraishin-kunai.webp',
    iconAlt: '波风水门特制三叉苦无',
    runeText: '飞雷神✦',
    title: '飞雷神 · 瞬身回到顶部',
  },

  // 社交与外部链接
  socials: {
    github: 'https://github.com/dvvb258/chakra-blog',
    email: '',
  },
}

export type SiteConfig = typeof siteConfig
