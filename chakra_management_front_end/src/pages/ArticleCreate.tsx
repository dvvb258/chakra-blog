import { useState, useEffect, useMemo, useRef, useCallback } from 'react'
import {
  Form,
  Input,
  Button,
  Select,
  InputNumber,
  Space,
  App,
  Card,
  Row,
  Col,
  Tag,
  Typography,
  Modal,
  Switch,
} from 'antd'
import {
  ArrowLeftOutlined,
  SendOutlined,
  ThunderboltOutlined,
  TagOutlined,
  BookOutlined,
  ClockCircleOutlined,
  CloudUploadOutlined,
  PictureOutlined,
  DeleteOutlined,
  LoadingOutlined,
  PlusOutlined,
  PushpinOutlined,
} from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import MDEditor from '@uiw/react-md-editor'
import { useAuthStore } from '@/stores/authStore'
import * as articlesApi from '@/api/articles'
import * as categoriesApi from '@/api/categories'
import { uploadMedia } from '@/api/media'
import { useMarkdownImageUpload } from '@/hooks/useMarkdownImageUpload'
import { usePermission } from '@/hooks/usePermission'
import { PERMISSIONS } from '@/constants/permissions'
import PermissionGuard from '@/components/PermissionGuard'
import { ArticleStatus, type SeriesListItemDto, type CategoryDto } from '@/types'
import { POPULAR_TAGS, COLOR_OPTIONS } from '@/constants/article'
import styles from './ArticleEditor.module.css'

const { Title, Text } = Typography

export default function ArticleCreate() {
  const { message, modal } = App.useApp()
  const navigate = useNavigate()
  const user = useAuthStore((s) => s.user)
  const [form] = Form.useForm()

  const [content, setContent] = useState('')
  const [coverUrl, setCoverUrl] = useState('')
  const [uploadingCover, setUploadingCover] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [seriesList, setSeriesList] = useState<SeriesListItemDto[]>([])
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [popularTags, setPopularTags] = useState<string[]>(POPULAR_TAGS)
  const [submitting, setSubmitting] = useState(false)
  const isSubmittedRef = useRef(false)
  const isAdmin = user?.role === 'Admin'
  const { hasPermission } = usePermission()
  const [targetStatus, setTargetStatus] = useState<number>(ArticleStatus.Draft)

  // 权限防线：未获得创作文章权限的普通用户禁止进入创作工作台
  useEffect(() => {
    if (!isAdmin && !hasPermission(PERMISSIONS.ARTICLE_CREATE)) {
      message.warning('权限不足：您当前未获得新建文章权限')
      navigate('/articles', { replace: true })
    }
  }, [isAdmin, hasPermission, navigate, message])

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

  const [categoryModalOpen, setCategoryModalOpen] = useState(false)
  const [categorySubmitting, setCategorySubmitting] = useState(false)
  const [categoryForm] = Form.useForm()

  // 刷新分类列表
  const refreshCategories = useCallback(async () => {
    try {
      const res = await categoriesApi.getCategories()
      const list = Array.isArray(res) ? res : []
      setCategories(list)
      // 若当前尚未选中分类，自动选中首个可用分类
      if (!form.getFieldValue('categoryId') && list.length > 0) {
        form.setFieldValue('categoryId', String(list[0].id))
      }
      return list
    } catch {
      setCategories([])
      return []
    }
  }, [form])

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

  // 加载专栏、分类与热门标签列表
  useEffect(() => {
    articlesApi
      .getSeriesList()
      .then((res) => setSeriesList(Array.isArray(res) ? res : []))
      .catch(() => setSeriesList([]))

    articlesApi
      .getTags(30)
      .then((tags) => {
        if (Array.isArray(tags) && tags.length > 0) {
          const merged = Array.from(new Set([...tags, ...POPULAR_TAGS]))
          setPopularTags(merged)
        }
      })
      .catch(() => {})

    refreshCategories()
  }, [refreshCategories])
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (!isSubmittedRef.current && (content.trim().length > 10 || form.getFieldValue('title'))) {
        e.preventDefault()
        e.returnValue = ''
      }
    }
    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => window.removeEventListener('beforeunload', handleBeforeUnload)
  }, [content, form])

  // 返回列表前检查未保存内容
  const handleBack = () => {
    if (content.trim().length > 10 || form.getFieldValue('title')) {
      modal.confirm({
        title: '确定放弃当前创作？',
        content: '当前有尚未发布的文章内容，离开本页后未保存的内容将会丢失。',
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

  // 提交发布
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
    if (!content.trim()) {
      message.warning('文章正文不能为空，请输入 Markdown 内容')
      return
    }

    setSubmitting(true)
    try {
      await articlesApi.createArticle({
        title: values.title.trim(),
        content,
        summary: values.summary?.trim() || undefined,
        categoryId: values.categoryId ? String(values.categoryId) : undefined,
        seriesId: values.seriesId ? String(values.seriesId) : undefined,
        tags: values.tags || [],
        author: user?.nickname || user?.username || '博主',
        readingTime: values.readingTime || estimatedTime,
        coverColor: values.coverColor || '#0284c7',
        coverUrl: coverUrl || undefined,
        status: targetStatus,
        isPinned: isAdmin ? !!values.isPinned : false,
      })
      isSubmittedRef.current = true
      const successTip = targetStatus === ArticleStatus.Published
        ? '文章已正式发布上线'
        : targetStatus === ArticleStatus.PendingReview
        ? '文章已成功提交管理员审核'
        : '文章草稿已成功保存'
      message.success(successTip)
      navigate('/articles')
    } catch (err) {
      message.error(err instanceof Error ? err.message : '文章发布失败')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className={styles.container}>
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        initialValues={{
          coverColor: '#0284c7',
          readingTime: 3,
        }}
      >
        {/* 顶部固定操作导航 */}
        <div className={styles.topBar}>
          <Space align="center" size={12}>
            <Button icon={<ArrowLeftOutlined />} onClick={handleBack}>
              返回列表
            </Button>
            <Title level={4} className={styles.pageTitle}>
              创作新文章
            </Title>
            <Tag color="blue">MARKDOWN</Tag>
          </Space>

          <Space size={12} align="center">
            <Text className={styles.metaStatsText}>
              约 {wordCount} 字 · 预估阅读 {estimatedTime} 分钟
            </Text>
            
            {/* 存为草稿按钮 */}
            <Button
              loading={submitting}
              onClick={() => {
                setTargetStatus(ArticleStatus.Draft)
                form.submit()
              }}
            >
              保存草稿
            </Button>

            {/* 提交审核按钮 */}
            <Button
              type="primary"
              icon={<SendOutlined />}
              loading={submitting}
              onClick={() => {
                setTargetStatus(ArticleStatus.PendingReview)
                form.submit()
              }}
              style={{ padding: '0 24px', height: 38, fontWeight: 600 }}
            >
              提交审核
            </Button>
          </Space>
        </div>

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
                preview="live"
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
                  style={{
                    background: '#111827',
                    borderColor: '#334155',
                    color: '#e2e8f0',
                  }}
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
