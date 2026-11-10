import React, { useEffect, useState, useRef, useMemo } from 'react'
import {
  Alert,
  App,
  Button,
  Card,
  Col,
  Divider,
  Form,
  Input,
  Popconfirm,
  Row,
  Space,
  Tabs,
  Tag,
  Tooltip,
  Typography,
} from 'antd'
import {
  CheckCircleFilled,
  CheckOutlined,
  CloudUploadOutlined,
  CodeOutlined,
  CompassOutlined,
  ExportOutlined,
  FireOutlined,
  GithubOutlined,
  GlobalOutlined,
  InfoCircleOutlined,
  LoadingOutlined,
  MailOutlined,
  PictureOutlined,
  ReloadOutlined,
  SaveOutlined,
  SettingOutlined,
  ThunderboltOutlined,
  UndoOutlined,
  UserOutlined,
} from '@ant-design/icons'
import * as settingsApi from '@/api/settings'
import { uploadMedia } from '@/api/media'
import { setFrontUrl, getFrontUrl } from '@/config/brand'
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'
import styles from './SiteSettings.module.css'

const { Title, Text, Paragraph } = Typography

type Overrides = Record<string, unknown>

/**
 * 站点代码预置默认值定义（与展示端 chakra_front_end/src/config/site.ts 保持 100% 对齐）
 */
export const DEFAULT_SITE_SETTINGS = {
  themePreset: 'tech' as 'tech' | 'anime',
  name: 'Chakra',
  siteName: 'chakra.blog',
  url: 'http://localhost:3000',
  title: 'Chakra - 个人技术博客',
  description: '技术博客分享',
  keywords: '.NET, C#, 微服务, AI Agent',
  avatar: '/images/naruto/akatsuki-avatar.webp',
  defaultCover: '/images/naruto/cover-konoha.webp',
  workshopName: '技术空间',
  quote: '木叶飞舞之处，火亦生生不息',
  stationStatus: '在线运行',
  footerMotto: '所有的奇迹，都始于日常的专注',
  poweredBy: 'Powered by Chakra (.NET 10 & Next.js 15)',
  authorSubtitle: 'Chakra 博主',
  authorIntro: 'C#、微服务、Agent等架构实践。',
  github: 'https://github.com/dvvb258/chakra-blog',
  email: '',
} as const

export interface FormValues {
  themePreset?: 'tech' | 'anime'
  name?: string
  siteName?: string
  title?: string
  description?: string
  keywords?: string
  url?: string
  avatar?: string
  defaultCover?: string
  authorSubtitle?: string
  authorIntro?: string
  quote?: string
  footerMotto?: string
  poweredBy?: string
  github?: string
  email?: string
}

function asText(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() !== '' ? value.trim() : undefined
}

/**
 * 将后台保存的覆盖项回填到表单中
 */
function toFormValues(overrides: Overrides): FormValues {
  const socials = (overrides.socials ?? {}) as Overrides
  const keywords = overrides.keywords

  return {
    themePreset: overrides.themePreset === 'anime' ? 'anime' : 'tech',
    name: asText(overrides.name),
    siteName: asText(overrides.siteName),
    title: asText(overrides.title),
    description: asText(overrides.description),
    keywords: Array.isArray(keywords) ? keywords.join(', ') : asText(keywords),
    url: asText(overrides.url),
    avatar: asText(overrides.avatar),
    defaultCover: asText(overrides.defaultCover),
    authorSubtitle: asText(overrides.authorSubtitle),
    authorIntro: asText(overrides.authorIntro),
    quote: asText(overrides.quote),
    footerMotto: asText(overrides.footerMotto),
    poweredBy: asText(overrides.poweredBy),
    github: asText(socials.github),
    email: asText(socials.email),
  }
}

/**
 * 在覆盖对象上按路径写入或删除字段（空值表示删除覆盖，回落代码默认值）
 */
function setOrDelete(target: Overrides, path: string[], value: unknown): void {
  const [head, ...rest] = path
  const isEmpty =
    value === undefined ||
    value === null ||
    value === '' ||
    (Array.isArray(value) && value.length === 0)

  if (rest.length === 0) {
    if (isEmpty) {
      delete target[head]
    } else {
      target[head] = value
    }
    return
  }

  if (isEmpty) {
    const child = target[head]
    if (child && typeof child === 'object' && !Array.isArray(child)) {
      setOrDelete(child as Overrides, rest, value)
      if (Object.keys(child as Overrides).length === 0) {
        delete target[head]
      }
    }
    return
  }

  if (!target[head] || typeof target[head] !== 'object' || Array.isArray(target[head])) {
    target[head] = {}
  }
  setOrDelete(target[head] as Overrides, rest, value)
}

