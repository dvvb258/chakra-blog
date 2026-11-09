import { useState, useEffect, useCallback, useMemo } from 'react'
import {
  Table,
  Button,
  Space,
  Input,
  Select,
  Tag,
  Popconfirm,
  App,
  Card,
  Tooltip,
  Typography,
} from 'antd'
import {
  PlusOutlined,
  SearchOutlined,
  EditOutlined,
  DeleteOutlined,
  ExportOutlined,
  ReloadOutlined,
  EyeOutlined,
  ClockCircleOutlined,
  BookOutlined,
  PictureOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  SendOutlined,
} from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import dayjs from 'dayjs'
import { useAuthStore } from '@/stores/authStore'
import { getFrontUrl } from '@/config/brand'
import * as articlesApi from '@/api/articles'
import * as categoriesApi from '@/api/categories'
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'
import { ArticleStatus, type ArticleListItemDto, type SeriesListItemDto, type CategoryDto } from '@/types'
import { getCategoryColor, ARTICLE_STATUS_MAP, ARTICLE_STATUS_OPTIONS } from '@/constants/article'
import styles from './ArticleList.module.css'

const { Title, Text } = Typography

export default function ArticleList() {
  const { message } = App.useApp()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const isAdmin = user?.role === 'Admin'

  const [data, setData] = useState<ArticleListItemDto[]>([])
  const [seriesList, setSeriesList] = useState<SeriesListItemDto[]>([])
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [loading, setLoading] = useState(false)
  const [total, setTotal] = useState(0)

  // 筛选与分页状态
  const [pageIndex, setPageIndex] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [inputKeyword, setInputKeyword] = useState('')
  const [appliedKeyword, setAppliedKeyword] = useState('')
  const [categoryId, setCategoryId] = useState<string | number | undefined>(undefined)
  const [seriesId, setSeriesId] = useState<string | number | undefined>(undefined)
  const [status, setStatus] = useState<number | undefined>(undefined)

  // 动态分类映射表（ID => 显示名称）
  const categoryMap = useMemo(() => {
    const map = new Map<string, string>()
    categories.forEach((c) => {
      map.set(String(c.id), c.name)
    })
    return map
  }, [categories])

  // 加载系列与分类列表
  useEffect(() => {
    articlesApi
      .getSeriesList()
      .then((res) => setSeriesList(Array.isArray(res) ? res : []))
      .catch(() => setSeriesList([]))

    categoriesApi
      .getCategories()
      .then((res) => setCategories(Array.isArray(res) ? res : []))
      .catch(() => setCategories([]))
  }, [])

  // 加载文章分页数据
  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      const result = await articlesApi.getArticles({
        pageIndex,
        pageSize,
        keyword: appliedKeyword.trim() || undefined,
        categoryId: categoryId || undefined,
        seriesId: seriesId || undefined,
        status,
      })
      setData(result?.items || [])
      setTotal(result?.total || 0)
    } catch (err) {
      message.error(err instanceof Error ? err.message : '获取文章列表失败')
    } finally {
      setLoading(false)
    }
  }, [pageIndex, pageSize, appliedKeyword, categoryId, seriesId, status, message])

  useEffect(() => {
    fetchData()
  }, [fetchData])

  // 处理删除
  const handleDelete = async (id: string | number) => {
    try {
      await articlesApi.deleteArticle(id)
      message.success('文章已成功删除')
      fetchData()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '删除文章失败')
    }
  }

  // 管理员审核通过
  const handleApprove = async (id: string | number) => {
    try {
      await articlesApi.approveArticle(id)
      message.success('文章已审核通过并正式发布')
      fetchData()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '审核操作失败')
    }
  }

  // 管理员审核驳回
  const handleReject = async (id: string | number) => {
    try {
      await articlesApi.rejectArticle(id)
      message.success('文章已退回为草稿状态')
      fetchData()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '驳回操作失败')
    }
  }

  // 编辑者提交审核
  const handleSubmitReview = async (id: string | number) => {
    try {
      await articlesApi.submitArticleReview(id)
      message.success('已提交管理员审核，请耐心等待')
      fetchData()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '提交审核失败')
    }
  }

  // 分页改变
  const handleTableChange = (pagination: TablePaginationConfig) => {
    setPageIndex(pagination.current ?? 1)
    setPageSize(pagination.pageSize ?? 10)
  }

  // 执行搜索
  const handleSearch = () => {
    setAppliedKeyword(inputKeyword)
    setPageIndex(1)
  }

  // 重置筛选
  const handleReset = () => {
    setInputKeyword('')
    setAppliedKeyword('')
    setCategoryId(undefined)
    setSeriesId(undefined)
    setStatus(undefined)
    setPageIndex(1)
  }

  // 表格列定义
  const columns: ColumnsType<ArticleListItemDto> = [
    {
      title: '文章标题',
      dataIndex: 'title',
      key: 'title',
      render: (text: string, record) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12, maxWidth: 400 }}>
          {record.coverUrl ? (
            <img
              src={record.coverUrl}
              alt="封面"
              style={{ width: 48, height: 36, objectFit: 'cover', borderRadius: 6, flexShrink: 0, border: '1px solid #e2e8f0' }}
            />
          ) : (
            <div
              style={{
                width: 48,
                height: 36,
                borderRadius: 6,
                background: '#f1f5f9',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#94a3b8',
                fontSize: 14,
                flexShrink: 0,
              }}
            >
              <PictureOutlined />
            </div>
          )}
          <div style={{ flex: 1, minWidth: 0 }}>
            <Space align="center" size={6}>
              <Text strong className={styles.articleTitleText}>
                {text}
              </Text>
              <Tooltip title="在公开前台查看">
                <ExportOutlined
                  style={{ color: '#94a3b8', fontSize: 12, cursor: 'pointer' }}
                  onClick={() => window.open(`${getFrontUrl()}/articles/${record.id}`, '_blank')}
                />
              </Tooltip>
            </Space>
            {record.summary && (
              <div className={styles.articleSummaryText}>
                {record.summary}
              </div>
            )}
          </div>
        </div>
      ),
    },
    {
      title: '分类',
      dataIndex: 'categoryName',
      key: 'categoryName',
      width: 130,
      render: (name: string, record) => {
        const label = name || (record.categoryId ? categoryMap.get(String(record.categoryId)) : null)
        if (!label) return <Tag style={{ margin: 0 }}>未分类</Tag>
        const color = getCategoryColor(label)
        return (
          <Tag
            style={{
              margin: 0,
              fontWeight: 500,
              color: color,
              borderColor: `${color}33`,
              background: `${color}10`,
            }}
          >
            {label}
          </Tag>
        )
      },
    },
    {
      title: '标签',
      dataIndex: 'tags',
      key: 'tags',
      width: 180,
      render: (tags: string[]) => (
        <Space size={[0, 4]} wrap>
          {tags && tags.length > 0 ? (
            tags.map((t) => (
              <Tag key={t} bordered={false} style={{ fontSize: 11, background: '#f1f5f9', color: '#475569' }}>
                {t}
              </Tag>
            ))
          ) : (
            <Text type="secondary" style={{ fontSize: 12 }}>—</Text>
          )}
        </Space>
      ),
    },
    {
      title: '阅读指标',
      key: 'metrics',
      width: 140,
      render: (_, record) => (
        <Space direction="vertical" size={2} className={styles.metricsText}>
          <span>
            <EyeOutlined style={{ marginRight: 4 }} />
            {record.views ?? 0} 次阅读
          </span>
          <span>
            <ClockCircleOutlined style={{ marginRight: 4 }} />
            {record.readingTime || 1} 分钟
          </span>
        </Space>
      ),
    },
    {
      title: '所属系列',
      dataIndex: 'seriesId',
      key: 'seriesId',
      width: 140,
      render: (sid: string | number | null) => {
        if (!sid) return <Text type="secondary" style={{ fontSize: 12 }}>无归属</Text>
        const list = Array.isArray(seriesList) ? seriesList : []
        const s = list.find((item) => String(item.id) === String(sid))
        return s ? (
          <Tag icon={<BookOutlined />} color="cyan" style={{ margin: 0 }}>
            {s.name}
          </Tag>
        ) : (
          <Tag color="cyan" style={{ margin: 0 }}>系列 #{sid}</Tag>
        )
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (st: number) => {
        const info = ARTICLE_STATUS_MAP[st as keyof typeof ARTICLE_STATUS_MAP] || { label: '未知', color: 'default' }
        return (
          <Tag color={info.color} style={{ fontWeight: 600, borderRadius: 4 }}>
            {info.label}
          </Tag>
        )
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 150,
      render: (val: string) => (
        <Text className={styles.dateText}>
          {val ? dayjs(val).format('YYYY-MM-DD HH:mm') : '—'}
        </Text>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 220,
      fixed: 'right',
      render: (_, record) => {
        const isOwner = !record.createdByUserId || String(record.createdByUserId) === String(user?.id)
        // 方案 A 归属权隔离：删除操作允许作者或管理员
        const canDelete = isAdmin || isOwner

        return (
          <Space size={0} wrap>
            {/* 待审核状态：审核人员（具有 ARTICLE_APPROVE 权限）核验通过 */}
            {record.status === ArticleStatus.PendingReview && (
              <PermissionGuard code={PERMISSIONS.ARTICLE_APPROVE}>
                <Button
                  type="link"
                  size="small"
                  icon={<CheckCircleOutlined style={{ color: '#10b981' }} />}
                  style={{ color: '#10b981' }}
                  onClick={() => handleApprove(record.id)}
                >
                  通过
                </Button>
              </PermissionGuard>
            )}

            {/* 待审核状态：审核人员（具有 ARTICLE_APPROVE 权限）驳回退回草稿 */}
            {record.status === ArticleStatus.PendingReview && (
              <PermissionGuard code={PERMISSIONS.ARTICLE_APPROVE}>
                <Popconfirm
                  title="确定驳回该文章？"
                  description="驳回后文章将退回草稿状态，由作者修改后重新提交。"
                  onConfirm={() => handleReject(record.id)}
                  okText="确认驳回"
                  cancelText="取消"
                  okButtonProps={{ danger: true }}
                >
                  <Button type="link" size="small" danger icon={<CloseCircleOutlined />}>
                    驳回
                  </Button>
                </Popconfirm>
              </PermissionGuard>
            )}

            {/* 草稿或退回状态：创作者本人（无论是普通作者还是管理员）均可点击提审 */}
            {(record.status === ArticleStatus.Draft || record.status === ArticleStatus.Rejected) && isOwner && (
              <PermissionGuard code={PERMISSIONS.ARTICLE_SUBMIT}>
                <Button
                  type="link"
                  size="small"
                  icon={<SendOutlined />}
                  onClick={() => handleSubmitReview(record.id)}
                >
                  提审
                </Button>
              </PermissionGuard>
            )}

            {/* 作者本人或超级管理员均具备【编辑】权限 */}
            {(isOwner || isAdmin) ? (
              <PermissionGuard code={PERMISSIONS.ARTICLE_EDIT}>
                <Button
                  type="link"
                  size="small"
                  icon={<EditOutlined />}
                  onClick={() => navigate(`/articles/${record.id}/edit`)}
                >
                  编辑
                </Button>
              </PermissionGuard>
            ) : (
              <Button
                type="link"
                size="small"
                icon={<EyeOutlined />}
                onClick={() => navigate(`/articles/${record.id}/edit`)}
              >
                查看
              </Button>
            )}

            {/* 作者可删除自己的文章，管理员具备运维违规清理权 */}
            {canDelete && (
              <PermissionGuard code={PERMISSIONS.ARTICLE_DELETE}>
                <Popconfirm
                  title="确认软删除此文章？"
                  description="删除后公开前台将不再展示，文章仍可在数据库中审计。"
                  onConfirm={() => handleDelete(record.id)}
                  okText="确认删除"
                  cancelText="取消"
                  okButtonProps={{ danger: true }}
                >
                  <Button type="link" size="small" danger icon={<DeleteOutlined />}>
                    删除
                  </Button>
                </Popconfirm>
              </PermissionGuard>
            )}
          </Space>
        )
      },
    },
  ]

  return (
    <div>
      {/* 头部标题与新建入口 */}
      <div className={styles.headerContainer}>
        <Space align="center" size={12}>
          <Title level={3} className={styles.title}>
            文章管理
          </Title>
          <Tag color="blue" style={{ fontSize: 12, borderRadius: 10 }}>
            共 {total} 篇
          </Tag>
        </Space>

        <Space>
          <Button icon={<ReloadOutlined />} onClick={fetchData} loading={loading}>
            刷新
          </Button>
          <PermissionGuard code={PERMISSIONS.ARTICLE_CREATE}>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate('/articles/create')}
            >
              创作新文章
            </Button>
          </PermissionGuard>
        </Space>
      </div>

      {/* 筛选过滤工具条 */}
      <Card
        className={styles.filterCard}
        styles={{ body: { padding: '16px 20px' } }}
      >
        <Space wrap size={16} align="center">
          <Input
            placeholder="搜索标题或摘要关键词..."
            prefix={<SearchOutlined style={{ color: '#64748b' }} />}
            value={inputKeyword}
            onChange={(e) => setInputKeyword(e.target.value)}
            onPressEnter={handleSearch}
            style={{ width: 260 }}
            allowClear
            onClear={() => {
              setInputKeyword('')
              setAppliedKeyword('')
              setPageIndex(1)
            }}
          />

          <Select
            placeholder="按分类筛选"
            value={categoryId}
            onChange={(val) => {
              setCategoryId(val)
              setPageIndex(1)
            }}
            style={{ width: 150 }}
            allowClear
            options={categories.map((c) => ({
              value: c.id,
              label: c.name,
            }))}
          />

          <Select
            placeholder="按所属系列筛选"
            value={seriesId}
            onChange={(val) => {
              setSeriesId(val)
              setPageIndex(1)
            }}
            style={{ width: 170 }}
            allowClear
            options={(Array.isArray(seriesList) ? seriesList : []).map((s) => ({
              value: s.id,
              label: s.name,
            }))}
          />

          <Select
            placeholder="文章状态"
            value={status}
            onChange={(val) => {
              setStatus(val)
              setPageIndex(1)
            }}
            style={{ width: 120 }}
            allowClear
            options={ARTICLE_STATUS_OPTIONS}
          />

          <Button type="primary" icon={<SearchOutlined />} onClick={handleSearch}>
            搜索
          </Button>

          <Button onClick={handleReset}>
            重置
          </Button>
        </Space>
      </Card>

      {/* 表格容器 */}
      <Table<ArticleListItemDto>
        columns={columns}
        dataSource={data}
        rowKey="id"
        loading={loading}
        pagination={{
          current: pageIndex,
          pageSize,
          total,
          showSizeChanger: true,
          pageSizeOptions: ['10', '20', '50'],
          showTotal: (tot) => `共 ${tot} 篇文章`,
        }}
        onChange={handleTableChange}
        className={styles.tableCard}
      />
    </div>
  )
}
