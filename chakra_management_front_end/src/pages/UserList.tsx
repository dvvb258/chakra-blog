import { useState, useEffect, useRef, useCallback } from 'react'
import {
  Card,
  Typography,
  Tag,
  Space,
  Avatar,
  Button,
  Form,
  Input,
  Row,
  Col,
  App,
  Popconfirm,
  Alert,
  Tooltip,
  Tabs,
  Table,
  Select,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  UserOutlined,
  GithubOutlined,
  SafetyCertificateOutlined,
  ReloadOutlined,
  LockOutlined,
  CameraOutlined,
  CheckCircleOutlined,
  DisconnectOutlined,
  KeyOutlined,
  MailOutlined,
  IdcardOutlined,
  TeamOutlined,
  LoadingOutlined,
} from '@ant-design/icons'
import { useAuthStore } from '@/stores/authStore'
import * as authApi from '@/api/auth'
import * as usersApi from '@/api/users'
import { getPublicSystemConfig } from '@/api/system'
import type { UserListItemDto } from '@/types'
import { uploadMedia } from '@/api/media'
import { RoleCodes, ROLE_OPTIONS } from '@/constants/roles'
import { PERMISSIONS } from '@/constants/permissions'
import { usePermission } from '@/hooks/usePermission'
import dayjs from 'dayjs'
import styles from './UserList.module.css'

const { Title, Paragraph, Text } = Typography

/**
 * 个人中心与账号权限管理页面 (Profile & Account Center)
 *
 * 【架构设计决策说明】
 * 本系统定位于个人技术博客与知识库，采用精简可控的用户生命周期模型：
 * 1. 核心管理员账号在系统初始化脚本 (init.sql) 中预置；
 * 2. 外部协同编辑者通过 GitHub OAuth 授权接入；
 * 3. 因此后台无需冗余的企业级 HR 用户增删/停用接口，核心聚焦于：
 *    - 博主个人资料维护、密码重置与 GitHub 平台安全绑定；
 *    - 具备超管权限时对多用户角色的动态分配（Admin / Editor）。
 */
