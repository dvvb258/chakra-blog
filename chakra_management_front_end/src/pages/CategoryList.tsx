import React, { useState, useEffect, useCallback } from 'react'
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
  FolderOutlined,
  CodeOutlined,
  RobotOutlined,
  CloudOutlined,
  DatabaseOutlined,
  AppstoreOutlined,
  ToolOutlined,
  LaptopOutlined,
  RocketOutlined,
  ThunderboltOutlined,
} from '@ant-design/icons'
import * as categoriesApi from '@/api/categories'
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'
import type { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '@/types'
import { getCategoryColor } from '@/constants/article'
import styles from './CategoryList.module.css'

const { Title, Paragraph, Text } = Typography

// 预定义常用分类图标映射
const CATEGORY_ICON_MAP: Record<string, React.ReactNode> = {
  FolderOutlined: <FolderOutlined />,
  CodeOutlined: <CodeOutlined />,
  RobotOutlined: <RobotOutlined />,
  CloudOutlined: <CloudOutlined />,
  DatabaseOutlined: <DatabaseOutlined />,
  AppstoreOutlined: <AppstoreOutlined />,
  ToolOutlined: <ToolOutlined />,
  LaptopOutlined: <LaptopOutlined />,
  RocketOutlined: <RocketOutlined />,
  ThunderboltOutlined: <ThunderboltOutlined />,
}

function renderCategoryIcon(iconName?: string | null) {
  if (!iconName) return <FolderOutlined />
  return CATEGORY_ICON_MAP[iconName] || <FolderOutlined />
}

export default function CategoryList() {
  const { message } = App.useApp()
  const [form] = Form.useForm()

  const [loading, setLoading] = useState(false)
  const [categories, setCategories] = useState<CategoryDto[]>([])

  const [modalOpen, setModalOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [editingCategory, setEditingCategory] = useState<CategoryDto | null>(null)

  // 加载全量分类列表
  const fetchCategories = useCallback(async () => {
    setLoading(true)
    try {
      const data = await categoriesApi.getCategories()
      setCategories(Array.isArray(data) ? data : [])
    } catch (err) {
      message.error(err instanceof Error ? err.message : '加载分类列表失败')
    } finally {
      setLoading(false)
    }
  }, [message])

  useEffect(() => {
    fetchCategories()
  }, [fetchCategories])

  // 打开创建分类弹窗
  const handleOpenCreate = () => {
    setEditingCategory(null)
    form.resetFields()
    form.setFieldsValue({
      sort: 0,
      icon: 'FolderOutlined',
    })
    setModalOpen(true)
  }

  // 打开编辑分类弹窗
  const handleOpenEdit = (record: CategoryDto) => {
    setEditingCategory(record)
    form.setFieldsValue({
      name: record.name,
      description: record.description,
      icon: record.icon || 'FolderOutlined',
      sort: record.sort ?? 0,
    })
    setModalOpen(true)
  }

  // 提交分类表单
  const handleSubmit = async () => {
    try {
      const values = await form.validateFields()
      setSubmitting(true)

      const payload = {
        name: values.name.trim(),
        description: values.description?.trim() || '',
        icon: values.icon?.trim() || 'FolderOutlined',
        sort: values.sort ?? 0,
      }

      if (editingCategory) {
        await categoriesApi.updateCategory(editingCategory.id, payload as UpdateCategoryRequest)
        message.success(`文章分类 [${values.name}] 更新成功`)
      } else {
        await categoriesApi.createCategory(payload as CreateCategoryRequest)
        message.success(`文章分类 [${values.name}] 创建成功`)
      }

      setModalOpen(false)
      fetchCategories()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '保存分类失败')
    } finally {
      setSubmitting(false)
    }
  }

  // 删除分类
  const handleDelete = async (record: CategoryDto) => {
    try {
      await categoriesApi.deleteCategory(record.id)
      message.success(`分类 [${record.name}] 已成功删除`)
      fetchCategories()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '删除分类失败')
    }
  }

  // 表格列定义
  const columns: ColumnsType<CategoryDto> = [
    {
      title: '分类名称',
      dataIndex: 'name',
      key: 'name',
      render: (name: string, record) => (
        <Space>
          <span
            className={styles.iconPreview}
            style={{ color: getCategoryColor(record.name) }}
          >
            {renderCategoryIcon(record.icon)}
          </span>
          <Text strong style={{ color: '#0f172a' }}>
            {name}
          </Text>
        </Space>
      ),
    },
    {
      title: '描述说明',
      dataIndex: 'description',
      key: 'description',
      render: (desc: string) =>
        desc ? (
          <Text type="secondary" ellipsis={{ tooltip: desc }} style={{ maxWidth: 280 }}>
            {desc}
          </Text>
        ) : (
          <Text type="secondary">-</Text>
        ),
    },
    {
      title: '收录文章数',
      dataIndex: 'articleCount',
      key: 'articleCount',
      width: 120,
      align: 'center',
      render: (count: number) => (
        <Tag color={count > 0 ? 'cyan' : 'default'} style={{ borderRadius: 10, padding: '0 8px' }}>
          {count ?? 0} 篇
        </Tag>
      ),
    },
    {
      title: '排序权重',
      dataIndex: 'sort',
      key: 'sort',
      width: 100,
      align: 'center',
    },
    {
      title: '操作',
      key: 'action',
      width: 180,
      render: (_, record) => (
        <Space size="small">
          <PermissionGuard code={PERMISSIONS.CATEGORY_EDIT}>
            <Button
              size="small"
              type="link"
              icon={<EditOutlined />}
              onClick={() => handleOpenEdit(record)}
            >
              编辑
            </Button>
          </PermissionGuard>
          <PermissionGuard code={PERMISSIONS.CATEGORY_DELETE}>
            <Popconfirm
              title="确认删除该分类？"
              description={
                record.articleCount && record.articleCount > 0
                  ? `该分类下收录有 ${record.articleCount} 篇文章，删除前需先转移文章。`
                  : '删除后不可恢复，请谨慎操作。'
              }
              onConfirm={() => handleDelete(record)}
              okText="确认删除"
              cancelText="取消"
              okButtonProps={{ danger: true }}
            >
              <Button size="small" type="link" danger icon={<DeleteOutlined />}>
                删除
              </Button>
            </Popconfirm>
          </PermissionGuard>
        </Space>
      ),
    },
  ]

  return (
    <div className={styles.container}>
      {/* 顶部横幅 */}
      <div className={styles.welcomeBanner}>
        <Row align="middle" justify="space-between">
          <Col>
            <Title level={4} className={styles.welcomeTitle}>
              文章技术分类管理
            </Title>
            <Paragraph className={styles.welcomeSubtitle}>
              配置文章的技术分类维度，前台直显与文章发布动态联动。收录有文章的分类将受安全保护，防止误删破坏关联。
            </Paragraph>
          </Col>
          <Col>
            <Space>
              <Button icon={<ReloadOutlined />} onClick={fetchCategories} loading={loading}>
                刷新
              </Button>
              <PermissionGuard code={PERMISSIONS.CATEGORY_CREATE}>
                <Button type="primary" icon={<PlusOutlined />} onClick={handleOpenCreate}>
                  新建分类
                </Button>
              </PermissionGuard>
            </Space>
          </Col>
        </Row>
      </div>

      {/* 分类列表表格 */}
      <Card className={styles.categoryCard} variant="borderless">
        <Table<CategoryDto>
          rowKey={(record) => String(record.id)}
          columns={columns}
          dataSource={categories}
          loading={loading}
          pagination={false}
          size="middle"
        />
      </Card>

      {/* 创建 / 编辑分类弹窗 */}
      <Modal
        title={editingCategory ? `编辑分类 - ${editingCategory.name}` : '新建文章分类'}
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={submitting}
        okText="保存"
        cancelText="取消"
        width={500}
        destroyOnClose
      >
        <Form
          form={form}
          layout="vertical"
          style={{ marginTop: 16 }}
          initialValues={{
            sort: 0,
            icon: 'FolderOutlined',
          }}
        >
          <Form.Item
            name="name"
            label="分类名称"
            rules={[{ required: true, message: '请输入分类显示名称' }]}
          >
            <Input placeholder="如：.NET / C#、人工智能、前端工程" />
          </Form.Item>

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="icon" label="前端图标标识">
                <Select
                  showSearch
                  allowClear
                  placeholder="选择图标"
                  options={Object.keys(CATEGORY_ICON_MAP).map((key) => ({
                    label: (
                      <Space>
                        {CATEGORY_ICON_MAP[key]}
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
                tooltip="数字越小排列越靠前"
              >
                <InputNumber min={0} max={9999} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item name="description" label="分类详细描述">
            <Input.TextArea
              rows={3}
              placeholder="简要描述该技术分类覆盖的内容范围与核心主题..."
              maxLength={200}
              showCount
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
