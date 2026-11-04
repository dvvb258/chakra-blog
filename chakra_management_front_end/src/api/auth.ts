import client from './client'
import type {
  AuthTokenDto,
  UserDto,
  LoginRequest,
  GitHubAuthorizeDto,
  GitHubLoginRequest,
} from '@/types'

/**
 * 管理员账号密码登录（支持用户名或邮箱）
 */
export function login(credentials: LoginRequest): Promise<AuthTokenDto> {
  return client.post('/auth/login', credentials)
}

/**
 * 手动使用 RefreshToken 刷新双 Token
 */
export function refreshToken(refreshToken: string): Promise<AuthTokenDto> {
  return client.post('/auth/refresh', { refreshToken })
}

/**
 * 撤销刷新令牌并注销登录
 */
export function revokeToken(refreshToken: string): Promise<string> {
  return client.post('/auth/revoke', { refreshToken })
}

/**
 * 获取当前登录人员详细个人资料
 */
export function getCurrentUser(): Promise<UserDto> {
  return client.get('/auth/me')
}

/**
 * 获取 GitHub OAuth 授权页面 URL
 */
export function getGitHubAuthorizeUrl(): Promise<GitHubAuthorizeDto> {
  return client.get('/auth/github/url')
}

/**
 * 使用 GitHub 临时授权码 code 完成第三方登录
 */
export function githubLogin(payload: GitHubLoginRequest): Promise<AuthTokenDto> {
  return client.post('/auth/github', payload)
}

/**
 * 更新当前登录人基本资料（昵称、头像、邮箱）
 */
export function updateProfile(data: {
  nickname: string
  email?: string | null
  avatarUrl?: string | null
}): Promise<UserDto> {
  return client.put('/auth/profile', data)
}

/**
 * 修改当前登录人密码
 */
export function changePassword(data: {
  oldPassword?: string
  newPassword: string
}): Promise<string> {
  return client.put('/auth/password', data)
}

/**
 * 绑定 GitHub 账号
 */
export function bindGithub(code: string, state: string): Promise<UserDto> {
  return client.post('/auth/github/bind', { code, state })
}

/**
 * 解绑 GitHub 账号
 */
export function unbindGithub(): Promise<UserDto> {
  return client.delete('/auth/github/unbind')
}

/**
 * 获取当前登录人授权的可访问菜单列表
 */
export function getMyMenus(): Promise<import('@/types').MenuDto[]> {
  return client.get('/auth/my-menus')
}