export default function UserList() {
  const { user, setUser, restoreSession } = useAuthStore()
  const { message } = App.useApp()
  const { hasPermission, isAdmin } = usePermission()
  const canViewUsers = isAdmin || hasPermission(PERMISSIONS.USER_VIEW)
  const canUpdateRole = isAdmin || hasPermission(PERMISSIONS.USER_ROLE_UPDATE)

  const [profileForm] = Form.useForm()
  const [passwordForm] = Form.useForm()

  const [savingProfile, setSavingProfile] = useState(false)
  const [savingPassword, setSavingPassword] = useState(false)
  const [uploadingAvatar, setUploadingAvatar] = useState(false)
  const [unbindingGithub, setUnbindingGithub] = useState(false)
  const [githubEnabled, setGithubEnabled] = useState(false)
  const [refreshing, setRefreshing] = useState(false)

  const avatarInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    getPublicSystemConfig()
      .then((cfg) => {
        setGithubEnabled(!!cfg?.features?.githubLogin)
      })
      .catch(() => {
        setGithubEnabled(false)
      })
  }, [])

  // === 管理员：全站用户与角色管理状态 ===
  const [usersLoading, setUsersLoading] = useState(false)
  const [usersList, setUsersList] = useState<UserListItemDto[]>([])
  const [usersTotal, setUsersTotal] = useState(0)
  const [usersPage, setUsersPage] = useState(1)

  const fetchUsers = useCallback(async () => {
    if (!canViewUsers) return
    setUsersLoading(true)
    try {
      const res = await usersApi.getUsers({
        pageIndex: usersPage,
        pageSize: 10,
      })
      setUsersList(res.items || [])
      setUsersTotal(res.total || 0)
    } catch (err) {
      message.error(err instanceof Error ? err.message : '获取用户列表失败')
    } finally {
      setUsersLoading(false)
    }
  }, [canViewUsers, usersPage, message])

  useEffect(() => {
    fetchUsers()
  }, [fetchUsers])

  const handleRoleChange = async (targetUser: UserListItemDto, newRole: string) => {
    try {
      await usersApi.updateUserRole(targetUser.id, newRole)
      message.success(`用户 [${targetUser.nickname || targetUser.username}] 角色已更新为 ${newRole}`)
      fetchUsers()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '修改角色失败')
    }
  }

  // 同步用户数据至资料表单
  useEffect(() => {
    if (user) {
      profileForm.setFieldsValue({
        nickname: user.nickname,
        email: user.email,
        username: user.username,
        role: user.role,
        id: user.id,
      })
    }
  }, [user, profileForm])

  // 刷新资料
  const handleRefresh = async () => {
    setRefreshing(true)
    try {
      await restoreSession()
      message.success('个人资料已刷新')
    } catch {
      message.error('刷新失败，请稍后重试')
    } finally {
      setRefreshing(false)
    }
  }

  // 头像上传
  const handleAvatarUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    setUploadingAvatar(true)
    const hide = message.loading('正在上传头像...', 0)
    try {
      const res = await uploadMedia(file)
      // 立即更新当前用户资料
      const updatedUser = await authApi.updateProfile({
        nickname: user?.nickname || '',
        email: user?.email,
        avatarUrl: res.url,
      })
      setUser(updatedUser)
      message.success('头像已更新')
    } catch (err) {
      message.error(err instanceof Error ? err.message : '头像上传失败')
    } finally {
      hide()
      setUploadingAvatar(false)
      if (avatarInputRef.current) avatarInputRef.current.value = ''
    }
  }

  // 保存个人资料
  const handleSaveProfile = async () => {
    try {
      const values = await profileForm.validateFields()
      setSavingProfile(true)

      const updatedUser = await authApi.updateProfile({
        nickname: values.nickname.trim(),
        email: values.email?.trim() || null,
        avatarUrl: user?.avatarUrl,
      })

      setUser(updatedUser)
      message.success('个人基本资料保存成功')
    } catch (err) {
      if (err instanceof Error) {
        message.error(err.message)
      }
    } finally {
      setSavingProfile(false)
    }
  }

  // 修改密码
  const handleChangePassword = async () => {
    try {
      const values = await passwordForm.validateFields()
      setSavingPassword(true)

      await authApi.changePassword({
        oldPassword: values.oldPassword,
        newPassword: values.newPassword,
      })

      message.success('密码修改成功，请牢记新密码！')
      passwordForm.resetFields()
      // 刷新用户状态
      await restoreSession()
    } catch (err) {
      if (err instanceof Error) {
        message.error(err.message)
      }
    } finally {
      setSavingPassword(false)
    }
  }

  // 发起 GitHub 绑定
  const handleBindGithub = async () => {
    try {
      sessionStorage.setItem('github_action', 'bind')
      const result = await authApi.getGitHubAuthorizeUrl()
      if (result?.url && result.state) {
        sessionStorage.setItem('github_oauth_state', result.state)
        window.location.href = result.url
      } else {
        throw new Error('未获取到有效的 GitHub 授权跳转地址')
      }
    } catch (err) {
      sessionStorage.removeItem('github_action')
      sessionStorage.removeItem('github_oauth_state')
      message.error(err instanceof Error ? err.message : '获取 GitHub 授权地址失败')
    }
  }

  // 解绑 GitHub
  const handleUnbindGithub = async () => {
    setUnbindingGithub(true)
    try {
      const updatedUser = await authApi.unbindGithub()
      setUser(updatedUser)
      message.success('GitHub 账号已成功解除绑定')
    } catch (err) {
      message.error(err instanceof Error ? err.message : '解绑 GitHub 失败')
    } finally {
      setUnbindingGithub(false)
    }
  }

  const usersColumns: ColumnsType<UserListItemDto> = [
    {
      title: '用户',
      key: 'user',
      render: (_, record) => (
        <Space size={10}>
          <Avatar
            src={record.avatarUrl}
            icon={!record.avatarUrl && <UserOutlined />}
            style={{ backgroundColor: '#0284c7' }}
          />
          <div>
            <div style={{ fontWeight: 600, color: '#0f172a' }}>
              {record.nickname || record.username}
            </div>
            <div style={{ fontSize: 12, color: '#64748b' }}>
              {record.email || record.username}
            </div>
          </div>
        </Space>
      ),
    },
    {
      title: '当前角色分配',
      dataIndex: 'role',
      key: 'role',
      width: 170,
      render: (role: string, record) => {
        if (record.username === 'admin') {
          return (
            <Tag color="blue" icon={<SafetyCertificateOutlined />}>
              超级管理员 (内置)
            </Tag>
          )
        }
        return (
          <Select
            size="small"
            value={role}
            disabled={!canUpdateRole}
            onChange={(val) => handleRoleChange(record, val)}
            style={{ width: 140 }}
            options={ROLE_OPTIONS}
          />
        )
      },
    },
    {
      title: '第三方 GitHub',
      dataIndex: 'githubId',
      key: 'github',
      width: 140,
      render: (gid: string | null) =>
        gid ? (
          <Tag color="purple" icon={<GithubOutlined />}>已关联</Tag>
        ) : (
          <Text type="secondary" style={{ fontSize: 12 }}>未关联</Text>
        ),
    },
    {
      title: '账号状态',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 100,
      render: (active: boolean) =>
        active ? <Tag color="success">正常</Tag> : <Tag color="error">已停用</Tag>,
    },
    {
      title: '注册时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (d: string) => (d ? dayjs(d).format('YYYY-MM-DD HH:mm') : '—'),
    },
  ]

  const profileSection = (
    <Row gutter={[24, 24]}>
        {/* 左侧：个人基本资料 */}
        <Col xs={24} lg={11}>
          <Card
            className={styles.card}
            title={
              <div className={styles.cardTitle}>
                <IdcardOutlined style={{ color: '#0284c7' }} />
                <span>个人基本资料</span>
              </div>
            }
          >
            {/* 头像区域 */}
            <div className={styles.avatarSection}>
              <input
                ref={avatarInputRef}
                type="file"
                accept="image/*"
                style={{ display: 'none' }}
                onChange={handleAvatarUpload}
              />
              <Tooltip title="点击更换头像">
                <div
                  className={styles.avatarWrapper}
                  onClick={() => avatarInputRef.current?.click()}
                >
                  <Avatar
                    size={88}
                    src={user?.avatarUrl}
                    icon={!user?.avatarUrl && <UserOutlined />}
                    style={{ backgroundColor: '#0284c7' }}
                  />
                  <div className={styles.avatarOverlay}>
                    {uploadingAvatar ? <LoadingOutlined /> : <CameraOutlined />}
                    <span style={{ marginTop: 4 }}>更换头像</span>
                  </div>
                </div>
              </Tooltip>

              <div style={{ fontSize: 18, fontWeight: 700, color: '#0f172a' }}>
                {user?.nickname || user?.username}
              </div>
              <div className={styles.infoMeta}>
                最近登录：
                {user?.lastLoginAt
                  ? dayjs(user.lastLoginAt).format('YYYY-MM-DD HH:mm:ss')
                  : '首次登录'}
              </div>
            </div>

            {/* 资料表单 */}
            <Form form={profileForm} layout="vertical">
              <Form.Item
                label="用户昵称"
                name="nickname"
                rules={[{ required: true, message: '请输入昵称' }]}
              >
                <Input placeholder="输入公开展示的博主昵称" maxLength={50} />
              </Form.Item>

              <Form.Item
                label="电子邮箱"
                name="email"
                rules={[
                  { type: 'email', message: '请输入正确的邮箱格式' },
                ]}
              >
                <Input
                  prefix={<MailOutlined style={{ color: '#94a3b8' }} />}
                  placeholder="请输入电子邮箱"
                  maxLength={100}
                />
              </Form.Item>

              <Row gutter={16}>
                <Col span={12}>
                  <Form.Item label="登录账号（用户名）" name="username">
                    <Input disabled style={{ backgroundColor: '#f8fafc', color: '#475569' }} />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item label="系统角色">
                    <Tag color="blue" icon={<SafetyCertificateOutlined />} style={{ padding: '4px 10px' }}>
                      {user?.role || RoleCodes.Admin}
                    </Tag>
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item label="唯一雪花 ID">
                <Text code copyable style={{ color: '#0284c7', fontSize: 13 }}>
                  {user?.id ? String(user.id) : '—'}
                </Text>
              </Form.Item>

              <Button
                type="primary"
                onClick={handleSaveProfile}
                loading={savingProfile}
                style={{ width: '100%', height: 40, borderRadius: 8, marginTop: 8 }}
              >
                保存个人资料
              </Button>
            </Form>
          </Card>
        </Col>

        {/* 右侧：安全设置与第三方关联 */}
        <Col xs={24} lg={13}>
          {/* 登录密码修改 */}
          <Card
            className={styles.card}
            title={
              <div className={styles.cardTitle}>
                <KeyOutlined style={{ color: '#10b981' }} />
                <span>{user?.hasPassword ? '修改登录密码' : '设置登录密码'}</span>
              </div>
            }
          >
            {!user?.hasPassword && (
              <Alert
                message="尚未设置本地独立密码"
                description="您当前主要通过 GitHub 快捷登录。设置密码后，您即可直接使用用户名/邮箱与密码登录控制台。"
                type="warning"
                showIcon
                style={{ marginBottom: 16, borderRadius: 8 }}
              />
            )}

            <Form form={passwordForm} layout="vertical">
              {user?.hasPassword && (
                <Form.Item
                  label="原登录密码"
                  name="oldPassword"
                  rules={[{ required: true, message: '请输入原登录密码' }]}
                >
                  <Input.Password
                    prefix={<LockOutlined style={{ color: '#94a3b8' }} />}
                    placeholder="输入当前密码进行安全核验"
                    maxLength={100}
                  />
                </Form.Item>
              )}

              <Form.Item
                label="新登录密码"
                name="newPassword"
                rules={[
                  { required: true, message: '请输入新密码' },
                  { min: 6, message: '新密码长度不能少于 6 位' },
                ]}
              >
                <Input.Password
                  prefix={<LockOutlined style={{ color: '#94a3b8' }} />}
                  placeholder="请输入新密码（至少 6 位）"
                  maxLength={100}
                />
              </Form.Item>

              <Form.Item
                label="确认新密码"
                name="confirmPassword"
                dependencies={['newPassword']}
                rules={[
                  { required: true, message: '请再次输入新密码' },
                  ({ getFieldValue }) => ({
                    validator(_, value) {
                      if (!value || getFieldValue('newPassword') === value) {
                        return Promise.resolve()
                      }
                      return Promise.reject(new Error('两次输入的密码不一致！'))
                    },
                  }),
                ]}
              >
                <Input.Password
                  prefix={<CheckCircleOutlined style={{ color: '#94a3b8' }} />}
                  placeholder="请再次输入新密码"
                  maxLength={100}
                />
              </Form.Item>

              <Button
                type="primary"
                onClick={handleChangePassword}
                loading={savingPassword}
                style={{
                  width: '100%',
                  height: 40,
                  borderRadius: 8,
                  backgroundColor: '#10b981',
                  borderColor: '#10b981',
                }}
              >
                确认修改密码
              </Button>
            </Form>
          </Card>

          {/* 第三方账号绑定（仅在后端启用 GitHub 登录时展示） */}
          {githubEnabled && (
            <Card
              className={styles.card}
              title={
                <div className={styles.cardTitle}>
                  <GithubOutlined style={{ color: '#8b5cf6' }} />
                  <span>第三方平台账号关联</span>
                </div>
              }
            >
              <div className={styles.oauthItem}>
                <Space size={14} align="center">
                  <Avatar
                    size={42}
                    icon={<GithubOutlined />}
                    style={{ backgroundColor: '#24292e' }}
                  />
                  <div>
                    <div style={{ fontWeight: 600, color: '#0f172a', fontSize: 15 }}>
                      GitHub 账号
                    </div>
                    <div style={{ color: '#64748b', fontSize: 13, marginTop: 2 }}>
                      {user?.githubId ? (
                        <Space>
                          <Tag color="purple" style={{ borderRadius: 4 }}>
                            已关联 (ID: {user.githubId})
                          </Tag>
                        </Space>
                      ) : (
                        <span style={{ color: '#94a3b8' }}>尚未绑定 GitHub 账号</span>
                      )}
                    </div>
                  </div>
                </Space>

                <div>
                  {user?.githubId ? (
                    <Popconfirm
                      title="确定要解绑 GitHub 账号吗？"
                      description={
                        user.hasPassword
                          ? '解绑后您将无法再通过 GitHub 快捷登录，需使用账号密码登录。'
                          : '您尚未设置独立登录密码，必须先设置密码后方可解绑！'
                      }
                      okText="确认解绑"
                      cancelText="取消"
                      okButtonProps={{ danger: true, disabled: !user.hasPassword }}
                      onConfirm={handleUnbindGithub}
                    >
                      <Button
                        danger
                        icon={<DisconnectOutlined />}
                        loading={unbindingGithub}
                        style={{ borderRadius: 6 }}
                      >
                        解除绑定
                      </Button>
                    </Popconfirm>
                  ) : (
                    <Button
                      type="primary"
                      icon={<GithubOutlined />}
                      onClick={handleBindGithub}
                      style={{
                        borderRadius: 6,
                        backgroundColor: '#24292e',
                        borderColor: '#24292e',
                      }}
                    >
                      绑定账号
                    </Button>
                  )}
                </div>
              </div>
            </Card>
          )}
        </Col>
      </Row>
  )

  return (
    <div className={styles.container}>
      {/* 头部标题与操作 */}
      <div className={styles.header}>
        <div>
          <Title level={3} className={styles.title}>
            {canViewUsers ? '用户权限与个人中心' : '个人中心与安全设置'}
          </Title>
          <Paragraph className={styles.subtitle}>
            {canViewUsers
              ? '管理系统用户角色分配（Admin / Editor）、维护博主个人资料与第三方平台关联'
              : '管理博主个人资料、修改本地登录密码与关联第三方 GitHub 账号'}
          </Paragraph>
        </div>
        <Button
          icon={<ReloadOutlined />}
          onClick={handleRefresh}
          loading={refreshing}
          style={{ borderRadius: 8 }}
        >
          刷新资料
        </Button>
      </div>

      {canViewUsers ? (
        <Tabs
          type="card"
          size="large"
          items={[
            {
              key: 'profile',
              label: (
                <Space>
                  <IdcardOutlined />
                  <span>个人资料与安全</span>
                </Space>
              ),
              children: profileSection,
            },
            {
              key: 'users',
              label: (
                <Space>
                  <TeamOutlined />
                  <span>全站用户与角色管理</span>
                </Space>
              ),
              children: (
                <Card className={styles.card} styles={{ body: { padding: '16px 20px' } }}>
                  <Alert
                    message="单博主/协作系统说明"
                    description="博客系统用户由数据库初始预置或通过 GitHub OAuth 登录自动注册，超管可在此为协同写作者调整角色权限（Admin / Editor）。"
                    type="info"
                    showIcon
                    style={{ marginBottom: 16, borderRadius: 8 }}
                  />
                  <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Text type="secondary">
                      共 {usersTotal} 位注册用户。只有超级管理员（Admin）可调整用户角色，新用户默认角色为内容编辑者（Editor）。
                    </Text>
                    <Button icon={<ReloadOutlined />} onClick={fetchUsers} loading={usersLoading}>
                      刷新用户列表
                    </Button>
                  </div>
                  <Table<UserListItemDto>
                    rowKey="id"
                    columns={usersColumns}
                    dataSource={usersList}
                    loading={usersLoading}
                    pagination={{
                      current: usersPage,
                      pageSize: 10,
                      total: usersTotal,
                      showTotal: (t) => `共 ${t} 位系统用户`,
                      onChange: (p) => setUsersPage(p),
                    }}
                  />
                </Card>
              ),
            },
          ]}
        />
      ) : (
        profileSection
      )}
    </div>
  )
}
