import client from './client'

/** 站点配置覆盖项（仅包含需要覆盖默认值的字段） */
export type SiteSettingsOverrides = Record<string, unknown>

/**
 * 获取站点设置覆盖项（管理端，需站点设置权限）
 */
export function getSiteSettings(): Promise<SiteSettingsOverrides> {
  return client.get('/admin/settings/site')
}

/**
 * 保存站点设置覆盖项（整体覆盖，未提交字段回落代码默认值）
 */
export function updateSiteSettings(config: SiteSettingsOverrides): Promise<string> {
  return client.put('/admin/settings/site', { config })
}

/**
 * 获取公开站点设置（无需登录，用于读取前台地址等展示信息）
 */
export function getPublicSiteSettings(): Promise<SiteSettingsOverrides> {
  return client.get('/settings/site')
}
