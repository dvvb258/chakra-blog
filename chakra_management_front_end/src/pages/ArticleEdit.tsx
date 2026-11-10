import { useState, useEffect, useMemo, useRef, useCallback } from 'react'
import {
  Form,
  Input,
  Button,
  Select,
  InputNumber,
  Space,
  Spin,
  App,
  Card,
  Row,
  Col,
  Tag,
  Typography,
  Modal,
  Alert,
  Popconfirm,
  Switch,
} from 'antd'
import {
  ArrowLeftOutlined,
  SendOutlined,
  CheckCircleOutlined,
  ThunderboltOutlined,
  TagOutlined,
  BookOutlined,
  ClockCircleOutlined,
  ExportOutlined,
  CloudUploadOutlined,
  PictureOutlined,
  DeleteOutlined,
  LoadingOutlined,
  PlusOutlined,
  PushpinOutlined,
} from '@ant-design/icons'
import { useNavigate, useParams } from 'react-router-dom'
import MDEditor from '@uiw/react-md-editor'
import { useAuthStore } from '@/stores/authStore'
import { getFrontUrl } from '@/config/brand'
import * as articlesApi from '@/api/articles'
import * as categoriesApi from '@/api/categories'
import { uploadMedia } from '@/api/media'
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'
import { useMarkdownImageUpload } from '@/hooks/useMarkdownImageUpload'
import { ArticleStatus, type SeriesListItemDto, type CategoryDto } from '@/types'
import { POPULAR_TAGS, COLOR_OPTIONS, ARTICLE_STATUS_MAP } from '@/constants/article'
import styles from './ArticleEditor.module.css'

const { Title, Text } = Typography

