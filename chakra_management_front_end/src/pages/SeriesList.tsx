import { useState, useEffect, useCallback, useRef } from 'react'
import {
  Table,
  Card,
  Button,
  Input,
  Space,
  Tag,
  Typography,
  Modal,
  Form,
  Popconfirm,
  Tooltip,
  App,
  Row,
  Col,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  PlusOutlined,
  SearchOutlined,
  ReloadOutlined,
  EditOutlined,
  DeleteOutlined,
  ExportOutlined,
  PictureOutlined,
  LoadingOutlined,
  UploadOutlined,
} from '@ant-design/icons'
import * as seriesApi from '@/api/series'
import { FRONT_URL } from '@/config/brand'
import { uploadMedia } from '@/api/media'
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'
import type { SeriesListItemDto, CreateSeriesRequest, UpdateSeriesRequest } from '@/types'
import dayjs from 'dayjs'
import styles from './SeriesList.module.css'

const { Title, Paragraph, Text } = Typography

// 预设主题色彩
const PRESET_THEMES = [
  { label: '经典天蓝', value: '#0284c7' },
  { label: '清新翡翠', value: '#10b981' },
  { label: '优雅星紫', value: '#8b5cf6' },
  { label: '深邃暗夜', value: '#0f172a' },
  { label: '暖阳琥珀', value: '#f59e0b' },
  { label: '热烈珊瑚', value: '#f43f5e' },
  { label: '清爽青碧', value: '#06b6d4' },
]

