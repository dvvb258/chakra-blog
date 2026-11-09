/**
 * 博客管理后台通用系统定义常量
 * （标准通用名词，不依赖特定配置）
 */

export const brand = {
  /** 系统名称（用于侧边栏 Logo、登录页主标题等） */
  name: 'Chakra',

  /** 控制台副标题 */
  consoleTitle: 'Chakra Console',

  /** 登录页主标题 */
  loginTitle: 'Chakra Admin',

  /** 登录页副标题描述 */
  loginSubtitle: '文章创作 · 专栏归档 · 系统治理',

  /** 登录页顶部徽章文案 */
  loginBadge: 'CHAKRA BLOG MANAGEMENT SYSTEM',

  /** Logo 标识首字符 */
  logoChar: 'C',

  /** 仪表盘欢迎描述文案 */
  dashboardDesc: 'Chakra 博客管理后台 · 实时监控全站流量、文章收录与近7天创作活跃度。',

  /** 仪表盘技术栈标签 */
  techStackLabel: '.NET 10 WEBAPI & NEXT.JS 15',

  /** 页脚版权声明 */
  copyright: 'CHAKRA ENGINE · ALL RIGHTS RESERVED',
}

/**
 * 前台展示端访问地址（运行期从后台「站点设置」读取，未配置时回退构建期变量与默认值）
 */
let runtimeFrontUrl = ''

/** 更新前台地址（应用启动加载站点设置后调用；传入空值表示恢复默认回退） */
export function setFrontUrl(url?: string): void {
  runtimeFrontUrl = typeof url === 'string' && url.trim() !== ''
    ? url.trim().replace(/\/$/, '')
    : ''
}

/** 获取当前生效的前台访问地址 */
export function getFrontUrl(): string {
  return runtimeFrontUrl || import.meta.env.VITE_BLOG_FRONT_URL || 'http://localhost:3000'
}
