import React, { useState, useEffect, useCallback, useMemo } from 'react'
import {
  Card,
  Table,
  Button,
  Space,
  Tag,
  Modal,
  Form,
  Input,
  InputNumber,
  Select,
  Switch,
  Radio,
  AutoComplete,
  App,
  Popconfirm,
  Typography,
  Row,
  Col,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  ReloadOutlined,
  DashboardOutlined,
  FileTextOutlined,
  BookOutlined,
  SafetyCertificateOutlined,
  TeamOutlined,
  AppstoreOutlined,
  UserOutlined,
  SettingOutlined,
  FolderOutlined,
  TagOutlined,
  DatabaseOutlined,
  CloudOutlined,
  ToolOutlined,
  BarChartOutlined,
  BellOutlined,
  LinkOutlined,
  CommentOutlined,
  CompassOutlined,
  ProfileOutlined,
} from '@ant-design/icons'
import * as menusApi from '@/api/menus'
import type { MenuDto, CreateMenuRequest, UpdateMenuRequest, PermissionMetadata } from '@/types'
import styles from './MenuList.module.css'

const { Title, Paragraph, Text } = Typography

// 预定义常用图标字典映射
const ICON_DICT: Record<string, React.ReactNode> = {
  DashboardOutlined: <DashboardOutlined />,
  FileTextOutlined: <FileTextOutlined />,
  BookOutlined: <BookOutlined />,
  SafetyCertificateOutlined: <SafetyCertificateOutlined />,
  TeamOutlined: <TeamOutlined />,
  AppstoreOutlined: <AppstoreOutlined />,
  UserOutlined: <UserOutlined />,
  SettingOutlined: <SettingOutlined />,
  FolderOutlined: <FolderOutlined />,
  TagOutlined: <TagOutlined />,
  DatabaseOutlined: <DatabaseOutlined />,
  CloudOutlined: <CloudOutlined />,
  ToolOutlined: <ToolOutlined />,
  BarChartOutlined: <BarChartOutlined />,
  BellOutlined: <BellOutlined />,
  LinkOutlined: <LinkOutlined />,
  CommentOutlined: <CommentOutlined />,
  CompassOutlined: <CompassOutlined />,
  ProfileOutlined: <ProfileOutlined />,
}

// 辅助渲染图标组件
function renderMenuIcon(iconName?: string | null) {
  if (!iconName) return null
  return ICON_DICT[iconName] || <AppstoreOutlined />
}

interface MenuTableItem extends MenuDto {
  children?: MenuTableItem[]
}

