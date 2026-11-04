import { useEffect, lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { App as AntApp, ConfigProvider, theme, Spin } from 'antd'
import zhCN from 'antd/locale/zh_CN'
import { useAuthStore } from '@/stores/authStore'
import AuthGuard from '@/components/AuthGuard'
import AdminLayout from '@/layouts/AdminLayout'

const Login = lazy(() => import('@/pages/Login'))
const Dashboard = lazy(() => import('@/pages/Dashboard'))
const UserList = lazy(() => import('@/pages/UserList'))
const ArticleList = lazy(() => import('@/pages/ArticleList'))
const ArticleCreate = lazy(() => import('@/pages/ArticleCreate'))
const ArticleEdit = lazy(() => import('@/pages/ArticleEdit'))
const CategoryList = lazy(() => import('@/pages/CategoryList'))
const SeriesList = lazy(() => import('@/pages/SeriesList'))
const LogList = lazy(() => import('@/pages/LogList'))
const RoleList = lazy(() => import('@/pages/RoleList'))
const MenuList = lazy(() => import('@/pages/MenuList'))

const PageLoading = () => (
  <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '60vh' }}>
    <Spin size="large" />
  </div>
)

function AppRoutes() {
  const restoreSession = useAuthStore((s) => s.restoreSession)

  useEffect(() => {
    restoreSession()
  }, [restoreSession])

  return (
    <Suspense fallback={<PageLoading />}>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route
          element={
            <AuthGuard>
              <AdminLayout />
            </AuthGuard>
          }
        >
          <Route
            path="/"
            element={
              <AuthGuard path="/">
                <Dashboard />
              </AuthGuard>
            }
          />
          <Route
            path="/articles"
            element={
              <AuthGuard path="/articles">
                <ArticleList />
              </AuthGuard>
            }
          />
          <Route path="/articles/create" element={<ArticleCreate />} />
          <Route path="/articles/:id/edit" element={<ArticleEdit />} />
          <Route
            path="/categories"
            element={
              <AuthGuard path="/categories">
                <CategoryList />
              </AuthGuard>
            }
          />
          <Route
            path="/series"
            element={
              <AuthGuard path="/series">
                <SeriesList />
              </AuthGuard>
            }
          />
          <Route
            path="/logs"
            element={
              <AuthGuard path="/logs">
                <LogList />
              </AuthGuard>
            }
          />
          <Route
            path="/roles"
            element={
              <AuthGuard requireAdmin path="/roles">
                <RoleList />
              </AuthGuard>
            }
          />
          <Route
            path="/menus"
            element={
              <AuthGuard requireAdmin path="/menus">
                <MenuList />
              </AuthGuard>
            }
          />
          <Route path="/users" element={<UserList />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Suspense>
  )
}

export default function App() {
  return (
    <ConfigProvider
      locale={zhCN}
      theme={{
        algorithm: theme.defaultAlgorithm,
        token: {
          colorPrimary: '#0284c7', // 现代经典科技天蓝
          colorInfo: '#0284c7',
          colorSuccess: '#10b981',
          colorWarning: '#f59e0b',
          colorError: '#ef4444',
          colorBgLayout: '#f8fafc', // 极其舒适通透的底色（Slate-50）
          colorBgContainer: '#ffffff',
          colorBorder: '#e2e8f0',
          colorBorderSecondary: '#f1f5f9',
          colorText: '#0f172a',
          colorTextSecondary: '#64748b',
          colorTextTertiary: '#94a3b8',
          borderRadius: 8,
          wireframe: false,
        },
        components: {
          Layout: {
            siderBg: '#ffffff',
            headerBg: '#ffffff',
            bodyBg: '#f8fafc',
          },
          Card: {
            colorBgContainer: '#ffffff',
            colorBorderSecondary: '#e2e8f0',
          },
          Table: {
            headerBg: '#f8fafc',
            headerColor: '#475569',
            rowHoverBg: '#f8fafc',
            borderColor: '#f1f5f9',
          },
          Menu: {
            itemColor: '#475569',
            itemHoverColor: '#0284c7',
            itemHoverBg: '#f8fafc',
            itemSelectedColor: '#0284c7',
            itemSelectedBg: '#f0f9ff',
          },
        },
      }}
    >
      <AntApp>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  )
}