export default function SeriesList() {
  const { message } = App.useApp()
  const [form] = Form.useForm()

  const [loading, setLoading] = useState(false)
  const [data, setData] = useState<SeriesListItemDto[]>([])
  const [total, setTotal] = useState(0)
  const [pageIndex, setPageIndex] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [inputKeyword, setInputKeyword] = useState('')
  const [appliedKeyword, setAppliedKeyword] = useState('')

  // 新建/编辑弹窗状态
  const [modalOpen, setModalOpen] = useState(false)
  const [editingSeries, setEditingSeries] = useState<SeriesListItemDto | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // 封面上传状态
  const [coverUrl, setCoverUrl] = useState('')
  const [uploadingCover, setUploadingCover] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  // 当前选中的主题色
  const [selectedTheme, setSelectedTheme] = useState('#0284c7')

  // 加载数据
  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      const res = await seriesApi.getSeriesList({
        keyword: appliedKeyword.trim() || undefined,
        pageIndex,
        pageSize,
      })
      setData(res.items || [])
      setTotal(res.total || 0)
    } catch (err) {
      message.error(err instanceof Error ? err.message : '加载专栏列表失败')
    } finally {
      setLoading(false)
    }
  }, [appliedKeyword, pageIndex, pageSize, message])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  // 执行搜索
  const handleSearch = () => {
    setAppliedKeyword(inputKeyword)
    setPageIndex(1)
  }

  // 重置筛选
  const handleReset = () => {
    setInputKeyword('')
    setAppliedKeyword('')
    setPageIndex(1)
  }

  // 打开创建弹窗
  const handleOpenCreate = () => {
    setEditingSeries(null)
    setCoverUrl('')
    setSelectedTheme('#0284c7')
    form.resetFields()
    form.setFieldsValue({ theme: '#0284c7' })
    setModalOpen(true)
  }

  // 打开编辑弹窗
  const handleOpenEdit = (record: SeriesListItemDto) => {
    setEditingSeries(record)
    setCoverUrl(record.coverUrl || '')
    setSelectedTheme(record.theme || '#0284c7')
    form.setFieldsValue({
      name: record.name,
      description: record.description,
      theme: record.theme || '#0284c7',
      coverUrl: record.coverUrl || '',
    })
    setModalOpen(true)
  }

  // 封面图片上传
  const handleCoverUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    setUploadingCover(true)
    try {
      const res = await uploadMedia(file)
      setCoverUrl(res.url)
      form.setFieldsValue({ coverUrl: res.url })
      message.success('封面图上传成功')
    } catch (err) {
      message.error(err instanceof Error ? err.message : '封面图上传失败')
    } finally {
      setUploadingCover(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // 提交表单（创建或更新）
  const handleFormSubmit = async () => {
    try {
      const values = await form.validateFields()
      setSubmitting(true)

      if (editingSeries) {
        const updatePayload: UpdateSeriesRequest = {
          name: values.name,
          description: values.description,
          coverUrl: coverUrl || values.coverUrl,
          theme: values.theme || selectedTheme,
          version: editingSeries.version,
        }
        await seriesApi.updateSeries(editingSeries.id, updatePayload)
        message.success('专栏更新成功')
      } else {
        const createPayload: CreateSeriesRequest = {
          name: values.name,
          description: values.description,
          coverUrl: coverUrl || values.coverUrl,
          theme: values.theme || selectedTheme,
        }
        await seriesApi.createSeries(createPayload)
        message.success('专栏创建成功')
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

  // 删除专栏
  const handleDelete = async (id: string | number) => {
    try {
      await seriesApi.deleteSeries(id)
      message.success('专栏删除成功')
      fetchData()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '删除专栏失败')
    }
  }

  // 表格列定义
  const columns: ColumnsType<SeriesListItemDto> = [
    {
      title: '封面',
      dataIndex: 'coverUrl',
      key: 'coverUrl',
      width: 90,
      render: (cover: string) =>
        cover ? (
          <img src={cover} alt="封面" className={styles.coverThumb} />
        ) : (
          <div className={styles.coverPlaceholder}>
            <PictureOutlined />
          </div>
        ),
    },
    {
      title: '专栏系列名称',
      dataIndex: 'name',
      key: 'name',
      render: (name: string, record) => (
        <Space direction="vertical" size={2}>
          <Space align="center">
            {record.theme && (
              <span
                className={styles.themeBadge}
                style={{ backgroundColor: record.theme }}
              />
            )}
            <Text strong style={{ color: '#0f172a', fontSize: 15 }}>
              {name}
            </Text>
          </Space>
          {record.description && (
            <Text type="secondary" ellipsis style={{ maxWidth: 360, fontSize: 13 }}>
              {record.description}
            </Text>
          )}
        </Space>
      ),
    },
    {
      title: '收录文章数',
      dataIndex: 'articleCount',
      key: 'articleCount',
      width: 120,
      render: (count: number) => (
        <Tag color={count > 0 ? 'blue' : 'default'} style={{ borderRadius: 6, fontWeight: 600 }}>
          {count ?? 0} 篇文章
        </Tag>
      ),
    },
    {
      title: '创建人',
      dataIndex: 'author',
      key: 'author',
      width: 120,
      render: (author: string) => author || '博主',
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date: string) => (date ? dayjs(date).format('YYYY-MM-DD HH:mm') : '—'),
    },
    {
      title: '操作',
      key: 'action',
      width: 180,
      render: (_, record) => (
        <Space size={8}>
          <PermissionGuard code={PERMISSIONS.SERIES_EDIT}>
            <Tooltip title="编辑专栏">
              <Button
                type="text"
                icon={<EditOutlined />}
                onClick={() => handleOpenEdit(record)}
              />
            </Tooltip>
          </PermissionGuard>

          <Tooltip title="在前台新标签页查看专栏">
            <Button
              type="text"
              icon={<ExportOutlined />}
              onClick={() => window.open(`${FRONT_URL}/series/${record.id}`, '_blank')}
            />
          </Tooltip>

          <PermissionGuard code={PERMISSIONS.SERIES_DELETE}>
            <Popconfirm
              title="确定要删除该专栏系列吗？"
              description="删除后专栏将被软删除，归属于该专栏的文章将自动解除绑定。"
              okText="确定删除"
              cancelText="取消"
              okButtonProps={{ danger: true }}
              onConfirm={() => handleDelete(record.id)}
            >
              <Tooltip title="删除专栏">
                <Button type="text" danger icon={<DeleteOutlined />} />
              </Tooltip>
            </Popconfirm>
          </PermissionGuard>
        </Space>
      ),
    },
  ]

  return (
    <div className={styles.container}>
      {/* 头部标题与新建按钮 */}
      <div className={styles.header}>
        <div>
          <Title level={3} className={styles.title}>
            专栏系列管理
          </Title>
          <Paragraph className={styles.subtitle}>
            管理博客体系化专栏系列，支持专栏封面配置、主题色定制与收录文章联动
          </Paragraph>
        </div>
        <PermissionGuard code={PERMISSIONS.SERIES_CREATE}>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleOpenCreate}
            size="large"
            style={{ borderRadius: 8, height: 42 }}
          >
            新建专栏系列
          </Button>
        </PermissionGuard>
      </div>

      {/* 筛选过滤条 */}
      <Card className={styles.filterCard} styles={{ body: { padding: '16px 20px' } }}>
        <Row gutter={[16, 16]} align="middle" justify="space-between">
          <Col xs={24} sm={16} md={12}>
            <Input
              placeholder="按专栏名称搜索..."
              prefix={<SearchOutlined style={{ color: '#94a3b8' }} />}
              value={inputKeyword}
              onChange={(e) => setInputKeyword(e.target.value)}
              onPressEnter={handleSearch}
              allowClear
              onClear={() => {
                setInputKeyword('')
                setAppliedKeyword('')
                setPageIndex(1)
              }}
              style={{ borderRadius: 8 }}
            />
          </Col>
          <Col>
            <Space>
              <Button
                type="primary"
                icon={<SearchOutlined />}
                onClick={handleSearch}
              >
                搜索
              </Button>
              <Button
                icon={<ReloadOutlined />}
                onClick={handleReset}
              >
                重置
              </Button>
            </Space>
          </Col>
        </Row>
      </Card>

      {/* 专栏列表表格 */}
      <Card className={styles.tableCard} styles={{ body: { padding: 0 } }}>
        <Table<SeriesListItemDto>
          rowKey="id"
          columns={columns}
          dataSource={data}
          loading={loading}
          pagination={{
            current: pageIndex,
            pageSize,
            total,
            showSizeChanger: true,
            showTotal: (t) => `共 ${t} 个专栏系列`,
            onChange: (p, ps) => {
              setPageIndex(p)
              setPageSize(ps)
            },
          }}
        />
      </Card>

      {/* 新建/编辑专栏 Modal */}
      <Modal
        title={editingSeries ? '编辑专栏系列' : '新建专栏系列'}
        open={modalOpen}
        onOk={handleFormSubmit}
        onCancel={() => setModalOpen(false)}
        confirmLoading={submitting}
        okText={editingSeries ? '保存修改' : '立即创建'}
        cancelText="取消"
        width={560}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            label="专栏名称"
            name="name"
            rules={[{ required: true, message: '请输入专栏名称' }]}
          >
            <Input placeholder="例如：ASP.NET Core 深入实战、TypeScript 高级编程" maxLength={100} />
          </Form.Item>

          <Form.Item label="专栏简介 / 描述" name="description">
            <Input.TextArea
              rows={3}
              placeholder="概括该专栏收录的核心内容与技术栈..."
              maxLength={500}
              showCount
            />
          </Form.Item>

          <Form.Item label="专栏主题色彩" name="theme">
            <Space direction="vertical" style={{ width: '100%' }}>
              <div className={styles.colorPickerGroup}>
                {PRESET_THEMES.map((theme) => (
                  <Tooltip key={theme.value} title={theme.label}>
                    <div
                      className={`${styles.colorDot} ${
                        selectedTheme === theme.value ? styles.colorDotActive : ''
                      }`}
                      style={{ backgroundColor: theme.value }}
                      onClick={() => {
                        setSelectedTheme(theme.value)
                        form.setFieldsValue({ theme: theme.value })
                      }}
                    />
                  </Tooltip>
                ))}
              </div>
              <Input
                placeholder="支持十六进制颜色值，如 #0284c7"
                value={selectedTheme}
                onChange={(e) => {
                  setSelectedTheme(e.target.value)
                  form.setFieldsValue({ theme: e.target.value })
                }}
                style={{ marginTop: 8 }}
              />
            </Space>
          </Form.Item>

          <Form.Item label="专栏封面图 (云端媒体库)">
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              style={{ display: 'none' }}
              onChange={handleCoverUpload}
            />
            <Space direction="vertical" style={{ width: '100%' }}>
              <Space>
                <Button
                  icon={uploadingCover ? <LoadingOutlined /> : <UploadOutlined />}
                  onClick={() => fileInputRef.current?.click()}
                  loading={uploadingCover}
                >
                  上传封面图
                </Button>
                {coverUrl && (
                  <Button danger type="text" onClick={() => setCoverUrl('')}>
                    移除封面
                  </Button>
                )}
              </Space>

              {coverUrl && (
                <div style={{ marginTop: 8 }}>
                  <img
                    src={coverUrl}
                    alt="封面预览"
                    style={{
                      maxHeight: 120,
                      maxWidth: '100%',
                      borderRadius: 8,
                      border: '1px solid #e2e8f0',
                      objectFit: 'cover',
                    }}
                  />
                </div>
              )}
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