export default function MenuList() {
  const { message } = App.useApp()
  const [form] = Form.useForm()
  const selectedType = Form.useWatch('type', form)

  const [loading, setLoading] = useState(false)
  const [menus, setMenus] = useState<MenuDto[]>([])
  const [metadataList, setMetadataList] = useState<PermissionMetadata[]>([])

  const [modalOpen, setModalOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [editingMenu, setEditingMenu] = useState<MenuDto | null>(null)

  // 加载全量菜单列表
  const fetchMenus = useCallback(async () => {
    setLoading(true)
    try {
      const data = await menusApi.getMenus()
      setMenus(data || [])
    } catch (err) {
      message.error(err instanceof Error ? err.message : '加载系统菜单失败')
    } finally {
      setLoading(false)
    }
  }, [message])

  useEffect(() => {
    fetchMenus()
    // 加载后端注册的权威系统权限元数据字典
    menusApi
      .getPermissionsMetadata()
      .then((data) => setMetadataList(data || []))
      .catch(() => {})
  }, [fetchMenus])

  // 构造树形表格数据（支持多层级嵌套）
  const tableData = useMemo<MenuTableItem[]>(() => {
    const map = new Map<string, MenuTableItem>()
    const rootNodes: MenuTableItem[] = []

    menus.forEach((item) => {
      map.set(String(item.id), { ...item, children: [] })
    })

    menus.forEach((item) => {
      const current = map.get(String(item.id))!
      if (item.parentId && map.has(String(item.parentId))) {
        map.get(String(item.parentId))!.children?.push(current)
      } else {
        rootNodes.push(current)
      }
    })

    // 清理空 children 数组以正确配合 Antd Table 展开折叠
    const cleanEmptyChildren = (nodes: MenuTableItem[]) => {
      nodes.forEach((n) => {
        if (n.children && n.children.length === 0) {
          delete n.children
        } else if (n.children) {
          cleanEmptyChildren(n.children)
        }
      })
    }
    cleanEmptyChildren(rootNodes)

    return rootNodes
  }, [menus])

  // 打开创建顶级菜单弹窗
  const handleOpenCreateRoot = () => {
    setEditingMenu(null)
    form.resetFields()
    form.setFieldsValue({
      parentId: null,
      sort: 0,
      type: 2,
      isHidden: false,
      icon: 'AppstoreOutlined',
    })
    setModalOpen(true)
  }

  // 打开创建子菜单弹窗
  const handleOpenCreateSub = (parentRecord: MenuDto) => {
    setEditingMenu(null)
    form.resetFields()
    // 若父级本身是菜单页面，默认子项为按钮/操作
    const defaultType = parentRecord.type === 2 ? 3 : 2
    form.setFieldsValue({
      parentId: String(parentRecord.id),
      sort: 0,
      type: defaultType,
      isHidden: defaultType === 3,
      icon: defaultType === 3 ? undefined : 'AppstoreOutlined',
    })
    setModalOpen(true)
  }

  // 打开编辑弹窗
  const handleOpenEdit = (record: MenuDto) => {
    setEditingMenu(record)
    form.setFieldsValue({
      name: record.name,
      path: record.path,
      permissionCode: record.permissionCode,
      parentId: record.parentId ? String(record.parentId) : null,
      icon: record.icon || (record.type === 3 ? undefined : 'AppstoreOutlined'),
      sort: record.sort,
      type: record.type ?? 2,
      isHidden: record.isHidden,
    })
    setModalOpen(true)
  }

  // 提交菜单表单
  const handleSubmit = async () => {
    try {
      const values = await form.validateFields()
      setSubmitting(true)

      const payload = {
        name: values.name.trim(),
        path: values.path ? values.path.trim() : '',
        permissionCode: values.permissionCode?.trim() || undefined,
        parentId: values.parentId ? Number(values.parentId) : null,
        icon: values.icon?.trim() || null,
        sort: values.sort ?? 0,
        type: values.type ?? 2,
        isHidden: values.type === 3 ? true : !!values.isHidden,
      }

      if (editingMenu) {
        await menusApi.updateMenu(editingMenu.id, payload as UpdateMenuRequest)
        message.success(`菜单 [${values.name}] 更新成功`)
      } else {
        await menusApi.createMenu(payload as CreateMenuRequest)
        message.success(`菜单 [${values.name}] 创建成功`)
      }

      setModalOpen(false)
      fetchMenus()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '保存菜单失败')
    } finally {
      setSubmitting(false)
    }
  }

  // 删除菜单
  const handleDelete = async (record: MenuDto) => {
    try {
      await menusApi.deleteMenu(record.id)
      message.success(`菜单 [${record.name}] 已成功删除`)
      fetchMenus()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '删除菜单失败')
    }
  }

  // 表格列定义
  const columns: ColumnsType<MenuTableItem> = [
    {
      title: '菜单名称',
      dataIndex: 'name',
      key: 'name',
      render: (name: string, record) => (
        <Space>
          <span className={styles.iconPreview}>{renderMenuIcon(record.icon)}</span>
          <Text strong style={{ color: '#0f172a' }}>
            {name}
          </Text>
        </Space>
      ),
    },
    {
      title: '资源类型',
      dataIndex: 'type',
      key: 'type',
      width: 100,
      render: (type?: number) => {
        if (type === 1) return <Tag color="orange">目录</Tag>
        if (type === 3) return <Tag color="green">按钮操作</Tag>
        return <Tag color="blue">页面菜单</Tag>
      },
    },
    {
      title: '路由路径',
      dataIndex: 'path',
      key: 'path',
      render: (path: string, record) =>
        record.type === 3 ? (
          <Text type="secondary" style={{ fontSize: 12 }}>
            (按钮无独立路由)
          </Text>
        ) : (
          <Tag color="blue" style={{ fontFamily: 'monospace' }}>
            {path || '/'}
          </Tag>
        ),
    },
    {
      title: '权限标识 (Code)',
      dataIndex: 'permissionCode',
      key: 'permissionCode',
      render: (code: string) => (
        <Tag color="cyan" style={{ fontFamily: 'monospace' }}>
          {code}
        </Tag>
      ),
    },
    {
      title: '图标标识',
      dataIndex: 'icon',
      key: 'icon',
      render: (icon: string | null) =>
        icon ? (
          <Text type="secondary" style={{ fontSize: 13 }}>
            {icon}
          </Text>
        ) : (
          <Text type="secondary">-</Text>
        ),
    },
    {
      title: '显示排序',
      dataIndex: 'sort',
      key: 'sort',
      width: 100,
      align: 'center',
    },
    {
      title: '显示状态',
      dataIndex: 'isHidden',
      key: 'isHidden',
      width: 110,
      render: (isHidden: boolean) =>
        isHidden ? (
          <Tag color="warning">侧栏隐藏</Tag>
        ) : (
          <Tag color="success">正常显示</Tag>
        ),
    },
    {
      title: '操作',
      key: 'action',
      width: 220,
      render: (_, record) => (
        <Space size="small">
          <Button
            size="small"
            type="link"
            icon={<PlusOutlined />}
            onClick={() => handleOpenCreateSub(record)}
          >
            添加子项
          </Button>
          <Button
            size="small"
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleOpenEdit(record)}
          >
            编辑
          </Button>
          <Popconfirm
            title="确认删除该菜单项？"
            description="删除后将同步解除角色的此项权限绑定；若有子菜单需先清理子菜单。"
            onConfirm={() => handleDelete(record)}
            okText="确认删除"
            cancelText="取消"
            okButtonProps={{ danger: true }}
          >
            <Button size="small" type="link" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  // 下拉父级菜单候选项列表（编辑时排除自身以防止成环）
  const parentOptions = useMemo(() => {
    return [
      { label: '顶级根菜单 (NULL)', value: '' },
      ...menus
        .filter((m) => !editingMenu || String(m.id) !== String(editingMenu.id))
        .map((m) => ({
          label: `${m.name} (${m.path})`,
          value: String(m.id),
        })),
    ]
  }, [menus, editingMenu])

  return (
    <div className={styles.container}>
      {/* 顶部横幅 */}
      <div className={styles.welcomeBanner}>
        <Row align="middle" justify="space-between">
          <Col>
            <Title level={4} className={styles.welcomeTitle}>
              系统菜单与权限项管理
            </Title>
            <Paragraph className={styles.welcomeSubtitle}>
              配置前端路由资源与权限代码。新增的菜单项将即时同步至角色权限分配树，并随授权动态装配至侧边栏。
            </Paragraph>
          </Col>
          <Col>
            <Space>
              <Button
                icon={<ReloadOutlined />}
                onClick={fetchMenus}
                loading={loading}
              >
                刷新
              </Button>
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={handleOpenCreateRoot}
              >
                新建顶级菜单
              </Button>
            </Space>
          </Col>
        </Row>
      </div>

      {/* 菜单列表表格 */}
      <Card className={styles.menuCard} variant="borderless">
        <Table<MenuTableItem>
          rowKey={(record) => String(record.id)}
          columns={columns}
          dataSource={tableData}
          loading={loading}
          pagination={false}
          size="middle"
        />
      </Card>

      {/* 创建 / 编辑菜单弹窗 */}
      <Modal
        title={editingMenu ? `编辑菜单 - ${editingMenu.name}` : '新建系统菜单项'}
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={submitting}
        okText="保存"
        cancelText="取消"
        width={560}
        destroyOnClose
      >
        <Form
          form={form}
          layout="vertical"
          style={{ marginTop: 16 }}
          initialValues={{
            sort: 0,
            type: 2,
            isHidden: false,
            icon: 'AppstoreOutlined',
          }}
        >
          <Form.Item name="type" label="资源类型" tooltip="目录用于折叠分类，菜单对应可打开页面，按钮操作对应页面内具体操作">
            <Radio.Group buttonStyle="solid">
              <Radio.Button value={1}>目录 (Folder)</Radio.Button>
              <Radio.Button value={2}>菜单页面 (Menu)</Radio.Button>
              <Radio.Button value={3}>按钮操作 (Button)</Radio.Button>
            </Radio.Group>
          </Form.Item>

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="name"
                label={selectedType === 3 ? '操作名称' : '菜单名称'}
                rules={[{ required: true, message: '请输入名称' }]}
              >
                <Input placeholder={selectedType === 3 ? '如：删除文章、审核文章' : '如：菜单管理、分类管理'} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="path"
                label="路由路径"
                rules={[{ required: selectedType !== 3, message: '请输入前端路由路径' }]}
                tooltip="对应前端路由路径，以 / 开头，按钮权限可不填"
              >
                <Input placeholder={selectedType === 3 ? '按钮权限无需配置路由' : '如：/menus'} disabled={selectedType === 3} />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="permissionCode"
                label="权限标识 (Permission Code)"
                tooltip="可直接从后端权威元数据字典中搜索选择，彻底杜绝手敲失误"
              >
                <AutoComplete
                  placeholder="搜索或选择后端标准权限，如 content:article:delete"
                  options={metadataList.map((m) => ({
                    value: m.code,
                    label: (
                      <Space>
                        <Tag color="cyan">{m.module}</Tag>
                        <span>{m.name}</span>
                        <Text type="secondary" style={{ fontSize: 11 }}>
                          ({m.code})
                        </Text>
                      </Space>
                    ),
                  }))}
                  filterOption={(inputValue, option) =>
                    String(option?.value || '')
                      .toLowerCase()
                      .includes(inputValue.toLowerCase())
                  }
                  onSelect={(value) => {
                    const matched = metadataList.find((m) => m.code === value)
                    if (matched && !form.getFieldValue('name')) {
                      form.setFieldsValue({ name: matched.name })
                    }
                  }}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="parentId" label="挂载父级节点">
                <Select
                  options={parentOptions}
                  placeholder="选择父级菜单或顶级根菜单"
                  allowClear
                />
              </Form.Item>
            </Col>
          </Row>

          {selectedType !== 3 && (
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  name="icon"
                  label="菜单图标"
                  tooltip="可选择常用图标或直接输入 Antd 图标组件名"
                >
                  <Select
                    showSearch
                    allowClear
                    placeholder="选择或输入图标"
                    options={Object.keys(ICON_DICT).map((key) => ({
                      label: (
                        <Space>
                          {ICON_DICT[key]}
                          <span>{key}</span>
                        </Space>
                      ),
                      value: key,
                    }))}
                  />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  name="sort"
                  label="排序权重"
                  tooltip="数字越小在侧边栏中排列越靠前"
                >
                  <InputNumber min={0} max={9999} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
            </Row>
          )}

          {selectedType !== 3 && (
            <Form.Item
              name="isHidden"
              label="侧边栏状态"
              valuePropName="checked"
              tooltip="隐藏后该项不会直接在左侧导航栏显示，常用于非直显页面"
            >
              <Switch
                checkedChildren="侧栏隐藏"
                unCheckedChildren="正常显示"
              />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  )
}
