import { useState, useEffect, useCallback } from 'react'
import {
  Tabs,
  Card,
  Table,
  Button,
  Input,
  Select,
  DatePicker,
  Space,
  Tag,
  Typography,
  Row,
  Col,
  App,
  Tooltip,
  Badge,
  Modal,
  Descriptions,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  SearchOutlined,
  ReloadOutlined,
  SafetyCertificateOutlined,
  BugOutlined,
  UserOutlined,
  EyeOutlined,
  CopyOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons'
import * as logsApi from '@/api/logs'
import type { AuditLogDto, ExceptionLogDto } from '@/api/logs'
import { AUDIT_MODULE_COLORS, AUDIT_MODULE_OPTIONS } from '@/constants/logs'
import { PERMISSIONS } from '@/constants/permissions'
import { usePermission } from '@/hooks/usePermission'
import dayjs from 'dayjs'
import styles from './LogList.module.css'

const { Title, Paragraph, Text } = Typography
const { RangePicker } = DatePicker

// 模块色彩映射（使用统一常量配置）
const MODULE_COLORS = AUDIT_MODULE_COLORS

export default function LogList() {
  const { message } = App.useApp()
  const { hasPermission, isAdmin } = usePermission()
  const canViewAudit = isAdmin || hasPermission(PERMISSIONS.LOG_AUDIT_VIEW)
  const canViewException = isAdmin || hasPermission(PERMISSIONS.LOG_EXCEPTION_VIEW)

  const [activeTab, setActiveTab] = useState<'audit' | 'exceptions'>(() => {
    if (!canViewAudit && canViewException) return 'exceptions'
    return 'audit'
  })

  // === 审计日志状态 ===
  const [auditLoading, setAuditLoading] = useState(false)
  const [auditData, setAuditData] = useState<AuditLogDto[]>([])
  const [auditTotal, setAuditTotal] = useState(0)
  const [auditPage, setAuditPage] = useState(1)
  const [auditPageSize, setAuditPageSize] = useState(20)
  const [auditModule, setAuditModule] = useState<string | undefined>(undefined)
  const [auditUsername, setAuditUsername] = useState('')
  const [auditTraceId, setAuditTraceId] = useState('')
  const [auditStatus, setAuditStatus] = useState<boolean | undefined>(undefined)
  const [auditDateRange, setAuditDateRange] = useState<[dayjs.Dayjs | null, dayjs.Dayjs | null] | null>(null)

  // === 异常日志状态 ===
  const [exLoading, setExLoading] = useState(false)
  const [exData, setExData] = useState<ExceptionLogDto[]>([])
  const [exTotal, setExTotal] = useState(0)
  const [exPage, setExPage] = useState(1)
  const [exPageSize, setExPageSize] = useState(20)
  const [exKeyword, setExKeyword] = useState('')
  const [exTraceId, setExTraceId] = useState('')
  const [exDateRange, setExDateRange] = useState<[dayjs.Dayjs | null, dayjs.Dayjs | null] | null>(null)

  // === 弹窗详情状态 ===
  const [selectedException, setSelectedException] = useState<ExceptionLogDto | null>(null)
  const [isExDetailOpen, setIsExDetailOpen] = useState(false)
  const [selectedAudit, setSelectedAudit] = useState<AuditLogDto | null>(null)
  const [isAuditDetailOpen, setIsAuditDetailOpen] = useState(false)

  // 加载审计日志
  const fetchAuditLogs = useCallback(async () => {
    setAuditLoading(true)
    try {
      const res = await logsApi.getAuditLogs({
        module: auditModule,
        username: auditUsername.trim() || undefined,
        traceId: auditTraceId.trim() || undefined,
        isSuccess: auditStatus,
        startDate: auditDateRange?.[0] ? auditDateRange[0].startOf('day').toISOString() : undefined,
        endDate: auditDateRange?.[1] ? auditDateRange[1].endOf('day').toISOString() : undefined,
        pageIndex: auditPage,
        pageSize: auditPageSize,
      })
      setAuditData(res.items || [])
      setAuditTotal(res.total || 0)
    } catch (err) {
      message.error(err instanceof Error ? err.message : '加载操作审计日志失败')
    } finally {
      setAuditLoading(false)
    }
  }, [auditModule, auditUsername, auditTraceId, auditStatus, auditDateRange, auditPage, auditPageSize, message])

  // 加载异常日志
  const fetchExceptionLogs = useCallback(async () => {
    setExLoading(true)
    try {
      const res = await logsApi.getExceptionLogs({
        keyword: exKeyword.trim() || undefined,
        traceId: exTraceId.trim() || undefined,
        startDate: exDateRange?.[0] ? exDateRange[0].startOf('day').toISOString() : undefined,
        endDate: exDateRange?.[1] ? exDateRange[1].endOf('day').toISOString() : undefined,
        pageIndex: exPage,
        pageSize: exPageSize,
      })
      setExData(res.items || [])
      setExTotal(res.total || 0)
    } catch (err) {
      message.error(err instanceof Error ? err.message : '加载系统异常日志失败')
    } finally {
      setExLoading(false)
    }
  }, [exKeyword, exTraceId, exDateRange, exPage, exPageSize, message])

  useEffect(() => {
    if (activeTab === 'audit' && canViewAudit) {
      fetchAuditLogs()
    } else if (activeTab === 'exceptions' && canViewException) {
      fetchExceptionLogs()
    }
  }, [activeTab, canViewAudit, canViewException, fetchAuditLogs, fetchExceptionLogs])

  // 重置审计筛选
  const handleResetAudit = () => {
    setAuditModule(undefined)
    setAuditUsername('')
    setAuditTraceId('')
    setAuditStatus(undefined)
    setAuditDateRange(null)
    setAuditPage(1)
  }

  // 重置异常筛选
  const handleResetEx = () => {
    setExKeyword('')
    setExTraceId('')
    setExDateRange(null)
    setExPage(1)
  }

  // 文本截断辅助函数（截取前 max 个字）
  const truncateText = (text?: string | null, max = 20) => {
    if (!text) return '—'
    return text.length > max ? `${text.slice(0, max)}...` : text
  }

  // 复制辅助函数
  const fallbackCopy = (content: string, successMsg: string) => {
    const el = document.createElement('textarea')
    el.value = content
    el.setAttribute('readonly', '')
    el.style.position = 'absolute'
    el.style.left = '-9999px'
    document.body.appendChild(el)
    el.select()
    try {
      document.execCommand('copy')
      message.success(successMsg)
    } catch {
      message.error('复制失败，请手动选择复制')
    } finally {
      document.body.removeChild(el)
    }
  }

  const handleCopy = (content?: string | null, successMsg = '已复制到剪贴板') => {
    if (!content) return
    if (navigator.clipboard && window.isSecureContext) {
      navigator.clipboard.writeText(content).then(
        () => message.success(successMsg),
        () => fallbackCopy(content, successMsg)
      )
    } else {
      fallbackCopy(content, successMsg)
    }
  }

  const handleShowExceptionDetail = (record: ExceptionLogDto) => {
    setSelectedException(record)
    setIsExDetailOpen(true)
  }

  const handleShowAuditDetail = (record: AuditLogDto) => {
    setSelectedAudit(record)
    setIsAuditDetailOpen(true)
  }

  // 审计日志表格列定义
  const auditColumns: ColumnsType<AuditLogDto> = [
    {
      title: '发生时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date: string) => (date ? dayjs(date).format('YYYY-MM-DD HH:mm:ss') : '—'),
    },
    {
      title: '操作人',
      dataIndex: 'username',
      key: 'username',
      width: 130,
      render: (name: string) => (
        <Space size={6}>
          <UserOutlined style={{ color: '#64748b' }} />
          <Text strong style={{ color: '#0f172a' }}>
            {name || '匿名访客'}
          </Text>
        </Space>
      ),
    },
    {
      title: '业务模块',
      dataIndex: 'module',
      key: 'module',
      width: 110,
      render: (mod: string) => (
        <Tag color={MODULE_COLORS[mod] || 'default'} style={{ borderRadius: 6, fontWeight: 500 }}>
          {mod}
        </Tag>
      ),
    },
    {
      title: '操作行为',
      dataIndex: 'action',
      key: 'action',
      width: 140,
      render: (act: string) => (
        <Text style={{ color: '#0f172a', fontWeight: 600 }}>{act}</Text>
      ),
    },
    {
      title: '请求方式与路径',
      key: 'request',
      width: 280,
      render: (_, record) => (
        <div className={styles.requestCell}>
          <Tag
            color={
              record.method === 'POST'
                ? 'blue'
                : record.method === 'PUT'
                ? 'orange'
                : record.method === 'DELETE'
                ? 'red'
                : record.method === 'GET'
                ? 'cyan'
                : 'default'
            }
            style={{ fontWeight: 600, fontSize: 11, marginRight: 6, flexShrink: 0 }}
          >
            {record.method}
          </Tag>
          <Tooltip title={record.path}>
            <Text code className={styles.pathCode} style={{ maxWidth: 195 }}>
              {record.path}
            </Text>
          </Tooltip>
        </div>
      ),
    },
    {
      title: '客户端 IP',
      dataIndex: 'clientIp',
      key: 'clientIp',
      width: 130,
      render: (ip: string) => <Text style={{ color: '#64748b', fontSize: 13 }}>{ip || '—'}</Text>,
    },
    {
      title: '耗时',
      dataIndex: 'durationMs',
      key: 'durationMs',
      width: 95,
      render: (ms: number) => {
        const color = ms < 100 ? 'success' : ms < 500 ? 'warning' : 'error'
        return <Tag color={color}>{ms} ms</Tag>
      },
    },
    {
      title: 'Trace ID',
      dataIndex: 'traceId',
      key: 'traceId',
      width: 140,
      render: (tid: string) =>
        tid ? (
          <Text code copyable style={{ fontSize: 11, color: '#0284c7' }}>
            {tid.length > 14 ? `${tid.slice(0, 14)}…` : tid}
          </Text>
        ) : (
          <Text type="secondary" style={{ fontSize: 12 }}>—</Text>
        ),
    },
    {
      title: '执行结果',
      dataIndex: 'isSuccess',
      key: 'isSuccess',
      width: 100,
      render: (success: boolean, record) =>
        success ? (
          <Badge status="success" text={<span style={{ color: '#10b981', fontWeight: 600 }}>成功</span>} />
        ) : (
          <Tooltip title={record.errorMessage || '操作失败，点击查看详情'}>
            <span style={{ cursor: 'pointer' }} onClick={() => handleShowAuditDetail(record)}>
              <Badge status="error" text={<span style={{ color: '#ef4444', fontWeight: 600 }}>失败</span>} />
            </span>
          </Tooltip>
        ),
    },
    {
      title: '操作',
      key: 'actions',
      width: 80,
      fixed: 'right',
      render: (_, record) => (
        <Button
          type="link"
          size="small"
          icon={<EyeOutlined />}
          onClick={() => handleShowAuditDetail(record)}
        >
          详情
        </Button>
      ),
    },
  ]

  // 异常日志表格列定义
  const exColumns: ColumnsType<ExceptionLogDto> = [
    {
      title: '发生时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date: string) => (date ? dayjs(date).format('YYYY-MM-DD HH:mm:ss') : '—'),
    },
    {
      title: '异常类型',
      dataIndex: 'exceptionType',
      key: 'exceptionType',
      width: 180,
      render: (type: string) => {
        const shortName = type?.split('.').pop() || type || '—'
        return (
          <Tooltip title={type}>
            <Tag
              color="red"
              style={{
                borderRadius: 6,
                fontWeight: 600,
                maxWidth: 155,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                display: 'inline-block',
                verticalAlign: 'middle',
              }}
            >
              {shortName}
            </Tag>
          </Tooltip>
        )
      },
    },
    {
      title: '错误摘要',
      dataIndex: 'message',
      key: 'message',
      width: 220,
      render: (msg: string, record) => {
        const displayText = truncateText(msg, 20)
        return (
          <Tooltip title={msg ? `点击弹出详情：${msg}` : '点击弹出详情'}>
            <span
              className={styles.messageLink}
              onClick={() => handleShowExceptionDetail(record)}
            >
              <Text strong style={{ color: '#ef4444', cursor: 'pointer' }}>
                {displayText}
              </Text>
              <InfoCircleOutlined style={{ fontSize: 12, color: '#ef4444', opacity: 0.75, flexShrink: 0 }} />
            </span>
          </Tooltip>
        )
      },
    },
    {
      title: '请求方式与路径',
      key: 'request',
      width: 280,
      render: (_, record) => (
        <div className={styles.requestCell}>
          <Tag
            color={
              record.method === 'POST'
                ? 'blue'
                : record.method === 'PUT'
                ? 'orange'
                : record.method === 'DELETE'
                ? 'red'
                : record.method === 'GET'
                ? 'cyan'
                : 'magenta'
            }
            style={{ fontWeight: 600, fontSize: 11, marginRight: 6, flexShrink: 0 }}
          >
            {record.method}
          </Tag>
          <Tooltip title={record.path}>
            <Text code className={styles.pathCode} style={{ maxWidth: 195 }}>
              {record.path}
            </Text>
          </Tooltip>
        </div>
      ),
    },
    {
      title: '客户端 IP',
      dataIndex: 'clientIp',
      key: 'clientIp',
      width: 130,
      render: (ip: string) => ip || '—',
    },
    {
      title: 'Trace ID',
      dataIndex: 'traceId',
      key: 'traceId',
      width: 140,
      render: (tid: string) =>
        tid ? (
          <Text code copyable style={{ fontSize: 11, color: '#ef4444' }}>
            {tid.length > 14 ? `${tid.slice(0, 14)}…` : tid}
          </Text>
        ) : (
          <Text type="secondary" style={{ fontSize: 12 }}>—</Text>
        ),
    },
    {
      title: '操作用户',
      dataIndex: 'username',
      key: 'username',
      width: 110,
      render: (name: string) => name || '匿名访客',
    },
    {
      title: '操作',
      key: 'actions',
      width: 80,
      fixed: 'right',
      render: (_, record) => (
        <Button
          type="link"
          size="small"
          icon={<EyeOutlined />}
          onClick={() => handleShowExceptionDetail(record)}
        >
          详情
        </Button>
      ),
    },
  ]

  return (
    <div className={styles.container}>
      {/* 页面头部 */}
      <div className={styles.header}>
        <div>
          <Title level={3} className={styles.title}>
            系统运行与审计日志
          </Title>
          <Paragraph className={styles.subtitle}>
            实时记录业务操作审计与系统异常轨迹，基于无锁内存通道异步批量持久化
          </Paragraph>
        </div>
      </div>

      {/* 标签切换 */}
      <Tabs
        activeKey={activeTab}
        onChange={(k) => setActiveTab(k as 'audit' | 'exceptions')}
        type="card"
        size="large"
        items={[
          ...(canViewAudit ? [{
            key: 'audit',
            label: (
              <Space>
                <SafetyCertificateOutlined />
                <span>业务操作审计日志</span>
              </Space>
            ),
            children: (
              <>
                {/* 审计日志筛选条 */}
                <Card className={styles.filterCard} styles={{ body: { padding: '16px 20px' } }}>
                  <Row gutter={[16, 16]} align="middle">
                    <Col xs={24} sm={12} md={5}>
                      <Select
                        placeholder="所属功能模块"
                        allowClear
                        value={auditModule}
                        onChange={setAuditModule}
                        style={{ width: '100%' }}
                        options={AUDIT_MODULE_OPTIONS}
                      />
                    </Col>

                    <Col xs={24} sm={12} md={5}>
                      <Input
                        placeholder="操作人用户名..."
                        value={auditUsername}
                        onChange={(e) => setAuditUsername(e.target.value)}
                        allowClear
                      />
                    </Col>

                    <Col xs={24} sm={12} md={4}>
                      <Input
                        placeholder="Trace ID 追踪码..."
                        value={auditTraceId}
                        onChange={(e) => setAuditTraceId(e.target.value)}
                        onPressEnter={() => {
                          setAuditPage(1)
                          fetchAuditLogs()
                        }}
                        allowClear
                      />
                    </Col>

                    <Col xs={24} sm={12} md={3}>
                      <Select
                        placeholder="执行状态"
                        allowClear
                        value={auditStatus}
                        onChange={setAuditStatus}
                        style={{ width: '100%' }}
                        options={[
                          { label: '全部状态', value: undefined as unknown as boolean },
                          { label: '执行成功', value: true },
                          { label: '执行失败', value: false },
                        ]}
                      />
                    </Col>

                    <Col xs={24} sm={24} md={6}>
                      <RangePicker
                        value={auditDateRange}
                        onChange={(dates) => setAuditDateRange(dates as [dayjs.Dayjs | null, dayjs.Dayjs | null])}
                        style={{ width: '100%' }}
                      />
                    </Col>

                    <Col xs={24} sm={24} md={4}>
                      <Space>
                        <Button
                          type="primary"
                          icon={<SearchOutlined />}
                          onClick={() => {
                            setAuditPage(1)
                            fetchAuditLogs()
                          }}
                        >
                          查询
                        </Button>
                        <Button
                          icon={<ReloadOutlined />}
                          onClick={() => {
                            handleResetAudit()
                            setTimeout(fetchAuditLogs, 0)
                          }}
                        >
                          重置
                        </Button>
                      </Space>
                    </Col>
                  </Row>
                </Card>

                {/* 审计日志表格 */}
                <Card className={styles.tableCard} styles={{ body: { padding: 0 } }}>
                  <Table<AuditLogDto>
                    rowKey="id"
                    tableLayout="fixed"
                    scroll={{ x: 1280 }}
                    columns={auditColumns}
                    dataSource={auditData}
                    loading={auditLoading}
                    pagination={{
                      current: auditPage,
                      pageSize: auditPageSize,
                      total: auditTotal,
                      showSizeChanger: true,
                      showTotal: (t) => `共 ${t} 条操作审计日志`,
                      onChange: (p, ps) => {
                        setAuditPage(p)
                        setAuditPageSize(ps)
                      },
                    }}
                  />
                </Card>
              </>
            ),
          }] : []),
          ...(canViewException ? [{
            key: 'exceptions',
            label: (
              <Space>
                <BugOutlined />
                <span>系统未处理异常日志</span>
              </Space>
            ),
            children: (
              <>
                {/* 异常日志筛选条 */}
                <Card className={styles.filterCard} styles={{ body: { padding: '16px 20px' } }}>
                  <Row gutter={[16, 16]} align="middle">
                    <Col xs={24} sm={12} md={6}>
                      <Input
                        placeholder="搜索异常类型、消息摘要或请求路径..."
                        value={exKeyword}
                        onChange={(e) => setExKeyword(e.target.value)}
                        onPressEnter={() => {
                          setExPage(1)
                          fetchExceptionLogs()
                        }}
                        allowClear
                      />
                    </Col>

                    <Col xs={24} sm={12} md={5}>
                      <Input
                        placeholder="Trace ID 追踪码..."
                        value={exTraceId}
                        onChange={(e) => setExTraceId(e.target.value)}
                        onPressEnter={() => {
                          setExPage(1)
                          fetchExceptionLogs()
                        }}
                        allowClear
                      />
                    </Col>

                    <Col xs={24} sm={12} md={7}>
                      <RangePicker
                        value={exDateRange}
                        onChange={(dates) => setExDateRange(dates as [dayjs.Dayjs | null, dayjs.Dayjs | null])}
                        style={{ width: '100%' }}
                      />
                    </Col>

                    <Col xs={24} sm={24} md={6}>
                      <Space>
                        <Button
                          type="primary"
                          icon={<SearchOutlined />}
                          onClick={() => {
                            setExPage(1)
                            fetchExceptionLogs()
                          }}
                        >
                          查询
                        </Button>
                        <Button
                          icon={<ReloadOutlined />}
                          onClick={() => {
                            handleResetEx()
                            setTimeout(fetchExceptionLogs, 0)
                          }}
                        >
                          重置
                        </Button>
                      </Space>
                    </Col>
                  </Row>
                </Card>

                {/* 异常日志表格（支持展开查看详细堆栈） */}
                <Card className={styles.tableCard} styles={{ body: { padding: 0 } }}>
                  <Table<ExceptionLogDto>
                    rowKey="id"
                    tableLayout="fixed"
                    scroll={{ x: 1280 }}
                    columns={exColumns}
                    dataSource={exData}
                    loading={exLoading}
                    expandable={{
                      expandedRowRender: (record) => (
                        <div style={{ padding: '8px 16px' }}>
                          <div className={styles.detailRow}>
                            <span className={styles.detailLabel}>全链路 Trace ID：</span>
                            <Text code copyable style={{ color: '#ef4444', fontWeight: 600 }}>
                              {record.traceId || '—'}
                            </Text>
                          </div>
                          <div className={styles.detailRow}>
                            <span className={styles.detailLabel}>完整异常类型：</span>
                            <Text code>{record.exceptionType}</Text>
                          </div>
                          {record.queryString && (
                            <div className={styles.detailRow}>
                              <span className={styles.detailLabel}>URL 查询参数：</span>
                              <Text code>{record.queryString}</Text>
                            </div>
                          )}
                          <div style={{ marginBottom: 6, fontWeight: 600, color: '#475569' }}>
                            异常堆栈追踪（StackTrace）：
                          </div>
                          <div className={styles.stackTraceBox}>
                            {record.stackTrace || '无堆栈追踪信息'}
                          </div>
                        </div>
                      ),
                    }}
                    pagination={{
                      current: exPage,
                      pageSize: exPageSize,
                      total: exTotal,
                      showSizeChanger: true,
                      showTotal: (t) => `共 ${t} 条系统异常记录`,
                      onChange: (p, ps) => {
                        setExPage(p)
                        setExPageSize(ps)
                      },
                    }}
                  />
                </Card>
              </>
            ),
          }] : []),
        ]}
      />

      {/* 系统异常详情弹窗 */}
      <Modal
        title={
          <Space>
            <BugOutlined style={{ color: '#ef4444', fontSize: 18 }} />
            <span style={{ fontWeight: 600 }}>系统异常详情</span>
          </Space>
        }
        open={isExDetailOpen}
        onCancel={() => setIsExDetailOpen(false)}
        footer={[
          <Button key="close" onClick={() => setIsExDetailOpen(false)}>
            关闭
          </Button>,
          <Button
            key="copyTrace"
            icon={<CopyOutlined />}
            onClick={() => {
              if (selectedException?.traceId) {
                handleCopy(selectedException.traceId, 'Trace ID 已复制')
              }
            }}
          >
            复制 Trace ID
          </Button>,
          <Button
            key="copyAll"
            type="primary"
            danger
            icon={<CopyOutlined />}
            onClick={() => {
              if (selectedException) {
                const detailText = `【异常类型】${selectedException.exceptionType}\n【错误摘要】${selectedException.message}\n【请求】${selectedException.method} ${selectedException.path}\n【时间】${dayjs(selectedException.createdAt).format('YYYY-MM-DD HH:mm:ss')}\n【Trace ID】${selectedException.traceId || '—'}\n【客户端 IP】${selectedException.clientIp || '—'}\n【堆栈追踪】\n${selectedException.stackTrace || '无堆栈追踪'}`
                handleCopy(detailText, '异常完整详情已复制到剪贴板')
              }
            }}
          >
            复制完整异常报告
          </Button>,
        ]}
        width={800}
        destroyOnClose
      >
        {selectedException && (
          <div>
            {/* 错误摘要高亮区域 */}
            <div className={styles.modalAlertBox}>
              <div className={styles.modalAlertText}>
                {selectedException.message || '（无错误消息）'}
              </div>
              <Button
                size="small"
                icon={<CopyOutlined />}
                onClick={() => handleCopy(selectedException.message, '错误摘要已复制')}
              >
                复制摘要
              </Button>
            </div>

            {/* 结构化基本信息 */}
            <Descriptions
              bordered
              size="small"
              column={{ xs: 1, sm: 2 }}
              styles={{ label: { width: '130px', fontWeight: 600, color: '#475569' } }}
            >
              <Descriptions.Item label="发生时间">
                {selectedException.createdAt ? dayjs(selectedException.createdAt).format('YYYY-MM-DD HH:mm:ss') : '—'}
              </Descriptions.Item>
              <Descriptions.Item label="操作用户">
                {selectedException.username || '匿名访客'}
              </Descriptions.Item>
              <Descriptions.Item label="请求方式与路径" span={2}>
                <Space>
                  <Tag color="magenta" style={{ fontWeight: 600 }}>{selectedException.method}</Tag>
                  <Text code copyable>{selectedException.path}</Text>
                </Space>
              </Descriptions.Item>
              <Descriptions.Item label="完整异常类型" span={2}>
                <Text code style={{ color: '#ef4444' }}>{selectedException.exceptionType}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="Trace ID">
                <Text code copyable style={{ color: '#0284c7' }}>
                  {selectedException.traceId || '—'}
                </Text>
              </Descriptions.Item>
              <Descriptions.Item label="客户端 IP">
                <Text>{selectedException.clientIp || '—'}</Text>
              </Descriptions.Item>
              {selectedException.queryString && (
                <Descriptions.Item label="URL 查询参数" span={2}>
                  <Text code>{selectedException.queryString}</Text>
                </Descriptions.Item>
              )}
            </Descriptions>

            {/* 异常堆栈追踪 */}
            <div className={styles.sectionHeader}>
              <span>异常堆栈追踪 (StackTrace)：</span>
              {selectedException.stackTrace && (
                <Button
                  type="link"
                  size="small"
                  icon={<CopyOutlined />}
                  onClick={() => handleCopy(selectedException.stackTrace!, '堆栈信息已复制')}
                >
                  复制堆栈
                </Button>
              )}
            </div>
            <div className={styles.modalStackTraceBox}>
              {selectedException.stackTrace || '无堆栈追踪信息'}
            </div>
          </div>
        )}
      </Modal>

      {/* 操作审计详情弹窗 */}
      <Modal
        title={
          <Space>
            <SafetyCertificateOutlined style={{ color: '#0284c7', fontSize: 18 }} />
            <span style={{ fontWeight: 600 }}>业务操作审计详情</span>
          </Space>
        }
        open={isAuditDetailOpen}
        onCancel={() => setIsAuditDetailOpen(false)}
        footer={[
          <Button key="close" onClick={() => setIsAuditDetailOpen(false)}>
            关闭
          </Button>,
          <Button
            key="copyTrace"
            icon={<CopyOutlined />}
            onClick={() => {
              if (selectedAudit?.traceId) {
                handleCopy(selectedAudit.traceId, 'Trace ID 已复制')
              }
            }}
          >
            复制 Trace ID
          </Button>,
        ]}
        width={750}
        destroyOnClose
      >
        {selectedAudit && (
          <div>
            <Descriptions
              bordered
              size="small"
              column={{ xs: 1, sm: 2 }}
              styles={{ label: { width: '130px', fontWeight: 600, color: '#475569' } }}
            >
              <Descriptions.Item label="发生时间">
                {selectedAudit.createdAt ? dayjs(selectedAudit.createdAt).format('YYYY-MM-DD HH:mm:ss') : '—'}
              </Descriptions.Item>
              <Descriptions.Item label="操作人">
                {selectedAudit.username || '匿名访客'}
              </Descriptions.Item>
              <Descriptions.Item label="业务模块">
                <Tag color={MODULE_COLORS[selectedAudit.module] || 'default'}>
                  {selectedAudit.module}
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="操作行为">
                <Text strong>{selectedAudit.action}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="请求方式与路径" span={2}>
                <Space>
                  <Tag color="blue">{selectedAudit.method}</Tag>
                  <Text code copyable>{selectedAudit.path}</Text>
                </Space>
              </Descriptions.Item>
              <Descriptions.Item label="客户端 IP">
                <Text>{selectedAudit.clientIp || '—'}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="耗时">
                <Tag color={selectedAudit.durationMs < 100 ? 'success' : selectedAudit.durationMs < 500 ? 'warning' : 'error'}>
                  {selectedAudit.durationMs} ms
                </Tag>
              </Descriptions.Item>
              <Descriptions.Item label="Trace ID" span={2}>
                <Text code copyable style={{ color: '#0284c7' }}>
                  {selectedAudit.traceId || '—'}
                </Text>
              </Descriptions.Item>
              <Descriptions.Item label="执行结果" span={2}>
                {selectedAudit.isSuccess ? (
                  <Badge status="success" text={<span style={{ color: '#10b981', fontWeight: 600 }}>执行成功</span>} />
                ) : (
                  <Badge status="error" text={<span style={{ color: '#ef4444', fontWeight: 600 }}>执行失败</span>} />
                )}
              </Descriptions.Item>
            </Descriptions>

            {selectedAudit.errorMessage && (
              <div style={{ marginTop: 16 }}>
                <div style={{ fontWeight: 600, color: '#ef4444', marginBottom: 6 }}>
                  执行失败原因：
                </div>
                <div className={styles.modalAlertBox}>
                  <div className={styles.modalAlertText}>{selectedAudit.errorMessage}</div>
                </div>
              </div>
            )}

            {selectedAudit.parameters && (
              <div style={{ marginTop: 16 }}>
                <div className={styles.sectionHeader}>
                  <span>请求操作入参 (Parameters)：</span>
                  <Button
                    type="link"
                    size="small"
                    icon={<CopyOutlined />}
                    onClick={() => handleCopy(selectedAudit.parameters!, '入参信息已复制')}
                  >
                    复制入参
                  </Button>
                </div>
                <div className={styles.modalStackTraceBox} style={{ maxHeight: 200 }}>
                  {selectedAudit.parameters}
                </div>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  )
}
