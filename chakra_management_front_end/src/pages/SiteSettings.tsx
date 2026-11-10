import { useEffect, useState } from 'react'
import {
  Alert,
  App,
  Button,
  Card,
  Col,
  Form,
  Input,
  Radio,
  Row,
  Space,
  Typography,
} from 'antd'
import { ReloadOutlined, SaveOutlined, SettingOutlined } from '@ant-design/icons'
import * as settingsApi from '@/api/settings'
import { setFrontUrl } from '@/config/brand'
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'

const { Title, Paragraph, Text } = Typography

type Overrides = Record<string, unknown>

interface FormValues {
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

/**
 * 将后台保存的覆盖项回填到表单
 */
function toFormValues(overrides: Overrides): FormValues {
  const socials = (overrides.socials ?? {}) as Overrides
  const keywords = overrides.keywords

  return {
    themePreset: (overrides.themePreset === 'anime' ? 'anime' : 'tech') as 'tech' | 'anime',
    name: asText(overrides.name),
    siteName: asText(overrides.siteName),
    title: asText(overrides.title),
    description: asText(overrides.description),
    keywords: Array.isArray(keywords) ? keywords.join(',') : asText(keywords),
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

function asText(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim() !== '' ? value : undefined
}

/**
 * 在覆盖对象上按路径写入或删除字段（空值表示回落代码默认值）
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

  const loadSettings = async () => {
    setLoading(true)
    try {
      const overrides = (await settingsApi.getSiteSettings()) ?? {}
      setJsonText(JSON.stringify(overrides, null, 2))
      form.resetFields()
      form.setFieldsValue(toFormValues(overrides))
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

  const handleSave = async () => {
    let values: FormValues
    try {
      values = await form.validateFields()
    } catch {
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
      message.error('高级 JSON 格式不正确，请检查后再保存')
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
      message.success('站点设置已保存，前台最迟 60 秒内生效')
    } catch {
      message.error('保存站点设置失败')
    } finally {
      setSaving(false)
    }
  }

  const handleReset = async () => {
    setJsonText('{}')
    form.resetFields()
    setFrontUrl(undefined)
    setSaving(true)
    try {
      await settingsApi.updateSiteSettings({})
      message.success('已恢复代码默认配置')
    } catch {
      message.error('恢复默认配置失败')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div>
      <Space align="center" style={{ marginBottom: 4 }}>
        <SettingOutlined style={{ fontSize: 20, color: '#0284c7' }} />
        <Title level={4} style={{ margin: 0 }}>
          站点设置
        </Title>
      </Space>
      <Paragraph type="secondary" style={{ marginBottom: 16 }}>
        仅保存需要覆盖的字段，留空即使用代码默认值（<Text code>chakra_front_end/src/config/site.ts</Text>
        ）。保存后前台最迟 60 秒生效，无需重新打包镜像。
      </Paragraph>

      <Card loading={loading} style={{ marginBottom: 16 }}>
        <Form form={form} layout="vertical" disabled={saving}>
          <Row gutter={16}>
            <Col xs={24}>
              <Form.Item
                label="站点主题风格预设"
                name="themePreset"
                extra="前台视觉与彩蛋模式：技术风（Tech）提供极简现代通用设计；火影风（Anime）提供写轮眼名片、苦无回顶与动漫特色彩蛋。"
              >
                <Radio.Group buttonStyle="solid">
                  <Radio.Button value="tech">极简技术风 (Tech - 现代通用推荐)</Radio.Button>
                  <Radio.Button value="anime">木叶火影风 (Anime - 特色彩蛋)</Radio.Button>
                </Radio.Group>
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col xs={24} md={8}>
              <Form.Item label="站点名称" name="name" extra="用于作者署名、版权等展示位">
                <Input placeholder="如：张三的技术博客" allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item label="品牌名" name="siteName" extra="子页面标题模板：%s · 品牌名">
                <Input placeholder="如：zhangsan.dev" allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item
                label="站点地址"
                name="url"
                extra="用于 SEO、RSS 与 Sitemap，需带协议"
                rules={[
                  {
                    validator: (_, value?: string) =>
                      !value || /^https?:\/\/.+/.test(value)
                        ? Promise.resolve()
                        : Promise.reject(new Error('请填写以 http:// 或 https:// 开头的完整地址')),
                  },
                ]}
              >
                <Input placeholder="https://blog.example.com" allowClear />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item label="默认标题" name="title">
                <Input placeholder="Chakra - 个人技术博客" allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item label="关键词" name="keywords" extra="英文逗号分隔，用于 SEO">
                <Input placeholder=".NET,C#,微服务" allowClear />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item label="站点描述" name="description">
            <Input.TextArea rows={2} placeholder="一句话介绍你的站点" allowClear />
          </Form.Item>

          <Row gutter={16}>
            <Col xs={24} md={8}>
              <Form.Item label="头像路径" name="avatar">
                <Input placeholder="/images/avatar.webp" allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item label="默认封面路径" name="defaultCover">
                <Input placeholder="/images/cover.webp" allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item label="作者副标题" name="authorSubtitle">
                <Input placeholder="如：后端工程师" allowClear />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item label="作者简介" name="authorIntro">
            <Input.TextArea rows={2} allowClear />
          </Form.Item>

          <Row gutter={16}>
            <Col xs={24} md={8}>
              <Form.Item label="首屏标语" name="quote">
                <Input allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item label="页脚格言" name="footerMotto">
                <Input allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item label="页脚版权" name="poweredBy">
                <Input allowClear />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col xs={24} md={12}>
              <Form.Item label="GitHub 链接" name="github">
                <Input placeholder="https://github.com/yourname" allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item label="联系邮箱" name="email">
                <Input placeholder="you@example.com" allowClear />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Card>

      <Card
        title="高级配置（JSON）"
        style={{ marginBottom: 16 }}
        extra={<Text type="secondary">可覆盖首页文案、关于页技术栈、角色设定等任意层级字段</Text>}
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="仅需填写要覆盖的字段，保存时会与上方表单合并；清空为 {} 表示全部使用代码默认值。"
        />
        <Input.TextArea
          value={jsonText}
          onChange={(event) => {
            const next = event.target.value
            setJsonText(next)
            // JSON 可直接编辑：解析成功后同步回表单，避免保存时表单旧值覆盖 JSON 中的修改
            try {
              const parsed = JSON.parse(next)
              if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
                form.resetFields()
                form.setFieldsValue(toFormValues(parsed as Overrides))
              }
            } catch {
              // 输入过程中的非法 JSON 不打断编辑，保存时会统一校验
            }
          }}
          autoSize={{ minRows: 10, maxRows: 24 }}
          style={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Consolas, monospace', fontSize: 12 }}
          spellCheck={false}
        />
      </Card>

      <Space>
        <PermissionGuard code={PERMISSIONS.SETTINGS_MANAGE}>
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={saving}
            disabled={loading}
            onClick={handleSave}
          >
            保存设置
          </Button>
          <Button icon={<ReloadOutlined />} loading={saving} disabled={loading} onClick={handleReset}>
            恢复默认
          </Button>
        </PermissionGuard>
        <Button onClick={loadSettings} disabled={saving || loading}>
          重新加载
        </Button>
      </Space>
    </div>
  )
}