export default function SiteSettings() {
  const { message } = App.useApp()
  const [form] = Form.useForm<FormValues>()

  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [jsonText, setJsonText] = useState('{}')
  const [activeTab, setActiveTab] = useState('theme')

  // 用于右侧所见即所得实时预览的表单镜像数据
  const [currentFormValues, setCurrentFormValues] = useState<FormValues>({
    themePreset: 'tech',
  })

  // 上传图片相关状态
  const [uploadingAvatar, setUploadingAvatar] = useState(false)
  const [uploadingCover, setUploadingCover] = useState(false)
  const avatarInputRef = useRef<HTMLInputElement>(null)
  const coverInputRef = useRef<HTMLInputElement>(null)

  // 加载当前站点设置覆盖项
  const loadSettings = async () => {
    setLoading(true)
    try {
      const overrides = (await settingsApi.getSiteSettings()) ?? {}
      setJsonText(JSON.stringify(overrides, null, 2))
      const parsedValues = toFormValues(overrides)
      form.resetFields()
      form.setFieldsValue(parsedValues)
      setCurrentFormValues(parsedValues)
    } catch {
      message.error('加载站点设置失败')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadSettings()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // 快捷键 Ctrl+S / Cmd+S 触发保存
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
        event.preventDefault()
        handleSave()
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  })

  // 保存设置到后端
  const handleSave = async () => {
    let values: FormValues
    try {
      values = await form.validateFields()
    } catch {
      setActiveTab('basic')
      message.warning('请检查基础信息中的输入项格式是否正确')
      return
    }

    let base: Overrides
    try {
      const parsed = JSON.parse(jsonText || '{}')
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
        throw new Error('配置必须是 JSON 对象')
      }
      base = parsed as Overrides
    } catch {
      message.error('高级 JSON 格式不正确，请切换到【高级配置】排查后再保存')
      setActiveTab('advanced')
      return
    }

    const merged: Overrides = JSON.parse(JSON.stringify(base))

    setOrDelete(merged, ['themePreset'], values.themePreset)
    setOrDelete(merged, ['name'], values.name?.trim())
    setOrDelete(merged, ['siteName'], values.siteName?.trim())
    setOrDelete(merged, ['title'], values.title?.trim())
    setOrDelete(merged, ['description'], values.description?.trim())
    setOrDelete(
      merged,
      ['keywords'],
      values.keywords
        ?.split(',')
        .map((item) => item.trim())
        .filter(Boolean)
    )
    setOrDelete(merged, ['url'], values.url?.trim())
    setOrDelete(merged, ['avatar'], values.avatar?.trim())
    setOrDelete(merged, ['defaultCover'], values.defaultCover?.trim())
    setOrDelete(merged, ['authorSubtitle'], values.authorSubtitle?.trim())
    setOrDelete(merged, ['authorIntro'], values.authorIntro?.trim())
    setOrDelete(merged, ['quote'], values.quote?.trim())
    setOrDelete(merged, ['footerMotto'], values.footerMotto?.trim())
    setOrDelete(merged, ['poweredBy'], values.poweredBy?.trim())
    setOrDelete(merged, ['socials', 'github'], values.github?.trim())
    setOrDelete(merged, ['socials', 'email'], values.email?.trim())

    setSaving(true)
    try {
      await settingsApi.updateSiteSettings(merged)
      setJsonText(JSON.stringify(merged, null, 2))
      setFrontUrl(typeof merged.url === 'string' ? merged.url : undefined)
      message.success('站点设置已成功保存！前台展示端 60 秒内全局生效')
    } catch {
      message.error('保存站点设置失败，请稍后重试')
    } finally {
      setSaving(false)
    }
  }

  // 恢复所有配置为代码默认值（清空覆盖项）
  const handleReset = async () => {
    setJsonText('{}')
    form.resetFields()
    form.setFieldsValue({ themePreset: 'tech' })
    setCurrentFormValues({ themePreset: 'tech' })
    setFrontUrl(undefined)
    setSaving(true)
    try {
      await settingsApi.updateSiteSettings({})
      message.success('已全部恢复系统代码默认配置')
    } catch {
      message.error('恢复默认配置失败')
    } finally {
      setSaving(false)
    }
  }

  // 表单字段变动同步到实时预览镜像
  const handleValuesChange = (_changed: Partial<FormValues>, allValues: FormValues) => {
    setCurrentFormValues({ ...allValues })
  }

  // 单字段快捷操作：填入系统默认值
  const handleFillDefault = (field: keyof FormValues) => {
    const defaultValue = DEFAULT_SITE_SETTINGS[field as keyof typeof DEFAULT_SITE_SETTINGS]
    if (defaultValue !== undefined) {
      form.setFieldValue(field, defaultValue)
      setCurrentFormValues({ ...currentFormValues, [field]: defaultValue })
      message.info(`已填入【${field}】的系统默认值`)
    }
  }

  // 单字段快捷操作：恢复该项默认（清空为 undefined）
  const handleRestoreField = (field: keyof FormValues) => {
    form.setFieldValue(field, undefined)
    setCurrentFormValues({ ...currentFormValues, [field]: undefined })
    message.info(`已恢复【${field}】为系统默认值`)
  }

  // 头像上传
  const handleAvatarUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return
    setUploadingAvatar(true)
    try {
      const res = await uploadMedia(file)
      form.setFieldValue('avatar', res.url)
      setCurrentFormValues({ ...currentFormValues, avatar: res.url })
      message.success('头像上传成功')
    } catch (err) {
      message.error(err instanceof Error ? err.message : '头像上传失败')
    } finally {
      setUploadingAvatar(false)
      if (avatarInputRef.current) avatarInputRef.current.value = ''
    }
  }

  // 封面图上传
  const handleCoverUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return
    setUploadingCover(true)
    try {
      const res = await uploadMedia(file)
      form.setFieldValue('defaultCover', res.url)
      setCurrentFormValues({ ...currentFormValues, defaultCover: res.url })
      message.success('封面图上传成功')
    } catch (err) {
      message.error(err instanceof Error ? err.message : '封面图上传失败')
    } finally {
      setUploadingCover(false)
      if (coverInputRef.current) coverInputRef.current.value = ''
    }
  }

  // 从当前表单逆向同步生成格式化 JSON
  const handleSyncFormToJson = () => {
    const values = form.getFieldsValue()
    let base: Overrides = {}
    try {
      base = JSON.parse(jsonText || '{}')
    } catch {
      base = {}
    }
    const merged = JSON.parse(JSON.stringify(base))
    setOrDelete(merged, ['themePreset'], values.themePreset)
    setOrDelete(merged, ['name'], values.name?.trim())
    setOrDelete(merged, ['siteName'], values.siteName?.trim())
    setOrDelete(merged, ['title'], values.title?.trim())
    setOrDelete(merged, ['description'], values.description?.trim())
    setOrDelete(
      merged,
      ['keywords'],
      values.keywords
        ?.split(',')
        .map((item) => item.trim())
        .filter(Boolean)
    )
    setOrDelete(merged, ['url'], values.url?.trim())
    setOrDelete(merged, ['avatar'], values.avatar?.trim())
    setOrDelete(merged, ['defaultCover'], values.defaultCover?.trim())
    setOrDelete(merged, ['authorSubtitle'], values.authorSubtitle?.trim())
    setOrDelete(merged, ['authorIntro'], values.authorIntro?.trim())
    setOrDelete(merged, ['quote'], values.quote?.trim())
    setOrDelete(merged, ['footerMotto'], values.footerMotto?.trim())
    setOrDelete(merged, ['poweredBy'], values.poweredBy?.trim())
    setOrDelete(merged, ['socials', 'github'], values.github?.trim())
    setOrDelete(merged, ['socials', 'email'], values.email?.trim())

    setJsonText(JSON.stringify(merged, null, 2))
    message.success('已从当前表单生成完整覆盖项 JSON')
  }

  // 格式化当前的高级 JSON
  const handleFormatJson = () => {
    try {
      const parsed = JSON.parse(jsonText)
      setJsonText(JSON.stringify(parsed, null, 2))
      message.success('JSON 格式化成功')
    } catch {
      message.error('当前 JSON 语法错误，无法格式化')
    }
  }

  // 计算当前自定义覆盖项数量统计
  const stats = useMemo(() => {
    let customCount = 0
    const keys: (keyof FormValues)[] = [
      'name',
      'siteName',
      'url',
      'title',
      'description',
      'keywords',
      'avatar',
      'defaultCover',
      'authorSubtitle',
      'authorIntro',
      'quote',
      'footerMotto',
      'poweredBy',
      'github',
      'email',
    ]

    keys.forEach((k) => {
      const val = currentFormValues[k]
      if (val !== undefined && val !== null && String(val).trim() !== '') {
        customCount++
      }
    })

    if (currentFormValues.themePreset && currentFormValues.themePreset !== 'tech') {
      customCount++
    }

    return {
      total: keys.length + 1,
      customCount,
      defaultCount: keys.length + 1 - customCount,
    }
  }, [currentFormValues])

  // 解析并兜底图片路径（相对路径补全前台 URL，绝对路径直接使用）
  const resolveImageUrl = (path?: string) => {
    if (!path) return ''
    if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('data:')) {
      return path
    }
    const frontUrl = getFrontUrl().replace(/\/$/, '')
    return `${frontUrl}${path.startsWith('/') ? path : `/${path}`}`
  }

  // 渲染配置项标签头与当前生效状态辅助组件
  const renderFieldHeader = (
    label: string,
    field: keyof FormValues,
    tooltip?: string
  ) => {
    const rawVal = currentFormValues[field]
    const isCustom = rawVal !== undefined && rawVal !== null && String(rawVal).trim() !== ''

    return (
      <div className={styles.fieldHeader}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <span className={styles.fieldLabel}>{label}</span>
          {tooltip && (
            <Tooltip title={tooltip}>
              <InfoCircleOutlined style={{ color: '#94a3b8', fontSize: 13 }} />
            </Tooltip>
          )}
        </div>
        <div className={styles.fieldActions}>
          {isCustom ? (
            <>
              <span className={styles.statusCustom}>已自定义覆盖</span>
              <Button
                type="link"
                size="small"
                className={styles.actionBtn}
                icon={<UndoOutlined />}
                onClick={() => handleRestoreField(field)}
              >
                恢复默认
              </Button>
            </>
          ) : (
            <>
              <span className={styles.statusDefault}>系统默认</span>
              <Button
                type="link"
                size="small"
                className={styles.actionBtn}
                onClick={() => handleFillDefault(field)}
              >
                填入默认值
              </Button>
            </>
          )}
        </div>
      </div>
    )
  }

  // 渲染当前生效值提示
  const renderEffectiveTip = (field: keyof FormValues) => {
    const rawVal = currentFormValues[field]
    const isCustom = rawVal !== undefined && rawVal !== null && String(rawVal).trim() !== ''
    const defaultVal = DEFAULT_SITE_SETTINGS[field as keyof typeof DEFAULT_SITE_SETTINGS] || '（无）'
    const effectiveVal = isCustom ? String(rawVal) : defaultVal

    return (
      <div className={styles.effectiveTip}>
        当前实际生效：
        <span className={styles.effectiveValue}>
          {effectiveVal.length > 50 ? `${effectiveVal.slice(0, 50)}...` : effectiveVal}
        </span>
      </div>
    )
  }

  const selectedTheme = currentFormValues.themePreset || 'tech'
  const isAnimeTheme = selectedTheme === 'anime'

  return (
    <div className={styles.container}>
      {/* 隐藏的文件上传 input */}
      <input
        type="file"
        ref={avatarInputRef}
        style={{ display: 'none' }}
        accept="image/*"
        onChange={handleAvatarUpload}
      />
      <input
        type="file"
        ref={coverInputRef}
        style={{ display: 'none' }}
        accept="image/*"
        onChange={handleCoverUpload}
      />

      {/* 顶部科技感 Header Banner */}
      <div className={styles.headerBanner}>
        <div className={styles.headerLeft}>
          <div className={styles.headerTitleWrapper}>
            <SettingOutlined className={styles.headerIcon} />
            <Title level={4} className={styles.headerTitle}>
              站点全局设置
            </Title>
          </div>
          <Paragraph className={styles.headerSubtitle}>
            统一管理博客前台的主题预设、品牌名称、SEO 规范与博主名片。
            系统采用<strong>「双层配置驱动」</strong>：未填写的项将自动回退到系统代码默认规范中，保存后前台无需重启容器，最迟 60 秒内全局生效。
          </Paragraph>
        </div>

        <div className={styles.headerRight}>
          <div className={styles.statsBadge}>
            <CheckCircleFilled style={{ color: '#0284c7' }} />
            <span>
              已自定义 <strong>{stats.customCount}</strong> 项 · 默认 <strong>{stats.defaultCount}</strong> 项
            </span>
          </div>

          <Button
            icon={<ExportOutlined />}
            onClick={() => window.open(getFrontUrl(), '_blank')}
          >
            访问前台预览
          </Button>
        </div>
      </div>

      {/* 主布局：左侧表单配置区 + 右侧所见即所得实时效果预览卡片 */}
      <div className={styles.layoutRow}>
        <div className={styles.formCol}>
          <Card className={styles.settingsCard} loading={loading}>
            <Form
              form={form}
              layout="vertical"
              disabled={saving}
              onValuesChange={handleValuesChange}
            >
              <Tabs
                activeKey={activeTab}
                onChange={setActiveTab}
                type="line"
                items={[
                  // Tab 1: 主题风格与品牌形象
                  {
                    key: 'theme',
                    label: (
                      <span>
                        <FireOutlined /> 风格与形象
                      </span>
                    ),
                    children: (
                      <div className={styles.tabContent}>
                        <div className={styles.sectionHeader}>
                          <div className={styles.sectionTitle}>
                            <FireOutlined style={{ color: '#0284c7' }} /> 站点主题风格预设
                          </div>
                          <p className={styles.sectionDesc}>
                            切换前台展示端的视觉主题与交互模式。系统遵循高内聚解耦原则，无论是技术风还是动漫风，文章与分类等基础功能始终统一。
                          </p>
                        </div>

                        {/* 主题预设可视化选择卡片 */}
                        <Form.Item name="themePreset" noStyle>
                          <div className={styles.themeGrid}>
                            {/* 极简技术风卡片 */}
                            <div
                              className={`${styles.themeOptionCard} ${
                                !isAnimeTheme ? styles.themeOptionActive : ''
                              }`}
                              onClick={() => {
                                form.setFieldValue('themePreset', 'tech')
                                setCurrentFormValues({ ...currentFormValues, themePreset: 'tech' })
                              }}
                            >
                              <div className={styles.themeCardTop}>
                                <div className={styles.themeTitle}>极简技术风 (Tech)</div>
                                {!isAnimeTheme ? (
                                  <Tag color="blue" icon={<CheckOutlined />}>
                                    当前选用
                                  </Tag>
                                ) : (
                                  <Tag>现代推荐</Tag>
                                )}
                              </div>
                              <div className={styles.themeDesc}>
                                现代极简代码美学，纯粹清爽，专注技术干货输出。无动漫特色干扰，适合极客工程师个人品牌。
                              </div>
                              <div className={styles.themeTags}>
                                <Tag bordered={false} color="processing">极简响应式</Tag>
                                <Tag bordered={false} color="processing">代码块强化</Tag>
                                <Tag bordered={false} color="processing">极客优雅</Tag>
                              </div>
                            </div>

                            {/* 木叶火影风卡片 */}
                            <div
                              className={`${styles.themeOptionCard} ${
                                isAnimeTheme ? styles.themeOptionActiveAnime : ''
                              }`}
                              onClick={() => {
                                form.setFieldValue('themePreset', 'anime')
                                setCurrentFormValues({ ...currentFormValues, themePreset: 'anime' })
                              }}
                            >
                              <div className={styles.themeCardTop}>
                                <div className={styles.themeTitle}>木叶火影风 (Anime)</div>
                                {isAnimeTheme ? (
                                  <Tag color="orange" icon={<CheckOutlined />}>
                                    当前选用
                                  </Tag>
                                ) : (
                                  <Tag>特色彩蛋</Tag>
                                )}
                              </div>
                              <div className={styles.themeDesc}>
                                注入火影燃系文化特色，提供写轮眼名片动效、波风水门飞雷神苦无回顶、鸣人佐助昼夜彩蛋与忍道标语。
                              </div>
                              <div className={styles.themeTags}>
                                <Tag bordered={false} color="warning">写轮眼动效</Tag>
                                <Tag bordered={false} color="warning">苦无飞雷神</Tag>
                                <Tag bordered={false} color="warning">鸣佐双模</Tag>
                              </div>
                            </div>
                          </div>
                        </Form.Item>

                        <Divider style={{ margin: '20px 0' }} />

                        <div className={styles.sectionHeader}>
                          <div className={styles.sectionTitle}>
                            <PictureOutlined style={{ color: '#0284c7' }} /> 头像与封面媒体
                          </div>
                          <p className={styles.sectionDesc}>
                            支持输入网络绝对地址、本地系统相对地址，或直接点击上传本地图片至对象存储。
                          </p>
                        </div>

                        {/* 头像配置 */}
                        <div className={styles.mediaRow}>
                          <div className={styles.mediaThumbWrapper}>
                            <img
                              src={resolveImageUrl(currentFormValues.avatar || DEFAULT_SITE_SETTINGS.avatar)}
                              alt="头像预览"
                              className={styles.mediaThumbImg}
                              onError={(e) => {
                                ;(e.target as HTMLImageElement).src = '/favicon.svg'
                              }}
                            />
                          </div>
                          <div className={styles.mediaInfo}>
                            <Form.Item
                              label={renderFieldHeader('博主头像路径', 'avatar', '展示在导航栏名片、博主介绍及评论区的头像')}
                              name="avatar"
                              style={{ marginBottom: 4 }}
                            >
                              <Input
                                placeholder={`默认：${DEFAULT_SITE_SETTINGS.avatar}`}
                                allowClear
                              />
                            </Form.Item>
                            {renderEffectiveTip('avatar')}
                          </div>
                          <Space direction="vertical" size="small">
                            <Button
                              icon={uploadingAvatar ? <LoadingOutlined /> : <CloudUploadOutlined />}
                              loading={uploadingAvatar}
                              onClick={() => avatarInputRef.current?.click()}
                            >
                              上传头像
                            </Button>
                            <Button
                              type="text"
                              size="small"
                              onClick={() => {
                                form.setFieldValue('avatar', '/images/naruto/akatsuki-avatar.webp')
                                setCurrentFormValues({
                                  ...currentFormValues,
                                  avatar: '/images/naruto/akatsuki-avatar.webp',
                                })
                              }}
                            >
                              火影预设
                            </Button>
                          </Space>
                        </div>

                        {/* 默认封面图配置 */}
                        <div className={styles.mediaRow}>
                          <div className={`${styles.mediaThumbWrapper} ${styles.mediaThumbCover}`}>
                            <img
                              src={resolveImageUrl(currentFormValues.defaultCover || DEFAULT_SITE_SETTINGS.defaultCover)}
                              alt="封面预览"
                              className={styles.mediaThumbImg}
                              onError={(e) => {
                                ;(e.target as HTMLImageElement).src = '/favicon.svg'
                              }}
                            />
                          </div>
                          <div className={styles.mediaInfo}>
                            <Form.Item
                              label={renderFieldHeader('默认文章封面', 'defaultCover', '当文章未单独上传封面图时，列表页默认采用的备用背景图')}
                              name="defaultCover"
                              style={{ marginBottom: 4 }}
                            >
                              <Input
                                placeholder={`默认：${DEFAULT_SITE_SETTINGS.defaultCover}`}
                                allowClear
                              />
                            </Form.Item>
                            {renderEffectiveTip('defaultCover')}
                          </div>
                          <Space direction="vertical" size="small">
                            <Button
                              icon={uploadingCover ? <LoadingOutlined /> : <CloudUploadOutlined />}
                              loading={uploadingCover}
                              onClick={() => coverInputRef.current?.click()}
                            >
                              上传封面
                            </Button>
                            <Button
                              type="text"
                              size="small"
                              onClick={() => {
                                form.setFieldValue('defaultCover', '/images/naruto/cover-konoha.webp')
                                setCurrentFormValues({
                                  ...currentFormValues,
                                  defaultCover: '/images/naruto/cover-konoha.webp',
                                })
                              }}
                            >
                              木叶预设
                            </Button>
                          </Space>
                        </div>
                      </div>
                    ),
                  },

                  // Tab 2: 基础信息与 SEO
                  {
                    key: 'basic',
                    label: (
                      <span>
                        <GlobalOutlined /> 基础与 SEO
                      </span>
                    ),
                    children: (
                      <div className={styles.tabContent}>
                        <div className={styles.sectionHeader}>
                          <div className={styles.sectionTitle}>
                            <GlobalOutlined style={{ color: '#0284c7' }} /> 站点品牌与访问地址
                          </div>
                          <p className={styles.sectionDesc}>
                            定义博客对外的站点名称、品牌短代号以及前台生产域名。
                          </p>
                        </div>

                        <Row gutter={16}>
                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('站点名称', 'name', '用于作者署名、页头 Logo、版权展示')}
                              name="name"
                            >
                              <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.name}`} allowClear />
                            </Form.Item>
                            {renderEffectiveTip('name')}
                          </Col>

                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('品牌代号', 'siteName', '用于子页面标题模板：%s · 品牌代号')}
                              name="siteName"
                            >
                              <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.siteName}`} allowClear />
                            </Form.Item>
                            {renderEffectiveTip('siteName')}
                          </Col>
                        </Row>

                        <Form.Item
                          label={renderFieldHeader('站点生产访问地址 (URL)', 'url', '用于 SEO、RSS 订阅源以及 Sitemap 生成，需包含完整协议头 http:// 或 https://')}
                          name="url"
                          rules={[
                            {
                              validator: (_, value?: string) =>
                                !value || /^https?:\/\/.+/.test(value)
                                  ? Promise.resolve()
                                  : Promise.reject(new Error('请填写以 http:// 或 https:// 开头的完整有效网址')),
                            },
                          ]}
                        >
                          <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.url}`} allowClear />
                        </Form.Item>
                        {renderEffectiveTip('url')}

                        <Divider style={{ margin: '20px 0' }} />

                        <div className={styles.sectionHeader}>
                          <div className={styles.sectionTitle}>
                            <CompassOutlined style={{ color: '#0284c7' }} /> 搜索引擎与元信息 (SEO)
                          </div>
                          <p className={styles.sectionDesc}>
                            优化各大搜索引擎收录的首页标题、描述与全局关键词。
                          </p>
                        </div>

                        <Row gutter={16}>
                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('默认首页标题', 'title', '浏览器 Tab 页签与搜索引擎收录的首选主标题')}
                              name="title"
                            >
                              <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.title}`} allowClear />
                            </Form.Item>
                            {renderEffectiveTip('title')}
                          </Col>

                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('SEO 关键词', 'keywords', '英文逗号分隔的全局技术关键字')}
                              name="keywords"
                            >
                              <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.keywords}`} allowClear />
                            </Form.Item>
                            {renderEffectiveTip('keywords')}
                          </Col>
                        </Row>

                        <Form.Item
                          label={renderFieldHeader('站点描述 (Description)', 'description', '一句话向读者与搜索引擎介绍本站核心价值')}
                          name="description"
                        >
                          <Input.TextArea
                            rows={3}
                            placeholder={`默认：${DEFAULT_SITE_SETTINGS.description}`}
                            allowClear
                          />
                        </Form.Item>
                        {renderEffectiveTip('description')}
                      </div>
                    ),
                  },

                  // Tab 3: 博主文案与名片
                  {
                    key: 'author',
                    label: (
                      <span>
                        <UserOutlined /> 博主文案与名片
                      </span>
                    ),
                    children: (
                      <div className={styles.tabContent}>
                        <div className={styles.sectionHeader}>
                          <div className={styles.sectionTitle}>
                            <UserOutlined style={{ color: '#0284c7' }} /> 博主身份与介绍
                          </div>
                          <p className={styles.sectionDesc}>
                            个性化定制关于我页面、侧边栏名片中的作者介绍与职业身份定位。
                          </p>
                        </div>

                        <Row gutter={16}>
                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('博主副标题 / 职业身份', 'authorSubtitle')}
                              name="authorSubtitle"
                            >
                              <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.authorSubtitle}`} allowClear />
                            </Form.Item>
                            {renderEffectiveTip('authorSubtitle')}
                          </Col>

                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('首页首屏标语 (Quote)', 'quote', '首屏 Hero 醒目展示位口号或座右铭')}
                              name="quote"
                            >
                              <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.quote}`} allowClear />
                            </Form.Item>
                            {renderEffectiveTip('quote')}
                          </Col>
                        </Row>

                        <Form.Item
                          label={renderFieldHeader('博主个人简介', 'authorIntro')}
                          name="authorIntro"
                        >
                          <Input.TextArea
                            rows={2}
                            placeholder={`默认：${DEFAULT_SITE_SETTINGS.authorIntro}`}
                            allowClear
                          />
                        </Form.Item>
                        {renderEffectiveTip('authorIntro')}

                        <Divider style={{ margin: '20px 0' }} />

                        <div className={styles.sectionHeader}>
                          <div className={styles.sectionTitle}>
                            <ThunderboltOutlined style={{ color: '#0284c7' }} /> 页脚格言与社交连接
                          </div>
                          <p className={styles.sectionDesc}>
                            展示在博客每一页底部的名言格言、版权归属以及社交主页链接。
                          </p>
                        </div>

                        <Row gutter={16}>
                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('页脚格言', 'footerMotto')}
                              name="footerMotto"
                            >
                              <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.footerMotto}`} allowClear />
                            </Form.Item>
                            {renderEffectiveTip('footerMotto')}
                          </Col>

                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('页脚版权声明', 'poweredBy')}
                              name="poweredBy"
                            >
                              <Input placeholder={`默认：${DEFAULT_SITE_SETTINGS.poweredBy}`} allowClear />
                            </Form.Item>
                            {renderEffectiveTip('poweredBy')}
                          </Col>
                        </Row>

                        <Row gutter={16}>
                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('GitHub 主页链接', 'github')}
                              name="github"
                            >
                              <Input
                                prefix={<GithubOutlined style={{ color: '#64748b' }} />}
                                placeholder={`默认：${DEFAULT_SITE_SETTINGS.github}`}
                                allowClear
                              />
                            </Form.Item>
                            {renderEffectiveTip('github')}
                          </Col>

                          <Col xs={24} md={12}>
                            <Form.Item
                              label={renderFieldHeader('联系邮箱', 'email')}
                              name="email"
                            >
                              <Input
                                prefix={<MailOutlined style={{ color: '#64748b' }} />}
                                placeholder="如：your-email@example.com"
                                allowClear
                              />
                            </Form.Item>
                            {renderEffectiveTip('email')}
                          </Col>
                        </Row>
                      </div>
                    ),
                  },

                  // Tab 4: 开发者高级 JSON
                  {
                    key: 'advanced',
                    label: (
                      <span>
                        <CodeOutlined /> 高级配置 (JSON)
                      </span>
                    ),
                    children: (
                      <div className={styles.tabContent}>
                        <div className={styles.sectionHeader}>
                          <div className={styles.sectionTitle}>
                            <CodeOutlined style={{ color: '#0284c7' }} /> 底层配置覆盖对象 (JSON)
                          </div>
                          <p className={styles.sectionDesc}>
                            专供极客博主定制多层级高级字段（例如关于页技能矩阵、404彩蛋、首页按钮文案等）。
                            空对象 <Text code>{'{}'}</Text> 表示所有字段均直接继承系统代码默认配置。
                          </p>
                        </div>

                        <Alert
                          type="info"
                          showIcon
                          style={{ marginBottom: 16 }}
                          message="高级说明"
                          description="此处直接对应数据库 site_settings 中的实际 JSON 记录。上方可视化表单与此文本框双向关联。保存时两处配置将自动合并并生效。"
                        />

                        <Space style={{ marginBottom: 12 }}>
                          <Button size="small" onClick={handleFormatJson}>
                            格式化 JSON
                          </Button>
                          <Button size="small" onClick={handleSyncFormToJson}>
                            从当前表单同步到 JSON
                          </Button>
                          <Button
                            size="small"
                            danger
                            onClick={() => {
                              setJsonText('{}')
                              message.info('已将 JSON 高级配置重置为 {}')
                            }}
                          >
                            清空为 {'{}'}
                          </Button>
                        </Space>

                        <Input.TextArea
                          value={jsonText}
                          onChange={(e) => {
                            const next = e.target.value
                            setJsonText(next)
                            try {
                              const parsed = JSON.parse(next)
                              if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
                                form.setFieldsValue(toFormValues(parsed as Overrides))
                                setCurrentFormValues(toFormValues(parsed as Overrides))
                              }
                            } catch {
                              // 输入过程中暂时非法不打断
                            }
                          }}
                          autoSize={{ minRows: 12, maxRows: 24 }}
                          style={{
                            fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Consolas, monospace',
                            fontSize: 12,
                            background: '#0f172a',
                            color: '#e2e8f0',
                            borderRadius: 8,
                          }}
                          spellCheck={false}
                        />
                      </div>
                    ),
                  },
                ]}
              />
            </Form>
          </Card>
        </div>

        {/* 右侧：所见即所得前台实时效果预览卡片 (Live Mini-Card Preview) */}
        <div className={styles.previewCol}>
          <div className={styles.previewWidget}>
            <div className={styles.previewHeader}>
              <div className={styles.previewTitle}>
                <CompassOutlined /> 前台实时效果预览
              </div>
              <div className={styles.previewDotGroup}>
                <div className={`${styles.previewDot} ${styles.previewDotRed}`} />
                <div className={`${styles.previewDot} ${styles.previewDotYellow}`} />
                <div className={`${styles.previewDot} ${styles.previewDotGreen}`} />
              </div>
            </div>

            <div className={styles.previewBody}>
              {/* 首屏 Banner 模拟 */}
              <div
                className={`${styles.miniHeroBanner} ${
                  isAnimeTheme ? styles.miniHeroBannerAnime : ''
                }`}
              >
                <div className={styles.miniHeroThemeBadge}>
                  {isAnimeTheme ? '⛩️ 木叶火影风 (Anime)' : '⚡ 极简技术风 (Tech)'}
                </div>
                <div className={styles.miniSiteName}>
                  {currentFormValues.name || DEFAULT_SITE_SETTINGS.name}
                  <span
                    className={`${styles.miniBrandTag} ${
                      isAnimeTheme ? styles.miniBrandTagAnime : ''
                    }`}
                  >
                    {currentFormValues.siteName || DEFAULT_SITE_SETTINGS.siteName}
                  </span>
                </div>
                <div className={styles.miniQuote}>
                  “{currentFormValues.quote || DEFAULT_SITE_SETTINGS.quote}”
                </div>
              </div>

              {/* 博主名片组件模拟 */}
              <div className={styles.miniAuthorCard}>
                <img
                  src={resolveImageUrl(currentFormValues.avatar || DEFAULT_SITE_SETTINGS.avatar)}
                  alt="预览头像"
                  className={styles.miniAuthorAvatar}
                  onError={(e) => {
                    ;(e.target as HTMLImageElement).src = '/favicon.svg'
                  }}
                />
                <div className={styles.miniAuthorInfo}>
                  <div className={styles.miniAuthorName}>
                    {currentFormValues.name || DEFAULT_SITE_SETTINGS.name}
                  </div>
                  <div className={styles.miniAuthorSubtitle}>
                    {currentFormValues.authorSubtitle || DEFAULT_SITE_SETTINGS.authorSubtitle}
                  </div>
                  <div className={styles.miniAuthorIntro}>
                    {currentFormValues.authorIntro || DEFAULT_SITE_SETTINGS.authorIntro}
                  </div>
                </div>
              </div>

              {/* 模拟页脚组件 */}
              <div className={styles.miniFooter}>
                <div className={styles.miniFooterMotto}>
                  {currentFormValues.footerMotto || DEFAULT_SITE_SETTINGS.footerMotto}
                </div>
                <div className={styles.miniFooterPowered}>
                  {currentFormValues.poweredBy || DEFAULT_SITE_SETTINGS.poweredBy}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* 底部吸底操作浮动栏 (Sticky Action Bar) */}
      <div className={styles.stickyBottomBar}>
        <div className={styles.stickyLeftText}>
          <InfoCircleOutlined style={{ color: '#0284c7' }} />
          <span>修改后点击保存生效，支持快捷键 <strong>Ctrl + S</strong></span>
        </div>

        <Space size="middle">
          <PermissionGuard code={PERMISSIONS.SETTINGS_MANAGE}>
            <Popconfirm
              title="确定恢复全部代码默认设置吗？"
              description="该操作将清空所有自定义覆盖配置，重置为系统出厂默认值。"
              okText="确定恢复"
              cancelText="取消"
              onConfirm={handleReset}
            >
              <Button icon={<UndoOutlined />} loading={saving} disabled={loading}>
                恢复全部默认
              </Button>
            </Popconfirm>

            <Button
              type="primary"
              icon={<SaveOutlined />}
              loading={saving}
              disabled={loading}
              onClick={handleSave}
              style={{
                background: 'linear-gradient(135deg, #0284c7 0%, #0369a1 100%)',
                borderColor: '#0284c7',
                padding: '0 24px',
                height: 38,
                fontWeight: 600,
                boxShadow: '0 2px 8px rgba(2, 132, 199, 0.3)',
              }}
            >
              保存站点设置
            </Button>
          </PermissionGuard>

          <Button icon={<ReloadOutlined />} onClick={loadSettings} disabled={saving || loading}>
            重新加载
          </Button>
        </Space>
      </div>
    </div>
  )
}