export default function ArticleEdit() {
  const { message, modal } = App.useApp()
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const [form] = Form.useForm()
  const user = useAuthStore((s) => s.user)
  const isAdmin = user?.role === 'Admin'
  const [createdByUserId, setCreatedByUserId] = useState<string | number | null>(null)
  const isOwner = !createdByUserId || String(createdByUserId) === String(user?.id)
  const canEdit = isAdmin || isOwner

  const [content, setContent] = useState('')
  const [coverUrl, setCoverUrl] = useState('')
  const [uploadingCover, setUploadingCover] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [seriesList, setSeriesList] = useState<SeriesListItemDto[]>([])
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [popularTags, setPopularTags] = useState<string[]>(POPULAR_TAGS)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [currentStatus, setCurrentStatus] = useState<number>(ArticleStatus.Published)
  const [targetStatus, setTargetStatus] = useState<number | undefined>(undefined)
  const [version, setVersion] = useState<number>(0)
  const isSavedRef = useRef(false)
  const initialContentRef = useRef('')

  const [categoryModalOpen, setCategoryModalOpen] = useState(false)
  const [categorySubmitting, setCategorySubmitting] = useState(false)
  const [categoryForm] = Form.useForm()

  // 刷新分类列表
  const refreshCategories = useCallback(async () => {
    try {
      const res = await categoriesApi.getCategories()
      const list = Array.isArray(res) ? res : []
      setCategories(list)
      return list
    } catch {
      setCategories([])
      return []
    }
  }, [])

  // 快捷创建分类
  const handleQuickCreateCategory = async () => {
    try {
      const values = await categoryForm.validateFields()
      setCategorySubmitting(true)
      const newId = await categoriesApi.createCategory({
        name: values.name.trim(),
        description: values.description?.trim(),
      })
      message.success(`文章分类 [${values.name}] 创建成功`)
      await refreshCategories()
      if (newId) {
        form.setFieldValue('categoryId', String(newId))
      }
      setCategoryModalOpen(false)
      categoryForm.resetFields()
    } catch (err) {
      message.error(err instanceof Error ? err.message : '创建分类失败')
    } finally {
      setCategorySubmitting(false)
    }
  }

  // 动态字数与估算阅读时间
  const wordCount = useMemo(() => content.trim().length, [content])
  const estimatedTime = useMemo(() => Math.max(1, Math.ceil(wordCount / 350)), [wordCount])

  // 正文多维图片上传与插入 Hook（支持光标处插入、截图粘贴、拖拽上传、工具栏图片唤起）
  const {
    fileInputRef: contentFileInputRef,
    handlePaste: handleEditorPaste,
    handleDrop: handleEditorDrop,
    handleDragOver: handleEditorDragOver,
    handleToolbarFileSelect: handleContentImageUpload,
    editorCommands,
  } = useMarkdownImageUpload({
    content,
    setContent,
    message,
  })

  // 处理封面图片上传到云端对象存储
  const handleCoverUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    setUploadingCover(true)
    try {
      const res = await uploadMedia(file)
      setCoverUrl(res.url)
      message.success('封面图已成功上传')
    } catch (err) {
      message.error(err instanceof Error ? err.message : '封面图上传失败')
    } finally {
      setUploadingCover(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  // 加载专栏系列和文章详情
  useEffect(() => {
    if (!id) return

    const loadData = async () => {
      setLoading(true)
      try {
        const [articleData, seriesRes, categoriesRes, tagsRes] = await Promise.all([
          articlesApi.getArticle(id),
          articlesApi.getSeriesList().catch(() => []),
          categoriesApi.getCategories().catch(() => []),
          articlesApi.getTags(30).catch(() => []),
        ])

        const catList = Array.isArray(categoriesRes) ? categoriesRes : []
        setSeriesList(Array.isArray(seriesRes) ? seriesRes : [])
        setCategories(catList)

        if (Array.isArray(tagsRes) && tagsRes.length > 0) {
          setPopularTags(Array.from(new Set([...tagsRes, ...POPULAR_TAGS])))
        }

        if (articleData) {
          const ownerId = articleData.createdByUserId
          setCreatedByUserId(ownerId || null)

          const articleIsOwner = !ownerId || String(ownerId) === String(user?.id)
          // 权限隔离校验：非作者且非管理员无权查看他人文章
          if (!articleIsOwner && !isAdmin) {
            message.warning('权限不足：只有文章创建者本人才能查看此文章')
            navigate('/articles', { replace: true })
            return
          }

          const targetCatId = articleData.categoryId && String(articleData.categoryId) !== '0'
            ? String(articleData.categoryId)
            : undefined

          form.setFieldsValue({
            title: articleData.title,
            categoryId: targetCatId,
            seriesId: articleData.seriesId || undefined,
            tags: articleData.tags || [],
            readingTime: articleData.readingTime || 3,
            coverColor: articleData.coverColor || '#0284c7',
            summary: articleData.summary || '',
            isPinned: Boolean(articleData.isPinned),
          })
          setContent(articleData.content || '')
          initialContentRef.current = articleData.content || ''
          setCoverUrl(articleData.coverUrl || '')
          setCurrentStatus(articleData.status ?? ArticleStatus.Published)
          setVersion(articleData.version ?? 0)
        }
      } catch (err: unknown) {
        const errMsg = err instanceof Error ? err.message : '加载文章详情失败'
        message.error(errMsg)
        navigate('/articles', { replace: true })
      } finally {
        setLoading(false)
      }
    }

    loadData()
  }, [id, form, message, isAdmin, navigate, user?.id])

  // 页面离开未保存防丢保护
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (!isSavedRef.current && content !== initialContentRef.current) {
        e.preventDefault()
        e.returnValue = ''
      }
    }
    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => window.removeEventListener('beforeunload', handleBeforeUnload)
  }, [content])

  // 返回列表前检查未保存修改
  const handleBack = () => {
    if (content !== initialContentRef.current || form.isFieldsTouched()) {
      modal.confirm({
        title: '放弃未保存的修改？',
        content: '当前文章内容已有改动，离开本页后未保存的内容将会丢失。',
        okText: '确认离开',
        cancelText: '继续编辑',
        okButtonProps: { danger: true },
        onOk: () => navigate('/articles'),
      })
    } else {
      navigate('/articles')
    }
  }

  // 快捷键保存：Ctrl+S / Cmd+S
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
        e.preventDefault()
        form.submit()
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [form])

  // 管理员审核通过并发布文章
  const handleApprove = async () => {
    if (!id) return
    setSubmitting(true)
    try {
      await articlesApi.approveArticle(id)
      message.success('文章已审核通过并正式发布上线')
      navigate('/articles')
    } catch (err: unknown) {
      message.error(err instanceof Error ? err.message : '审核操作失败')
    } finally {
      setSubmitting(false)
    }
  }

  // 管理员驳回文章退回草稿箱
  const handleReject = async () => {
    if (!id) return
    setSubmitting(true)
    try {
      await articlesApi.rejectArticle(id)
      message.success('文章已驳回退回作者草稿箱')
      navigate('/articles')
    } catch (err: unknown) {
      message.error(err instanceof Error ? err.message : '驳回操作失败')
    } finally {
      setSubmitting(false)
    }
  }

  // 提交修改
  const handleSubmit = async (values: {
    title: string
    categoryId?: string | number
    seriesId?: string | number
    tags?: string[]
    readingTime?: number
    coverColor?: string
    summary?: string
    isPinned?: boolean
  }) => {
    if (!id) return

    if (!content.trim()) {
      message.warning('文章正文不能为空，请输入 Markdown 内容')
      return
    }

    setSubmitting(true)
    const finalTargetStatus = targetStatus !== undefined ? targetStatus
      : (!isAdmin && currentStatus === ArticleStatus.Published ? ArticleStatus.Draft : currentStatus)

    try {
      await articlesApi.updateArticle(id, {
        title: values.title.trim(),
        content,
        summary: values.summary?.trim() || undefined,
        categoryId: values.categoryId ? String(values.categoryId) : undefined,
        seriesId: values.seriesId ? String(values.seriesId) : undefined,
        tags: values.tags || [],
        readingTime: values.readingTime || estimatedTime,
        coverColor: values.coverColor || '#0284c7',
        coverUrl: coverUrl || undefined,
        status: finalTargetStatus,
        version: version >= 0 ? version : undefined,
        isPinned: isAdmin ? !!values.isPinned : undefined,
      })
      isSavedRef.current = true
      const successTip = finalTargetStatus === ArticleStatus.Published
        ? '文章已正式发布上线'
        : finalTargetStatus === ArticleStatus.PendingReview
        ? '文章已成功提交审核'
        : '文章修改已成功保存'
      message.success(successTip)
      navigate('/articles')
    } catch (err: unknown) {
      const errMsg = err instanceof Error ? err.message : '更新文章失败'
      if (errMsg.includes('版本') || errMsg.includes('409') || errMsg.includes('刷新页面')) {
        modal.error({
          title: '内容版本冲突（并发防覆盖）',
          content: errMsg,
          okText: '刷新获取最新内容',
          onOk: () => window.location.reload(),
        })
      } else {
        message.error(errMsg)
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '120px 0' }}>
        <Spin size="large" />
        <div style={{ color: '#94a3b8', marginTop: 16 }}>正在拉取文章正文与元数据...</div>
      </div>
    )
  }

  return (
    <div className={styles.container}>
      {/* 顶部固定操作导航（独立于 Form 外部，避免被 Form.disabled 连带禁用返回与审核按钮） */}
      <div className={styles.topBar}>
        <Space align="center" size={12}>
          <Button icon={<ArrowLeftOutlined />} onClick={handleBack}>
            返回列表
          </Button>
          <Title level={4} className={styles.pageTitle}>
            {canEdit ? '编辑文章' : '查看文章'}
          </Title>
          <Tag color={ARTICLE_STATUS_MAP[currentStatus as keyof typeof ARTICLE_STATUS_MAP]?.color || 'blue'}>
            {ARTICLE_STATUS_MAP[currentStatus as keyof typeof ARTICLE_STATUS_MAP]?.label || '未知状态'}
          </Tag>
          <Tag color="geekblue" title="当前内容版本（乐观并发锁保护）">
            v{version}
          </Tag>
        </Space>

        <Space size={12} align="center">
          <Button
            icon={<ExportOutlined />}
            onClick={() => window.open(`${getFrontUrl()}/articles/${id}`, '_blank')}
          >
            前台预览
          </Button>
          <Text className={styles.metaStatsText}>
            约 {wordCount} 字 · 预估阅读 {estimatedTime} 分钟
          </Text>

          {/* 管理员角色：具备全量编辑与发布权 */}
          {isAdmin ? (
            <>
              {currentStatus === ArticleStatus.PendingReview && (
                <>
                  <Popconfirm
                    title="确定驳回该文章？"
                    description="驳回后文章将退回草稿状态，由作者修改后重新提交。"
                    onConfirm={handleReject}
                    okText="确认驳回"
                    cancelText="取消"
                    okButtonProps={{ danger: true }}
                  >
                    <Button danger loading={submitting}>
                      驳回文章
                    </Button>
                  </Popconfirm>
                  <Button
                    loading={submitting}
                    onClick={handleApprove}
                    style={{ borderColor: '#10b981', color: '#10b981' }}
                  >
                    直接通过
                  </Button>
                </>
              )}

              <Button
                loading={submitting}
                onClick={() => {
                  setTargetStatus(ArticleStatus.Draft)
                  form.submit()
                }}
              >
                {currentStatus === ArticleStatus.Published ? '下线为草稿' : '保存为草稿'}
              </Button>

              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                loading={submitting}
                onClick={() => {
                  setTargetStatus(ArticleStatus.Published)
                  form.submit()
                }}
                style={{ backgroundColor: '#10b981', borderColor: '#10b981', padding: '0 20px', height: 38, fontWeight: 600 }}
              >
                {currentStatus === ArticleStatus.Published ? '保存修改' : '审核通过并发布'}
              </Button>
            </>
          ) : isOwner ? (
            /* 普通作者本人：展示草稿保存与提交审核 */
            <>
              <Button
                loading={submitting}
                onClick={() => {
                  setTargetStatus(ArticleStatus.Draft)
                  form.submit()
                }}
              >
                {currentStatus === ArticleStatus.Published ? '下线为草稿' : '保存草稿'}
              </Button>

              <Button
                type="primary"
                icon={<SendOutlined />}
                loading={submitting}
                onClick={() => {
                  setTargetStatus(ArticleStatus.PendingReview)
                  form.submit()
                }}
                style={{ padding: '0 20px', height: 38, fontWeight: 600 }}
              >
                {currentStatus === ArticleStatus.Published ? '保存并提审' : '提交审核'}
              </Button>
            </>
          ) : null}
        </Space>
      </div>

      <Form form={form} layout="vertical" onFinish={handleSubmit} disabled={!canEdit}>

        {/* 若为管理员操作他人/AI生成的文章，给出提示 */}
        {isAdmin && !isOwner && (
          <Alert
            type="info"
            showIcon
            message="管理员协同编辑模式"
            description={
              currentStatus === ArticleStatus.PendingReview
                ? "您正以超级管理员身份审阅此文章，可直接修改正文或在右上角执行通过/驳回。"
                : `您正以超级管理员身份编辑他人或系统生成的文章（作者ID: ${createdByUserId || '系统/AI'}），修改并保存后将直接生效。`
            }
            style={{ marginBottom: 16 }}
          />
        )}

        {/* 若是普通作者修改已公开文章，提示需重新提审 */}
        {!isAdmin && isOwner && currentStatus === ArticleStatus.Published && (
          <Alert
            type="warning"
            showIcon
            message="内容修改后需重新审核"
            description="修改已公开文章并保存后，文章将重新进入待审核状态，需由审核人员再次通过后发布。"
            style={{ marginBottom: 16 }}
          />
        )}

        <Row gutter={[24, 24]}>
          {/* 左侧主要区域：大标题 + Markdown 沉浸式编辑器 */}
          <Col xs={24} lg={17}>
            <Form.Item
              name="title"
              rules={[{ required: true, message: '请输入文章标题' }]}
              style={{ marginBottom: 16 }}
            >
              <Input
                placeholder="输入文章标题..."
                maxLength={200}
                className={styles.titleInput}
              />
            </Form.Item>

            {/* 隐藏的正文图片上传 input（由 MDEditor 工具栏的图片图标唤起） */}
            <input
              ref={contentFileInputRef}
              type="file"
              accept="image/*"
              multiple
              style={{ display: 'none' }}
              onChange={handleContentImageUpload}
            />

            {/* Markdown 编辑器 */}
            <div
              data-color-mode="light"
              className={styles.editorContainer}
              onPaste={handleEditorPaste}
              onDrop={handleEditorDrop}
              onDragOver={handleEditorDragOver}
            >
              <MDEditor
                value={content}
                onChange={(val) => setContent(val ?? '')}
                height={640}
                preview={!isOwner ? 'preview' : 'live'}
                visibleDragbar={false}
                commands={editorCommands}
              />
            </div>
          </Col>

          {/* 右侧属性配置抽屉 */}
          <Col xs={24} lg={7}>
            {/* 文章封面卡片 */}
            <Card
              title={
                <Space>
                  <PictureOutlined style={{ color: '#0284c7' }} />
                  <span className={styles.cardTitleText}>文章封面图</span>
                </Space>
              }
              className={styles.configCard}
              styles={{ header: { borderBottom: '1px solid #f1f5f9' } }}
            >
              <input
                type="file"
                ref={fileInputRef}
                style={{ display: 'none' }}
                accept="image/*"
                onChange={handleCoverUpload}
              />

              {coverUrl ? (
                <div className={styles.coverPreviewWrapper}>
                  <img src={coverUrl} alt="封面预览" className={styles.coverPreviewImg} />
                  <div className={styles.coverActions}>
                    <Button
                      type="text"
                      size="small"
                      icon={<CloudUploadOutlined style={{ color: '#fff' }} />}
                      onClick={() => fileInputRef.current?.click()}
                    >
                      <span style={{ color: '#fff' }}>替换</span>
                    </Button>
                    <Button
                      type="text"
                      size="small"
                      danger
                      icon={<DeleteOutlined />}
                      onClick={() => setCoverUrl('')}
                    >
                      移除
                    </Button>
                  </div>
                </div>
              ) : (
                <div
                  className={styles.coverUploadBox}
                  onClick={() => fileInputRef.current?.click()}
                >
                  {uploadingCover ? (
                    <LoadingOutlined className={styles.coverUploadIcon} />
                  ) : (
                    <CloudUploadOutlined className={styles.coverUploadIcon} />
                  )}
                  <div className={styles.coverUploadText}>
                    {uploadingCover ? '正在上传封面图...' : '点击或拖拽上传封面图'}
                  </div>
                  <div className={styles.coverUploadHint}>
                    支持 JPG, PNG, WebP（建议 16:9 比例）
                  </div>
                </div>
              )}
            </Card>

            <Card
              title={
                <Space>
                  <ThunderboltOutlined style={{ color: '#0284c7' }} />
                  <span className={styles.cardTitleText}>文章发布配置</span>
                </Space>
              }
              className={styles.configCard}
              styles={{ header: { borderBottom: '1px solid #f1f5f9' } }}
            >
              {/* 分类 */}
              <Form.Item
                name="categoryId"
                label={
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
                    <span>核心技术分类</span>
                    <PermissionGuard code={PERMISSIONS.CATEGORY_CREATE}>
                      <Button
                        type="link"
                        size="small"
                        icon={<PlusOutlined />}
                        style={{ padding: 0, height: 'auto', fontSize: 12 }}
                        onClick={() => setCategoryModalOpen(true)}
                      >
                        新建分类
                      </Button>
                    </PermissionGuard>
                  </div>
                }
                rules={[{ required: true, message: '请选择分类' }]}
              >
                <Select
                  options={categories.map((c) => ({ value: String(c.id), label: c.name }))}
                  style={{ width: '100%' }}
                  placeholder={categories.length > 0 ? '选择文章主分类' : '暂无分类'}
                  dropdownRender={(menu) => (
                    <>
                      {menu}
                      <PermissionGuard code={PERMISSIONS.CATEGORY_CREATE}>
                        <div style={{ padding: '8px', borderTop: '1px solid #f1f5f9' }}>
                          <Button
                            type="text"
                            size="small"
                            icon={<PlusOutlined />}
                            style={{ width: '100%', textAlign: 'left', color: '#0284c7' }}
                            onClick={() => setCategoryModalOpen(true)}
                          >
                            新建分类项...
                          </Button>
                        </div>
                      </PermissionGuard>
                    </>
                  )}
                />
              </Form.Item>

              {/* 所属系列 */}
              <Form.Item
                name="seriesId"
                label={
                  <Space>
                    <BookOutlined style={{ color: '#0284c7' }} />
                    <span>收录专栏系列 (可选)</span>
                  </Space>
                }
              >
                <Select
                  allowClear
                  placeholder="选择所属专题专栏"
                  options={(Array.isArray(seriesList) ? seriesList : []).map((s) => ({
                    value: s.id,
                    label: s.name,
                  }))}
                />
              </Form.Item>

              {/* 标签 */}
              <Form.Item
                name="tags"
                label={
                  <Space>
                    <TagOutlined style={{ color: '#0284c7' }} />
                    <span>技术标签 (回车添加)</span>
                  </Space>
                }
              >
                <Select
                  mode="tags"
                  placeholder="输入标签后按回车..."
                  style={{ width: '100%' }}
                  maxCount={10}
                  options={popularTags.map((t) => ({ value: t, label: t }))}
                />
              </Form.Item>

              {/* 预估阅读时间 */}
              <Form.Item
                name="readingTime"
                label={
                  <Space>
                    <ClockCircleOutlined style={{ color: '#0284c7' }} />
                    <span>阅读用时 (分钟)</span>
                  </Space>
                }
                extra={<Text type="secondary" style={{ fontSize: 12 }}>留空或为0将根据字数自动估算</Text>}
              >
                <InputNumber min={1} max={120} style={{ width: '100%' }} />
              </Form.Item>

              {/* 封面强调色 */}
              <Form.Item
                name="coverColor"
                label={<span>封面强调色</span>}
              >
                <Select options={COLOR_OPTIONS} style={{ width: '100%' }} />
              </Form.Item>

              {/* 置顶展示（仅限管理员配置） */}
              {isAdmin && (
                <Form.Item
                  name="isPinned"
                  label={
                    <Space>
                      <PushpinOutlined style={{ color: '#ef4444' }} />
                      <span>首页置顶展示</span>
                    </Space>
                  }
                  valuePropName="checked"
                  extra={<Text type="secondary" style={{ fontSize: 12 }}>开启后文章将在博客前台首页与文章列表置顶置首展示</Text>}
                >
                  <Switch checkedChildren="已置顶" unCheckedChildren="普通展示" />
                </Form.Item>
              )}
            </Card>

            {/* 摘要设置卡片 */}
            <Card
              title={<span className={styles.cardTitleText}>文章摘要</span>}
              className={styles.summaryCard}
              styles={{ header: { borderBottom: '1px solid #f1f5f9' } }}
            >
              <Form.Item
                name="summary"
                rules={[{ max: 500, message: '摘要不可超出500字' }]}
                extra={<Text type="secondary" style={{ fontSize: 12 }}>若留空，系统将自动从正文中提取前150字纯文本生成</Text>}
                style={{ marginBottom: 0 }}
              >
                <Input.TextArea
                  rows={4}
                  placeholder="自定义文章引言或摘要..."
                  maxLength={500}
                  showCount
                />
              </Form.Item>
            </Card>
          </Col>
        </Row>
      </Form>

      {/* 快捷新建分类弹窗 */}
      <Modal
        title="快捷新建文章分类"
        open={categoryModalOpen}
        onOk={handleQuickCreateCategory}
        onCancel={() => setCategoryModalOpen(false)}
        confirmLoading={categorySubmitting}
        okText="创建并选中"
        cancelText="取消"
        width={420}
        destroyOnClose
      >
        <Form form={categoryForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="name"
            label="分类显示名称"
            rules={[{ required: true, message: '请输入分类名称' }]}
          >
            <Input placeholder="如：前端工程、人工智能、架构设计" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
