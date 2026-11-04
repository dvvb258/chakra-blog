/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        // 主题系统语义 Tokens (通过 CSS 变量 RGB 通道驱动，支持任意透明度如 /20, /80)
        theme: {
          primary: 'rgb(var(--theme-primary) / <alpha-value>)',
          'primary-hover': 'rgb(var(--theme-primary-hover) / <alpha-value>)',
          secondary: 'rgb(var(--theme-secondary) / <alpha-value>)',
          'secondary-hover': 'rgb(var(--theme-secondary-hover) / <alpha-value>)',
          accent: 'rgb(var(--theme-accent) / <alpha-value>)',
          surface: 'rgb(var(--theme-surface) / <alpha-value>)',
          'surface-hover': 'rgb(var(--theme-surface-hover) / <alpha-value>)',
          border: 'rgb(var(--theme-border) / <alpha-value>)',
          'border-hover': 'rgb(var(--theme-border-hover) / <alpha-value>)',
          text: 'rgb(var(--theme-text) / <alpha-value>)',
          muted: 'rgb(var(--theme-muted) / <alpha-value>)',
          bg: 'rgb(var(--theme-bg) / <alpha-value>)',
        },
        // ninja 别名映射（完全兼容 theme 语义，统一单点维护）
        ninja: {
          primary: 'rgb(var(--theme-primary) / <alpha-value>)',
          'primary-hover': 'rgb(var(--theme-primary-hover) / <alpha-value>)',
          secondary: 'rgb(var(--theme-secondary) / <alpha-value>)',
          'secondary-hover': 'rgb(var(--theme-secondary-hover) / <alpha-value>)',
          accent: 'rgb(var(--theme-accent) / <alpha-value>)',
          surface: 'rgb(var(--theme-surface) / <alpha-value>)',
          'surface-hover': 'rgb(var(--theme-surface-hover) / <alpha-value>)',
          border: 'rgb(var(--theme-border) / <alpha-value>)',
          'border-hover': 'rgb(var(--theme-border-hover) / <alpha-value>)',
          text: 'rgb(var(--theme-text) / <alpha-value>)',
          muted: 'rgb(var(--theme-muted) / <alpha-value>)',
        },
        ink: {
          primary: '#1e293b',
          secondary: '#475569',
          muted: '#94a3b8',
          border: '#e2e8f0',
        },
      },
      boxShadow: {
        paper: '0 2px 0 rgba(0,0,0,0.03), 0 8px 24px -4px rgba(var(--theme-shadow-rgb), 0.1)',
        'paper-hover': '0 4px 0 rgba(0,0,0,0.05), 0 16px 32px -6px rgba(var(--theme-shadow-rgb), 0.2)',
        sunlight: '0 10px 30px -5px rgba(var(--theme-shadow-rgb), 0.25)',
        tactical: '3px 3px 0px 0px rgba(var(--theme-shadow-rgb), 0.35)',
      },
      borderRadius: {
        chamfer: '10px 3px 10px 3px',
        'chamfer-lg': '18px 6px 18px 6px',
      },
    },
  },
  plugins: [],
}
