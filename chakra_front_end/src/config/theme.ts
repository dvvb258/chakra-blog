/**
 * 站点主题设计系统调色板配置 (Theme Tokens)
 *
 * 如需调整全站配色，只需在此修改对应主题的 Hex / RGB 值，
 * 并与 globals.css 保持对应，全站所有组件与样式将统一生效。
 */

export interface ThemeTokenSet {
  name: string
  primary: string
  primaryRgb: string
  primaryHover: string
  primaryHoverRgb: string
  secondary: string
  secondaryRgb: string
  secondaryHover: string
  secondaryHoverRgb: string
  accent: string
  accentRgb: string
  surface: string
  surfaceRgb: string
  surfaceHover: string
  surfaceHoverRgb: string
  border: string
  borderRgb: string
  borderHover: string
  borderHoverRgb: string
  textPrimary: string
  textPrimaryRgb: string
  textMuted: string
  textMutedRgb: string
  bg: string
  bgRgb: string
  glow: string
  shadowRgb: string
}

export const themeTokens: {
  light: ThemeTokenSet
  dark: ThemeTokenSet
} = {
  light: {
    name: '暖阳明亮',
    primary: '#f59e0b',          // 暖金橙 (amber-500)
    primaryRgb: '245 158 11',
    primaryHover: '#d97706',     // amber-600
    primaryHoverRgb: '217 119 6',
    secondary: '#ea580c',        // 活力橙 (orange-600)
    secondaryRgb: '234 88 12',
    secondaryHover: '#c2410c',   // orange-700
    secondaryHoverRgb: '194 65 12',
    accent: '#10b981',           // 翠绿 (emerald-500)
    accentRgb: '16 185 129',
    surface: '#ffffff',          // 纯白卡片底色
    surfaceRgb: '255 255 255',
    surfaceHover: '#fffbeb',     // 卡片悬停底色 (amber-50)
    surfaceHoverRgb: '255 251 235',
    border: '#fde68a',           // 柔和边框 (amber-200)
    borderRgb: '253 230 138',
    borderHover: '#f59e0b',      // 激活边框 (amber-500)
    borderHoverRgb: '245 158 11',
    textPrimary: '#1c1917',      // 墨褐正文 (stone-900)
    textPrimaryRgb: '28 25 23',
    textMuted: '#78716c',        // 次要副文 (stone-500)
    textMutedRgb: '120 113 108',
    bg: '#fffdfa',               // 浅色背景底色
    bgRgb: '255 253 250',
    glow: 'rgba(245, 158, 11, 0.35)',
    shadowRgb: '245, 158, 11',
  },
  dark: {
    name: '深邃暗夜',
    primary: '#06b6d4',          // 冰蓝青 (cyan-500)
    primaryRgb: '6 182 212',
    primaryHover: '#0891b2',     // cyan-600
    primaryHoverRgb: '8 145 178',
    secondary: '#8b5cf6',        // 冷紫色 (violet-500)
    secondaryRgb: '139 92 246',
    secondaryHover: '#7c3aed',   // violet-600
    secondaryHoverRgb: '124 58 237',
    accent: '#ef4444',           // 点缀红 (red-500)
    accentRgb: '239 68 68',
    surface: '#0f1523',          // 深蓝黑卡片底色
    surfaceRgb: '15 21 35',
    surfaceHover: '#161e33',     // 悬停深蓝
    surfaceHoverRgb: '22 30 51',
    border: '#1e2942',           // 暗色边框
    borderRgb: '30 41 66',
    borderHover: '#06b6d4',      // 激活边框 (cyan-500)
    borderHoverRgb: '6 182 212',
    textPrimary: '#f8fafc',      // 纯白正文 (slate-50)
    textPrimaryRgb: '248 250 252',
    textMuted: '#94a3b8',        // 浅灰副文 (slate-400)
    textMutedRgb: '148 163 184',
    bg: '#080c16',               // 暗色背景底色
    bgRgb: '8 12 22',
    glow: 'rgba(6, 182, 212, 0.35)',
    shadowRgb: '6, 182, 212',
  },
}
