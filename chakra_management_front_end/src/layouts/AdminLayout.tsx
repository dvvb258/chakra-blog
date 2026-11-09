import { useState } from 'react'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import {
  Layout,
  Menu,
  Button,
  Dropdown,
  Avatar,
  Space,
  Tag,
  Tooltip,
} from 'antd'
import {
  UserOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  LogoutOutlined,
  DashboardOutlined,
  FileTextOutlined,
  ExportOutlined,
  BookOutlined,
  SafetyCertificateOutlined,
  TeamOutlined,
  AppstoreOutlined,
  FolderOutlined,
  SettingOutlined,
} from '@ant-design/icons'
import { useAuthStore } from '@/stores/authStore'
import { brand, getFrontUrl } from '@/config/brand'
import { RoleCodes } from '@/constants/roles'
import styles from './AdminLayout.module.css'

const { Header, Sider, Content } = Layout

const menuItems = [
  {
    key: '/',
    icon: <DashboardOutlined />,
    label: '仪表盘',
  },
  {
    key: '/articles',
    icon: <FileTextOutlined />,
    label: '文章管理',
  },
  {
    key: '/categories',
    icon: <FolderOutlined />,
    label: '分类管理',
  },
  {
    key: '/series',
    icon: <BookOutlined />,
    label: '专栏管理',
  },
  {
    key: '/logs',
    icon: <SafetyCertificateOutlined />,
    label: '系统日志',
  },
  {
    key: '/roles',
    icon: <TeamOutlined />,
    label: '角色权限',
  },
  {
    key: '/menus',
    icon: <AppstoreOutlined />,
    label: '菜单管理',
  },
  {
    key: '/settings',
    icon: <SettingOutlined />,
    label: '站点设置',
  },
  {
    key: '/users',
    icon: <UserOutlined />,
    label: '个人中心',
  },
]

export default function AdminLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const { user, logout } = useAuthStore()

  const isAdmin = user?.role === RoleCodes.Admin

  // 根据用户角色权限动态过滤侧边栏菜单（Admin 天然全量，普通角色根据动态分配的 menus 严格过滤）
  const currentMenuItems = menuItems.filter((item) => {
    if (isAdmin) return true
    return user?.menus?.includes(item.key)
  })

  const handleMenuClick = ({ key }: { key: string }) => {
    navigate(key)
  }

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  const userMenuItems = [
    {
      key: 'user-info',
      label: (
        <div style={{ padding: '4px 0' }}>
          <div style={{ fontWeight: 600, color: '#0f172a' }}>{user?.nickname || user?.username}</div>
          <div style={{ fontSize: 12, color: '#64748b' }}>{user?.role || '管理员'}</div>
        </div>
      ),
      disabled: true,
    },
    {
      type: 'divider' as const,
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '退出登录',
      danger: true,
      onClick: handleLogout,
    },
  ]

  // 计算当前选中的菜单项
  const selectedKey = location.pathname.startsWith('/articles')
    ? '/articles'
    : location.pathname.startsWith('/categories')
    ? '/categories'
    : location.pathname.startsWith('/series')
    ? '/series'
    : location.pathname.startsWith('/logs')
    ? '/logs'
    : location.pathname.startsWith('/roles')
    ? '/roles'
    : location.pathname.startsWith('/menus')
    ? '/menus'
    : location.pathname

  return (
    <Layout className={styles.layoutRoot}>
      <Sider
        trigger={null}
        collapsible
        collapsed={collapsed}
        className={styles.sider}
        width={220}
      >
        <div
          className={styles.logoContainer}
          style={{
            justifyContent: collapsed ? 'center' : 'flex-start',
            padding: collapsed ? 0 : '0 20px',
          }}
        >
          <div className={styles.logoBadge}>{brand.logoChar}</div>
          {!collapsed && (
            <div className={styles.logoTextWrapper}>
              <div className={styles.logoTitle}>{brand.name}</div>
              <div className={styles.logoSubtitle}>{brand.consoleTitle}</div>
            </div>
          )}
        </div>

        <Menu
          mode="inline"
          selectedKeys={[selectedKey]}
          items={currentMenuItems}
          onClick={handleMenuClick}
          style={{ borderRight: 'none', marginTop: 8, padding: '0 8px' }}
        />
      </Sider>

      <Layout style={{ background: '#f8fafc' }}>
        <Header className={styles.header}>
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
            className={styles.collapseTrigger}
          />

          <Space size={16}>
            <Tooltip title="访问博客前台公开端">
              <Button
                type="text"
                icon={<ExportOutlined />}
                style={{ color: '#64748b' }}
                onClick={() => window.open(getFrontUrl(), '_blank')}
              >
                前台站点
              </Button>
            </Tooltip>

            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight" arrow>
              <Space className={styles.userMenuTrigger}>
                <Avatar
                  src={user?.avatarUrl}
                  icon={!user?.avatarUrl && <UserOutlined />}
                  style={{
                    backgroundColor: '#0284c7',
                    boxShadow: '0 2px 6px rgba(2, 132, 199, 0.2)',
                  }}
                />
                <span className={styles.userName}>
                  {user?.nickname || user?.username || '博主'}
                </span>
                <Tag color="blue" bordered={false} style={{ margin: 0, fontSize: 11 }}>
                  {user?.role || RoleCodes.Admin}
                </Tag>
              </Space>
            </Dropdown>
          </Space>
        </Header>

        <Content className={styles.contentWrapper}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}
