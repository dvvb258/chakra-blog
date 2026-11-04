import axios, { type AxiosRequestConfig } from 'axios'
import type { ApiResult, AuthTokenDto } from '@/types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

// 优先读取本地已有 Token
const initialToken = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null

const client = axios.create({
  baseURL: BASE_URL ? `${BASE_URL.replace(/\/+$/, '')}/api` : '/api',
  timeout: 20000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

/**
 * 全局同步更新 Axios Authorization 请求头（双保险）
 */
export function setClientToken(token: string | null) {
  if (token) {
    client.defaults.headers.common['Authorization'] = `Bearer ${token}`
  } else {
    delete client.defaults.headers.common['Authorization']
  }
}

// 初始化时挂载
if (initialToken) {
  setClientToken(initialToken)
}

// 请求拦截器：附加 JWT Bearer Token 与全链路 Trace ID
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  if (token) {
    if (config.headers && typeof config.headers.set === 'function') {
      config.headers.set('Authorization', `Bearer ${token}`)
    } else {
      config.headers = config.headers || {}
      config.headers['Authorization'] = `Bearer ${token}`
    }
  }

  // 注入全链路 Trace ID（优先使用已有的，若无则生成前端唯一样本）
  const traceId = `tr_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 8)}`
  if (config.headers && typeof config.headers.set === 'function') {
    if (!config.headers.get('X-Trace-Id')) {
      config.headers.set('X-Trace-Id', traceId)
    }
  } else {
    config.headers = config.headers || {}
    if (!config.headers['X-Trace-Id']) {
      config.headers['X-Trace-Id'] = traceId
    }
  }

  return config
})

// 并发刷新互斥锁与排队订阅队列
let isRefreshing = false
let refreshSubscribers: Array<{
  resolve: (newToken: string) => void
  reject: (error: Error) => void
}> = []

function onRefreshed(token: string) {
  const subscribers = refreshSubscribers
  refreshSubscribers = []
  subscribers.forEach(({ resolve }) => resolve(token))
}

function onRefreshFailed(error: Error) {
  const subscribers = refreshSubscribers
  refreshSubscribers = []
  subscribers.forEach(({ reject }) => reject(error))
}

function clearSessionAndRedirect() {
  setClientToken(null)
  localStorage.removeItem('accessToken')
  localStorage.removeItem('refreshToken')
  localStorage.removeItem('user')
  if (window.location.pathname !== '/login') {
    window.location.href = '/login'
  }
}

// 响应拦截器：自动解包业务契约 + 401 无感刷新与队列重放
client.interceptors.response.use(
  (response) => {
    const resData = response.data as ApiResult<unknown>
    // 兼容后端统一响应契约 ApiResult<T>
    if (resData && typeof resData.code === 'number') {
      if (resData.code !== 200) {
        return Promise.reject(new Error(resData.message || '业务处理失败'))
      }
      return resData.data as never
    }
    return response.data
  },
  async (error) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean }

    // 针对 401 且非登录/刷新接口的情况进行无感续期
    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes('/auth/login') &&
      !originalRequest.url?.includes('/auth/refresh')
    ) {
      const refreshToken = localStorage.getItem('refreshToken')

      if (!refreshToken) {
        clearSessionAndRedirect()
        return Promise.reject(new Error('登录凭据已失效，请重新登录'))
      }

      if (isRefreshing) {
        originalRequest._retry = true
        // 当前已有刷新任务在执行，将该请求挂起排队
        return new Promise((resolve, reject) => {
          refreshSubscribers.push({ reject, resolve: (newToken: string) => {
            if (originalRequest.headers && typeof originalRequest.headers.set === 'function') {
              originalRequest.headers.set('Authorization', `Bearer ${newToken}`)
            } else if (originalRequest.headers) {
              originalRequest.headers['Authorization'] = `Bearer ${newToken}`
            }
            resolve(client(originalRequest))
          } })
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const apiPrefix = BASE_URL ? `${BASE_URL.replace(/\/+$/, '')}/api` : '/api'
        // 使用原生 axios 发起续期，避免重复触发当前实例的拦截器
        const res = await axios.post<ApiResult<AuthTokenDto>>(
          `${apiPrefix}/auth/refresh`,
          { refreshToken },
          { headers: { 'Content-Type': 'application/json' }, timeout: 20000, withCredentials: true }
        )

        if (res.data?.code === 200 && res.data.data?.accessToken) {
          const { accessToken: newAccessToken, refreshToken: newRefreshToken } = res.data.data
          localStorage.setItem('accessToken', newAccessToken)
          localStorage.setItem('refreshToken', newRefreshToken)
          setClientToken(newAccessToken)

          if (originalRequest.headers && typeof originalRequest.headers.set === 'function') {
            originalRequest.headers.set('Authorization', `Bearer ${newAccessToken}`)
          } else if (originalRequest.headers) {
            originalRequest.headers['Authorization'] = `Bearer ${newAccessToken}`
          }

          // 唤醒所有排队中的请求
          onRefreshed(newAccessToken)
          return client(originalRequest)
        } else {
          throw new Error('令牌刷新响应格式不正确')
        }
      } catch (refreshErr) {
        const failure = refreshErr instanceof Error ? refreshErr : new Error('令牌刷新失败')
        onRefreshFailed(failure)
        clearSessionAndRedirect()
        return Promise.reject(failure)
      } finally {
        isRefreshing = false
      }
    }

    const resTraceId =
      error.response?.data?.traceId ||
      error.response?.headers?.['x-trace-id'] ||
      originalRequest?.headers?.['X-Trace-Id']

    const rawMessage =
      error.response?.data?.message ||
      error.response?.data?.title ||
      error.message ||
      '网络请求失败，请稍后重试'

    const serverMessage = resTraceId
      ? `${rawMessage} [TraceID: ${resTraceId}]`
      : rawMessage

    const err = new Error(serverMessage) as Error & { traceId?: string }
    if (resTraceId) {
      err.traceId = String(resTraceId)
    }

    return Promise.reject(err)
  }
)

export default client
