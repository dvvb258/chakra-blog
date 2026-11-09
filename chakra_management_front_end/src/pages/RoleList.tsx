import { useState, useEffect, useCallback, useMemo } from 'react'
import {
  Card,
  Table,
  Button,
  Space,
  Tag,
  Modal,
  Form,
  Input,
  Tree,
  App,
  Popconfirm,
  Alert,
  Tooltip,
  Typography,
  Row,
  Col,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import type { DataNode } from 'antd/es/tree'
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  LockOutlined,
  ReloadOutlined,
  SafetyCertificateOutlined,
  CheckCircleFilled,
} from '@ant-design/icons'
import * as rolesApi from '@/api/roles'
import type { RoleDto, MenuDto, CreateRoleRequest, UpdateRoleRequest } from '@/types'
import { RoleCodes } from '@/constants/roles'
import styles from './RoleList.module.css'

const { Title, Paragraph, Text } = Typography

export default function RoleList() {
  const { message } = App.useApp()
  const [form] = Form.useForm()

  const [loading, setLoading] = useState(false)
  const [roles, setRoles] = useState<RoleDto[]>([])
  const [menus, setMenus] = useState<MenuDto[]>([])

  const [modalOpen, setModalOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null)
  const [checkedKeys, setCheckedKeys] = useState<React.Key[]>([])
  const [halfCheckedKeys, setHalfCheckedKeys] = useState<React.Key[]>([])

  // 加载角色与全量菜单数据
  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      const [rolesRes, menusRes] = await Promise.all([
        rolesApi.getRoles(),
        rolesApi.getMenus(),
      ])
      setRoles(rolesRes || [])
      setMenus(menusRes || [])
    } catch (err) {
      message.error(err instanceof Error ? err.message : '加载角色与权限数据失败')
    } finally {
      setLoading(false)
    }
  }, [message])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  // 菜单 ID 到菜单对象的映射表
  const menuMap = useMemo(() => {
    const map = new Map<string | number, MenuDto>()
    menus.forEach((m) => map.set(String(m.id), m))
    return map
  }, [menus])

  // 获取所有叶子节点 key（没有子菜单/按钮项的纯叶子节点）
  const leafKeys = useMemo(() => {
    const parentIdSet = new Set(menus.filter((m) => m.parentId).map((m) => String(m.parentId)))
    return new Set(menus.filter((m) => !parentIdSet.has(String(m.id))).map((m) => String(m.id)))
  }, [menus])

  // 将菜单列表构造为 Ant Design Tree 所需的层级树形结构
  const treeData = useMemo<DataNode[]>(() => {
    const map = new Map<string, DataNode>()
    const rootNodes: DataNode[] = []

    menus.forEach((m) => {
      let typeTag = <Tag color="blue" style={{ fontSize: 11, marginRight: 4 }}>页面</Tag>
      let label = `${m.name} (${m.path})`

      if (m.type === 1) {
        typeTag = <Tag color="orange" style={{ fontSize: 11, marginRight: 4 }}>目录</Tag>
        label = m.name
      } else if (m.type === 3) {
        typeTag = <Tag color="green" style={{ fontSize: 11, marginRight: 4 }}>按钮</Tag>
        label = `${m.name} [${m.permissionCode}]`
      }

      map.set(String(m.id), {
        key: String(m.id),
        title: (
          <span>
            {typeTag}
            <span>{label}</span>
          </span>
        ),
        disabled: editingRole?.code === RoleCodes.Admin, // 若为 Admin 则锁定置灰
        children: [],
      })
    })

    menus.forEach((m) => {
      const node = map.get(String(m.id))!
      if (m.parentId && map.has(String(m.parentId))) {
        map.get(String(m.parentId))!.children!.push(node)
      } else {
        rootNodes.push(node)
      }
    })

    const cleanEmptyChildren = (list: DataNode[]) => {
      list.forEach((item) => {
        if (item.children && item.children.length === 0) {
          delete item.children
        } else if (item.children) {
          cleanEmptyChildren(item.children)
        }
      })
    }
    cleanEmptyChildren(rootNodes)

    return rootNodes
  }, [menus, editingRole])

  // 打开创建弹窗
  const handleOpenCreate = () => {
    setEditingRole(null)
    setCheckedKeys([])
    setHalfCheckedKeys([])
    form.resetFields()
    setModalOpen(true)
  }

  // 打开编辑弹窗
  const handleOpenEdit = (record: RoleDto) => {
    setEditingRole(record)
    form.setFieldsValue({
      code: record.code,
      name: record.name,
      description: record.description,
    })

    if (record.code === RoleCodes.Admin) {
      // 超级管理员：全选所有菜单并锁定
      setCheckedKeys(menus.map((m) => String(m.id)))
      setHalfCheckedKeys([])
    } else {
      // 关键修复：只将真正的叶子节点传入 checkedKeys，让 Ant Design 自动计算父级半选状态
      // 杜绝因父节点 ID 导致未分配的子权限（如 205 审核发布）被强制级联全选的经典 Bug
      const currentIds = (record.menuIds || []).map((id) => String(id))
      const targetLeafChecked = currentIds.filter((id) => leafKeys.has(id))
      setCheckedKeys(targetLeafChecked)
      setHalfCheckedKeys(currentIds.filter((id) => !leafKeys.has(id)))
    }
    setModalOpen(true)
  }

  // 提交角色表单
  const handleSubmit = async () => {
    try {
      const values = await form.validateFields()
      setSubmitting(true)

      const allSelectedKeys = Array.from(new Set([...checkedKeys, ...halfCheckedKeys]))
      const selectedMenuIds = allSelectedKeys.map((k) => String(k).trim()).filter(Boolean)

      if (editingRole) {
        const payload: UpdateRoleRequest = {
          name: values.name.trim(),
          description: values.description?.trim() || undefined,
          menuIds: editingRole.code === RoleCodes.Admin ? menus.map((m) => String(m.id)) : selectedMenuIds,
        }
        await rolesApi.updateRole(editingRole.id, payload)
        message.success(`角色 [${values.name}] 权限配置已保存`)
      } else {
        const payload: CreateRoleRequest = {
          code: values.code.trim(),
          name: values.name.trim(),
          description: values.description?.trim() || undefined,
          menuIds: selectedMenuIds,
        }
        await rolesApi.createRole(payload)
        message.success(`新角色 [${values.name}] 已成功创建`)
      }

      setModalOpen(false)
      fetchData()
    } catch (err) {
      if (err instanceof Error) {
        message.error(err.message)
      }
    } finally {
      setSubmitting(false)
    }
  }

  // 删除角色
  const handleDelete = async (record: RoleDto) => {
    try {
      await rolesApi.deleteRole(record.id)
      message.success(`角色 [${record.name}] 已删除`)
      fetchData()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '删除角色失败')
    }
  }

  const columns: ColumnsType<RoleDto> = [
    {
      title: '角色标识',
      dataIndex: 'code',
      key: 'code',
      width: 150,
      render: (code: string, record) => (
        <Space>
          <Tag color={code === RoleCodes.Admin ? 'geekblue' : code === RoleCodes.Editor ? 'cyan' : 'purple'}>
            {code}
          </Tag>
          {record.isSystem && (
            <Tooltip title="系统保留角色不可删除">
              <LockOutlined style={{ color: '#faad14' }} />
            </Tooltip>
          )}
        </Space>
      ),
    },
    {
      title: '角色名称',
      dataIndex: 'name',
      key: 'name',
      width: 160,
      render: (name: string) => <Text strong>{name}</Text>,
    },
    {
      title: '职能描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
      render: (desc: string) => desc || <span style={{ color: '#94a3b8' }}>无描述</span>,
    },
    {
      title: '授权可访问菜单',
      key: 'menus',
      render: (_, record) => {
        if (record.code === RoleCodes.Admin) {
          return <Tag color="gold">全部系统菜单（特权通配）</Tag>
        }
        const ids = record.menuIds || []
        if (ids.length === 0) {
          return <span style={{ color: '#94a3b8' }}>未分配任何菜单</span>
        }
        return (
          <Space wrap size={[4, 6]}>
            {ids.map((id) => {
              const menu = menuMap.get(String(id))
              return menu ? (
                <Tag key={id} color="blue">
                  {menu.name}
                </Tag>
              ) : null
            })}
          </Space>
        )
      },
    },
    {
      title: '操作',
      key: 'action',
      width: 150,
      render: (_, record) => (
        <Space size={8}>
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleOpenEdit(record)}
          >
            配置权限
          </Button>
          {!record.isSystem && record.code !== RoleCodes.Admin && (
            <Popconfirm
              title={`确定要删除角色 [${record.name}] 吗？`}
              description="删除后，请确保该角色下无任何正在使用的用户。"
              okText="确认删除"
              cancelText="取消"
              okButtonProps={{ danger: true }}
              onConfirm={() => handleDelete(record)}
            >
              <Button type="link" size="small" danger icon={<DeleteOutlined />}>
                删除
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ]

  return (
    <div className={styles.container}>
      {/* 顶部欢迎横幅 */}
      <div className={styles.welcomeBanner}>
        <Row justify="space-between" align="middle" gutter={[16, 16]}>
          <Col xs={24} md={16}>
            <Space direction="horizontal" size={8} style={{ marginBottom: 8 }}>
              <Tag color="blue" icon={<CheckCircleFilled />}>
                RBAC PERMISSION ENGINE
              </Tag>
              <Tag color="cyan">
                .NET 10 DYNAMIC ACCESS CONTROL
              </Tag>
            </Space>
            <Title level={2} className={styles.welcomeTitle}>
              角色与菜单权限管理
            </Title>
            <Paragraph className={styles.welcomeSubtitle}>
              定义系统角色身份，通过权限树精确勾选并下发侧边栏菜单与路由访问边界。
            </Paragraph>
          </Col>
          <Col xs={24} md={8} style={{ textAlign: 'right' }}>
            <Space>
              <Button icon={<ReloadOutlined />} onClick={fetchData} loading={loading}>
                刷新
              </Button>
              <Button type="primary" icon={<PlusOutlined />} onClick={handleOpenCreate}>
                新增角色
              </Button>
            </Space>
          </Col>
        </Row>
      </div>

      <Card className={styles.roleCard} variant="borderless">
        <Table
          columns={columns}
          dataSource={roles}
          rowKey="id"
          loading={loading}
          pagination={false}
        />
      </Card>

      {/* 角色编辑/创建弹窗 */}
      <Modal
        title={
          <Space>
            <SafetyCertificateOutlined style={{ color: '#0284c7' }} />
            <span>{editingRole ? `配置角色权限 - ${editingRole.name}` : '创建新系统角色'}</span>
          </Space>
        }
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={handleSubmit}
        confirmLoading={submitting}
        width={580}
        destroyOnClose
      >
        {editingRole?.code === RoleCodes.Admin && (
          <Alert
            message="超级管理员拥有系统全量最高特权"
            description="超级管理员（Admin）拥有全量系统菜单且具备新模块自动适配能力，不可剥夺其菜单权限（核心防死锁保护）。"
            type="warning"
            showIcon
            style={{ marginBottom: 16 }}
          />
        )}

        <Form form={form} layout="vertical" preserve={false}>
          <Form.Item
            label="角色代号 (英文唯一标识)"
            name="code"
            rules={[
              { required: true, message: '请输入角色英文代号' },
              { pattern: /^[A-Za-z0-9_]{2,30}$/, message: '代号仅限 2-30 位字母、数字或下划线' },
            ]}
          >
            <Input
              placeholder="如 Operator, Reviewer"
              disabled={!!editingRole}
            />
          </Form.Item>

          <Form.Item
            label="角色名称"
            name="name"
            rules={[{ required: true, message: '请输入角色显示名称' }]}
          >
            <Input placeholder="如 运营专员、文章审核员" />
          </Form.Item>

          <Form.Item label="角色职能描述" name="description">
            <Input.TextArea rows={2} placeholder="简要说明该角色的职能与权限范围" />
          </Form.Item>

          <Form.Item label="授权可访问的系统菜单与细粒度按钮权限">
            <div className={styles.treeContainer}>
              <Tree
                checkable
                checkedKeys={checkedKeys}
                onCheck={(keys, info) => {
                  if (editingRole?.code !== RoleCodes.Admin) {
                    const checked = Array.isArray(keys) ? keys : keys.checked
                    setCheckedKeys(checked)
                    setHalfCheckedKeys(info.halfCheckedKeys || [])
                  }
                }}
                treeData={treeData}
                defaultExpandAll
              />
            </div>
            <div style={{ marginTop: 8, fontSize: 12, color: '#64748b' }}>
              提示：只有被勾选的菜单项，该角色的用户登录后才可在左侧导航栏看到并允许访问。
            </div>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
