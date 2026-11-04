import { useEffect, useCallback, useState, useRef } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Form, Input, Button, App, Spin, Alert } from 'antd'
import { UserOutlined, LockOutlined, GithubOutlined, ThunderboltOutlined } from '@ant-design/icons'
import { useAuthStore } from '@/stores/authStore'
import { getGitHubAuthorizeUrl, bindGithub } from '@/api/auth'
import { getPublicSystemConfig } from '@/api/system'
import { getFirstAuthorizedRoute } from '@/utils/permission'
import styles from './Login.module.css'
import { brand } from '@/config/brand'

export default function Login() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { login, githubLogin, setUser, loading, isAuthenticated } = useAuthStore()
  const { message } = App.useApp()
  const [githubLoading, setGithubLoading] = useState(false)
  const [githubEnabled, setGithubEnabled] = useState(false)
  const [authError, setAuthError] = useState<string | null>(null)
  const handledCodeRef = useRef<string | null>(null)

  // 挂载时探测系统特性（动态判断是否启用 GitHub 登录）
  useEffect(() => {
    getPublicSystemConfig()
      .then((cfg) => {
        setGithubEnabled(!!cfg?.features?.githubLogin)
      })
      .catch(() => {
        setGithubEnabled(false)
      })
  }, [])

  // 已登录用户直接重定向至后台首页（若正处于 GitHub 账号绑定回调则不拦截）
  useEffect(() => {
    const code = searchParams.get('code')
    const action = sessionStorage.getItem('github_action')
    if (isAuthenticated && !(code && action === 'bind')) {
      navigate('/', { replace: true })
    }
  }, [isAuthenticated, navigate, searchParams])

  // 处理 GitHub OAuth 回调授权码
  const handleGitHubCallback = useCallback(async () => {
    const code = searchParams.get('code')
    const state = searchParams.get('state')
    if (!code || handledCodeRef.current === code) return
    handledCodeRef.current = code

    // 状态参数一致性核验（防 CSRF 跨站请求伪造）
    const savedState = sessionStorage.getItem('github_oauth_state')
    if (!savedState || !state || savedState !== state) {
      const errMsg = 'OAuth 安全状态码校验失败（存在 CSRF 风险），请重新发起登录'
      setAuthError(errMsg)
      message.error(errMsg)
      sessionStorage.removeItem('github_oauth_state')
      sessionStorage.removeItem('github_action')
      return
    }
    sessionStorage.removeItem('github_oauth_state')

    setGithubLoading(true)
    setAuthError(null)

    // 区分“账号绑定”流程还是“登录”流程
    const action = sessionStorage.getItem('github_action')
    if (action === 'bind') {
      sessionStorage.removeItem('github_action')
      try {
        const updatedUser = await bindGithub(code, state)
        setUser(updatedUser)
        message.success('GitHub 账号绑定成功')
        navigate('/users', { replace: true })
      } catch (err) {
        const errMsg = err instanceof Error ? err.message : '绑定 GitHub 失败'
        message.error(errMsg)
        navigate('/users', { replace: true })
      } finally {
        setGithubLoading(false)
      }
      return
    }

    try {
      await githubLogin(code, state)
      const currentUser = useAuthStore.getState().user
      const targetRoute = getFirstAuthorizedRoute(currentUser?.menus, currentUser?.role === 'Admin')
      message.success('GitHub 身份认证成功，欢迎进入控制台')
      navigate(targetRoute, { replace: true })
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : 'GitHub 登录校验失败'
      setAuthError(errMsg)
      message.error(errMsg)
    } finally {
      setGithubLoading(false)
    }
  }, [searchParams, githubLogin, setUser, navigate, message])

  useEffect(() => {
    handleGitHubCallback()
  }, [handleGitHubCallback])

  // 账号密码提交登录
  const handleLogin = async (values: { account: string; password: string }) => {
    setAuthError(null)
    try {
      await login(values.account, values.password)
      const currentUser = useAuthStore.getState().user
      const targetRoute = getFirstAuthorizedRoute(currentUser?.menus, currentUser?.role === 'Admin')
      message.success('登录成功')
      navigate(targetRoute, { replace: true })
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : '登录认证失败，请检查账号密码'
      setAuthError(errMsg)
      message.error(errMsg)
    }
  }

  // 发起 GitHub OAuth 跳转
  const handleGitHubLogin = async () => {
    setAuthError(null)
    try {
      const result = await getGitHubAuthorizeUrl()
      if (result?.url && result.state) {
        sessionStorage.setItem('github_oauth_state', result.state)
        window.location.href = result.url
      } else {
        throw new Error('未获取到有效的 GitHub 授权跳转地址')
      }
    } catch (err) {
      const errMsg = err instanceof Error ? err.message : '获取 GitHub 授权配置失败'
      setAuthError(errMsg)
      message.error(errMsg)
    }
  }

  if (githubLoading) {
    return (
      <div className={styles.loadingOverlay}>
        <div className={styles.loadingCard}>
          <Spin size="large" />
          <div className={styles.loadingTextTitle}>正在验证 GitHub 授权凭据</div>
          <div className={styles.loadingTextSubtitle}>正在验证授权并获取访问凭据...</div>
        </div>
      </div>
    )
  }

  return (
    <div className={styles.container}>
      {/* 科技背景光晕与点阵 */}
      <div className={styles.ambientGlowPrimary} />
      <div className={styles.ambientGlowSecondary} />
      <div className={styles.gridOverlay} />

      <div className={styles.card}>
        <div className={styles.badgeTop}>
          <ThunderboltOutlined style={{ marginRight: 4 }} />
          <span>{brand.loginBadge}</span>
        </div>

        <div className={styles.header}>
          <div className={styles.logo}>
            <span className={styles.logoChar}>{brand.logoChar}</span>
          </div>
          <h1 className={styles.title}>{brand.loginTitle}</h1>
          <p className={styles.subtitle}>{brand.loginSubtitle}</p>
        </div>

        {authError && (
          <Alert
            message={authError}
            type="error"
            showIcon
            closable
            onClose={() => setAuthError(null)}
            style={{ marginBottom: 20 }}
          />
        )}

        <Form
          onFinish={handleLogin}
          size="large"
          layout="vertical"
          className={styles.form}
          requiredMark={false}
        >
          <Form.Item
            name="account"
            rules={[{ required: true, message: '请输入账号或邮箱' }]}
          >
            <Input
              prefix={<UserOutlined style={{ color: '#64748b' }} />}
              placeholder="请输入账号或邮箱"
              autoComplete="username"
              className={styles.input}
            />
          </Form.Item>

          <Form.Item
            name="password"
            rules={[{ required: true, message: '请输入密码' }]}
          >
            <Input.Password
              prefix={<LockOutlined style={{ color: '#64748b' }} />}
              placeholder="请输入登录密码"
              autoComplete="current-password"
              className={styles.input}
            />
          </Form.Item>

          <Form.Item style={{ marginBottom: 12 }}>
            <Button
              type="primary"
              htmlType="submit"
              block
              loading={loading}
              className={styles.loginBtn}
            >
              登 录 后 台
            </Button>
          </Form.Item>
        </Form>

        {githubEnabled && (
          <>
            <div className={styles.divider}>
              <span className={styles.dividerLine} />
              <span className={styles.dividerText}>第三方快捷授权</span>
              <span className={styles.dividerLine} />
            </div>

            <Button
              icon={<GithubOutlined />}
              block
              size="large"
              onClick={handleGitHubLogin}
              className={styles.githubBtn}
            >
              GitHub 一键安全登录
            </Button>
          </>
        )}

        <div className={styles.footer}>
          &copy; {new Date().getFullYear()} {brand.copyright}
        </div>
      </div>
    </div>
  )
}
