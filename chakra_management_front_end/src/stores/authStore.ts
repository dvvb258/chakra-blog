import { create } from 'zustand'
import type { UserDto } from '@/types'
import * as authApi from '@/api/auth'
import { setClientToken } from '@/api/client'

interface AuthState {
  user: UserDto | null
  accessToken: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  initialized: boolean
  loading: boolean

  /** 账号密码登录 */
  login: (account: string, password: string) => Promise<void>
  /** GitHub 授权码登录 */
  githubLogin: (code: string, state: string) => Promise<void>
  /** 主动退出登录 */
  logout: () => Promise<void>
  /** 初始化并恢复会话 */
  restoreSession: () => Promise<void>
  /** 更新当前用户资料 */
  setUser: (user: UserDto) => void
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  accessToken: localStorage.getItem('accessToken'),
  refreshToken: localStorage.getItem('refreshToken'),
  isAuthenticated: !!localStorage.getItem('accessToken'),
  initialized: false,
  loading: false,

  login: async (account, password) => {
    set({ loading: true })
    try {
      const data = await authApi.login({ account, password })
      localStorage.setItem('accessToken', data.accessToken)
      localStorage.setItem('refreshToken', data.refreshToken)
      localStorage.setItem('user', JSON.stringify(data.user))
      // 同步注入 Axios 请求头
      setClientToken(data.accessToken)

      set({
        user: data.user,
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        isAuthenticated: true,
      })
    } finally {
      set({ loading: false })
    }
  },

  githubLogin: async (code, state) => {
    set({ loading: true })
    try {
      const data = await authApi.githubLogin({ code, state })
      localStorage.setItem('accessToken', data.accessToken)
      localStorage.setItem('refreshToken', data.refreshToken)
      localStorage.setItem('user', JSON.stringify(data.user))
      // 同步注入 Axios 请求头
      setClientToken(data.accessToken)

      set({
        user: data.user,
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        isAuthenticated: true,
      })
    } finally {
      set({ loading: false })
    }
  },

  logout: async () => {
    const rfToken = get().refreshToken || localStorage.getItem('refreshToken')
    try {
      if (rfToken) {
        await authApi.revokeToken(rfToken)
      }
    } catch {
      // 退出接口异常不阻塞前端清理流程
    } finally {
      setClientToken(null)
      localStorage.removeItem('accessToken')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('user')

      set({
        user: null,
        accessToken: null,
        refreshToken: null,
        isAuthenticated: false,
      })
    }
  },

  restoreSession: async () => {
    const token = localStorage.getItem('accessToken')
    const userStr = localStorage.getItem('user')

    if (!token) {
      setClientToken(null)
      set({ initialized: true, isAuthenticated: false, user: null })
      return
    }

    // 确保 Axios 默认头已挂载
    setClientToken(token)

    // 优先加载本地缓存的用户快照，避免首屏闪烁
    if (userStr) {
      try {
        const cachedUser = JSON.parse(userStr) as UserDto
        set({ user: cachedUser, isAuthenticated: true })
      } catch {
        // 解析失败忽略
      }
    }

    try {
      // 请求后端 /auth/me 获取权威的最新用户信息
      const user = await authApi.getCurrentUser()
      localStorage.setItem('user', JSON.stringify(user))
      set({
        user,
        isAuthenticated: true,
        accessToken: localStorage.getItem('accessToken'),
        refreshToken: localStorage.getItem('refreshToken'),
      })
    } catch {
      // 若获取失败且本地无 token 说明拦截器已将其注销
      if (!localStorage.getItem('accessToken')) {
        setClientToken(null)
        set({ user: null, isAuthenticated: false })
      }
    } finally {
      set({ initialized: true })
    }
  },

  setUser: (user: UserDto) => {
    localStorage.setItem('user', JSON.stringify(user))
    set({ user })
  },
}))
