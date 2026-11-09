import {
  applySiteConfig,
  defaultSiteConfig,
  mergeSiteConfig,
  type SiteConfig,
  type SiteConfigOverrides,
} from '@/config/site'
import { getApiBaseUrl } from './api'

/**
 * 站点配置缓存时长（毫秒）：后台保存后最迟 60 秒在所有实例生效
 */
const CACHE_TTL_MS = 60_000

/**
 * 后端接口不可用时的兜底重试间隔（毫秒），避免每次请求都等待超时
 */
const ERROR_RETRY_MS = 10_000

interface CacheEntry {
  value: SiteConfig
  expiresAt: number
}

let cache: CacheEntry | null = null
let inFlight: Promise<SiteConfig> | null = null

/**
 * 服务端获取站点配置（读取后端公开接口，合并到代码默认值）。
 *
 * 设计要点：
 * 1. 任何异常（后端不可用、返回格式异常）都回退到代码默认值，保证页面可用；
 * 2. 结果缓存 60 秒，避免每次 SSR 都打后端；
 * 3. 同进程内并发请求共享一次网络调用。
 */
export async function getSiteConfig(): Promise<SiteConfig> {
  const now = Date.now()
  if (cache && cache.expiresAt > now) {
    applySiteConfig(cache.value)
    return cache.value
  }

  if (!inFlight) {
    inFlight = fetchSiteConfig().finally(() => {
      inFlight = null
    })
  }

  const value = await inFlight
  // 同步服务端共享配置，供同一次渲染中的其他服务端组件（含 api.ts 的默认作者/封面）读取
  applySiteConfig(value)
  return value
}

/**
 * 保存后立即失效缓存（管理端触发，可选）
 */
export function invalidateSiteConfigCache(): void {
  cache = null
}

async function fetchSiteConfig(): Promise<SiteConfig> {
  const controller = new AbortController()
  const timeoutId = setTimeout(() => controller.abort(), 3000)

  try {
    const response = await fetch(`${getApiBaseUrl().replace(/\/$/, '')}/settings/site`, {
      cache: 'no-store',
      signal: controller.signal,
      headers: { Accept: 'application/json' },
    })

    if (!response.ok) {
      throw new Error(`站点设置接口响应异常：HTTP ${response.status}`)
    }

    const payload = (await response.json()) as { data?: SiteConfigOverrides | null }
    const value = normalizeUrl(mergeSiteConfig<SiteConfig>(defaultSiteConfig, payload?.data ?? {}))
    cache = { value, expiresAt: Date.now() + CACHE_TTL_MS }
    return value
  } catch (error) {
    // 后端不可用时回退默认配置，并缩短缓存时间以便尽快恢复
    if (process.env.NODE_ENV !== 'production') {
      console.warn('[site-config] 读取站点设置失败，已回退默认配置：', error)
    }

    const value = normalizeUrl(mergeSiteConfig<SiteConfig>(defaultSiteConfig, {}))
    cache = { value, expiresAt: Date.now() + ERROR_RETRY_MS }
    return value
  } finally {
    clearTimeout(timeoutId)
  }
}

/**
 * 站点地址容错：后台误填非绝对 HTTP(S) 地址时回退默认值，避免 new URL() 抛错导致整页 500
 */
function normalizeUrl(config: SiteConfig): SiteConfig {
  try {
    const parsed = new URL(config.url)
    if (parsed.protocol === 'http:' || parsed.protocol === 'https:') {
      return config
    }
  } catch {
    // 忽略解析异常，走默认值
  }

  return { ...config, url: defaultSiteConfig.url }
}
