import { useEffect, useState, useRef, useCallback } from 'react'
import {
  Row,
  Col,
  Typography,
  Tag,
  Space,
  Button,
  Table,
  Badge,
  Spin,
  App,
  Tooltip,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  RocketOutlined,
  ExportOutlined,
  EyeOutlined,
  FileTextOutlined,
  ThunderboltOutlined,
  BookOutlined,
  PlusOutlined,
  EditOutlined,
  SafetyCertificateOutlined,
  LineChartOutlined,
  PieChartOutlined,
  CheckCircleFilled,
  ReloadOutlined,
} from '@ant-design/icons'
import * as echarts from 'echarts'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import { brand, FRONT_URL } from '@/config/brand'
import PermissionGuard from '@/components/PermissionGuard'
import { PERMISSIONS } from '@/constants/permissions'
import * as dashboardApi from '@/api/dashboard'
import type { DashboardSummaryDto, RecentArticleDto } from '@/api/dashboard'
import dayjs from 'dayjs'
import styles from './Dashboard.module.css'

const { Title, Paragraph, Text } = Typography

export default function Dashboard() {
  const user = useAuthStore((s) => s.user)
  const navigate = useNavigate()
  const { message } = App.useApp()

  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<DashboardSummaryDto | null>(null)

  const trendChartRef = useRef<HTMLDivElement>(null)
  const categoryChartRef = useRef<HTMLDivElement>(null)

  // 获取仪表盘汇总数据
  const fetchSummary = useCallback(async () => {
    setLoading(true)
    try {
      const res = await dashboardApi.getDashboardSummary()
      setData(res)
    } catch (err) {
      message.error(err instanceof Error ? err.message : '获取仪表盘统计数据失败')
    } finally {
      setLoading(false)
    }
  }, [message])

  useEffect(() => {
    fetchSummary()
  }, [fetchSummary])

  // 1. 初始化近期发文与活跃波形图 (Area Chart)
  useEffect(() => {
    if (!trendChartRef.current || !data) return
    const chart = echarts.init(trendChartRef.current)

    const dates = data.trendData.map((d) => d.date)
    const counts = data.trendData.map((d) => d.articleCount)

    const option: echarts.EChartsOption = {
      tooltip: {
        trigger: 'axis',
        backgroundColor: '#0f172a',
        borderColor: '#1e293b',
        textStyle: { color: '#f8fafc' },
        formatter: (params: unknown) => {
          const p = Array.isArray(params) ? params[0] : params
          const item = p as { axisValue: string; data: number }
          return `<div style="padding: 2px 4px;">
            <div style="color: #94a3b8; font-size: 12px;">${item.axisValue}</div>
            <div style="font-weight: 700; font-size: 14px; margin-top: 2px;">发布文章: <span style="color: #38bdf8;">${item.data}</span> 篇</div>
          </div>`
        },
      },
      grid: {
        left: '2%',
        right: '3%',
        bottom: '3%',
        top: '12%',
        containLabel: true,
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: dates,
        axisLine: { lineStyle: { color: '#cbd5e1' } },
        axisLabel: { color: '#64748b' },
      },
      yAxis: {
        type: 'value',
        minInterval: 1,
        splitLine: { lineStyle: { color: '#f1f5f9', type: 'dashed' } },
        axisLabel: { color: '#64748b' },
      },
      series: [
        {
          name: '当日发文',
          type: 'line',
          smooth: true,
          data: counts,
          symbolSize: 8,
          itemStyle: { color: '#0284c7' },
          lineStyle: { width: 3, color: '#0284c7' },
          areaStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
              { offset: 0, color: 'rgba(2, 132, 199, 0.45)' },
              { offset: 1, color: 'rgba(2, 132, 199, 0.02)' },
            ]),
          },
        },
      ],
    }

    chart.setOption(option)
    const handleResize = () => chart.resize()
    window.addEventListener('resize', handleResize)

    return () => {
      window.removeEventListener('resize', handleResize)
      chart.dispose()
    }
  }, [data])

  // 2. 初始化分类分布环形图 (Doughnut Chart)
  useEffect(() => {
    if (!categoryChartRef.current || !data) return
    const chart = echarts.init(categoryChartRef.current)

    const chartData = data.categoryDistribution.map((item) => ({
      name: item.category,
      value: item.count,
    }))

    const colors = ['#0284c7', '#10b981', '#8b5cf6', '#f59e0b', '#f43f5e', '#06b6d4', '#64748b']

    const option: echarts.EChartsOption = {
      tooltip: {
        trigger: 'item',
        backgroundColor: '#0f172a',
        borderColor: '#1e293b',
        textStyle: { color: '#f8fafc' },
        formatter: '{b}: <span style="font-weight:700;">{c} 篇</span> ({d}%)',
      },
      legend: {
        bottom: '2%',
        left: 'center',
        itemWidth: 10,
        itemHeight: 10,
        textStyle: { color: '#64748b', fontSize: 12 },
      },
      series: [
        {
          name: '分类分布',
          type: 'pie',
          radius: ['42%', '68%'],
          center: ['50%', '44%'],
          avoidLabelOverlap: false,
          itemStyle: {
            borderRadius: 6,
            borderColor: '#ffffff',
            borderWidth: 2,
          },
          label: {
            show: false,
            position: 'center',
          },
          emphasis: {
            label: {
              show: true,
              fontSize: 15,
              fontWeight: 'bold',
              color: '#0f172a',
            },
          },
          color: colors,
          data: chartData.length > 0 ? chartData : [{ name: '暂无分类', value: 0 }],
        },
      ],
    }

    chart.setOption(option)
    const handleResize = () => chart.resize()
    window.addEventListener('resize', handleResize)

    return () => {
      window.removeEventListener('resize', handleResize)
      chart.dispose()
    }
  }, [data])

  // 最新文章表格列定义
  const recentColumns: ColumnsType<RecentArticleDto> = [
    {
      title: '文章标题',
      dataIndex: 'title',
      key: 'title',
      render: (title: string, record) => (
        <Space direction="vertical" size={2}>
          <Text strong style={{ color: '#0f172a', fontSize: 14 }}>
            {title}
          </Text>
          <Space size={6}>
            <Tag color="blue" style={{ borderRadius: 4, fontSize: 11 }}>
              {record.category}
            </Tag>
            <Text type="secondary" style={{ fontSize: 12 }}>
              {record.author || '博主'}
            </Text>
          </Space>
        </Space>
      ),
    },
    {
      title: '阅读量',
      dataIndex: 'views',
      key: 'views',
      width: 100,
      render: (views: number) => (
        <Badge
          count={views}
          overflowCount={999999}
          style={{ backgroundColor: '#0284c7' }}
        />
      ),
    },
    {
      title: '发布时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 140,
      render: (d: string) => (d ? dayjs(d).format('MM-DD HH:mm') : '—'),
    },
    {
      title: '操作',
      key: 'action',
      width: 110,
      render: (_, record) => (
        <Space size={4}>
          <Tooltip title="快速编辑">
            <Button
              type="text"
              icon={<EditOutlined />}
              onClick={() => navigate(`/articles/${record.id}/edit`)}
            />
          </Tooltip>
          <Tooltip title="新标签页预览">
            <Button
              type="text"
              icon={<ExportOutlined />}
              onClick={() => window.open(`${FRONT_URL}/articles/${record.id}`, '_blank')}
            />
          </Tooltip>
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
                AUTHENTICATED AS {user?.role?.toUpperCase() || 'ADMIN'}
              </Tag>
              <Tag color="cyan">
                {brand.techStackLabel}
              </Tag>
            </Space>
            <Title level={2} className={styles.welcomeTitle}>
              欢迎回来，{user?.nickname || user?.username || '博主'}
            </Title>
            <Paragraph className={styles.welcomeSubtitle}>
              {brand.dashboardDesc}
            </Paragraph>
          </Col>
          <Col xs={24} md={8} style={{ textAlign: 'right' }}>
            <Space>
              <Button
                icon={<ReloadOutlined />}
                onClick={fetchSummary}
                loading={loading}
              >
                刷新数据
              </Button>
              <PermissionGuard code={PERMISSIONS.ARTICLE_CREATE}>
                <Button
                  type="primary"
                  icon={<RocketOutlined />}
                  onClick={() => navigate('/articles/create')}
                >
                  发布新文章
                </Button>
              </PermissionGuard>
            </Space>
          </Col>
        </Row>
      </div>

      <Spin spinning={loading}>
        {/* 4 大核心 KPI 指标卡片 */}
        <Row gutter={[20, 20]} style={{ marginBottom: 24 }}>
          <Col xs={24} sm={12} lg={6}>
            <div className={styles.kpiCard}>
              <div>
                <div className={styles.kpiLabel}>全站累计访问量</div>
                <div className={styles.kpiValue} style={{ color: '#0284c7' }}>
                  {(data?.totalVisits ?? 0).toLocaleString()}
                </div>
              </div>
              <div
                className={styles.kpiIconWrapper}
                style={{ background: '#f0f9ff', color: '#0284c7' }}
              >
                <EyeOutlined />
              </div>
            </div>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <div className={styles.kpiCard}>
              <div>
                <div className={styles.kpiLabel}>收录文章总数</div>
                <div className={styles.kpiValue} style={{ color: '#10b981' }}>
                  {(data?.totalArticles ?? 0).toLocaleString()}
                </div>
              </div>
              <div
                className={styles.kpiIconWrapper}
                style={{ background: '#ecfdf5', color: '#10b981' }}
              >
                <FileTextOutlined />
              </div>
            </div>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <div className={styles.kpiCard}>
              <div>
                <div className={styles.kpiLabel}>文章累计阅读量</div>
                <div className={styles.kpiValue} style={{ color: '#8b5cf6' }}>
                  {(data?.totalViews ?? 0).toLocaleString()}
                </div>
              </div>
              <div
                className={styles.kpiIconWrapper}
                style={{ background: '#f5f3ff', color: '#8b5cf6' }}
              >
                <ThunderboltOutlined />
              </div>
            </div>
          </Col>

          <Col xs={24} sm={12} lg={6}>
            <div className={styles.kpiCard}>
              <div>
                <div className={styles.kpiLabel}>专栏系列总数</div>
                <div className={styles.kpiValue} style={{ color: '#f59e0b' }}>
                  {(data?.totalSeries ?? 0).toLocaleString()}
                </div>
              </div>
              <div
                className={styles.kpiIconWrapper}
                style={{ background: '#fffbeb', color: '#f59e0b' }}
              >
                <BookOutlined />
              </div>
            </div>
          </Col>
        </Row>

        {/* ECharts 图表联动行 */}
        <Row gutter={[20, 20]} style={{ marginBottom: 24 }}>
          {/* 左侧：7天发布趋势面积图 */}
          <Col xs={24} lg={15}>
            <div className={styles.chartCard}>
              <div className={styles.chartHeader}>
                <div className={styles.chartTitle}>
                  <LineChartOutlined style={{ color: '#0284c7' }} />
                  <span>近 7 天文章发布趋势</span>
                </div>
                <Text type="secondary" style={{ fontSize: 13 }}>
                  每日发文数量追踪
                </Text>
              </div>
              <div ref={trendChartRef} className={styles.chartContainer} />
            </div>
          </Col>

          {/* 右侧：技术分类收录分布环形图 */}
          <Col xs={24} lg={9}>
            <div className={styles.chartCard}>
              <div className={styles.chartHeader}>
                <div className={styles.chartTitle}>
                  <PieChartOutlined style={{ color: '#8b5cf6' }} />
                  <span>技术分类文章占比</span>
                </div>
                <Text type="secondary" style={{ fontSize: 13 }}>
                  共 {data?.categoryDistribution.length ?? 0} 个分类
                </Text>
              </div>
              <div ref={categoryChartRef} className={styles.chartContainer} />
            </div>
          </Col>
        </Row>

        {/* 底部：最新文章列表 + 快捷操作与系统架构参考 */}
        <Row gutter={[20, 20]}>
          {/* 左侧：最新发布的 5 篇文章 */}
          <Col xs={24} lg={16}>
            <div className={styles.chartCard} style={{ marginBottom: 0 }}>
              <div className={styles.chartHeader}>
                <div className={styles.chartTitle}>
                  <FileTextOutlined style={{ color: '#10b981' }} />
                  <span>最新发布文章（Top 5）</span>
                </div>
                <Button type="link" onClick={() => navigate('/articles')}>
                  查看全部文章 →
                </Button>
              </div>
              <Table<RecentArticleDto>
                rowKey="id"
                columns={recentColumns}
                dataSource={data?.recentArticles || []}
                pagination={false}
                size="middle"
              />
            </div>
          </Col>

          {/* 右侧：快捷操作通道与运行状态 */}
          <Col xs={24} lg={8}>
            <div className={styles.chartCard} style={{ marginBottom: 0, padding: 22 }}>
              <div style={{ fontSize: 16, fontWeight: 700, color: '#0f172a', marginBottom: 16 }}>
                快捷通道
              </div>

              <Space direction="vertical" style={{ width: '100%' }} size={10}>
                <PermissionGuard code={PERMISSIONS.ARTICLE_CREATE}>
                  <button
                    type="button"
                    className={styles.quickActionBtn}
                    onClick={() => navigate('/articles/create')}
                  >
                    <div
                      className={styles.quickActionIcon}
                      style={{ background: '#f0f9ff', color: '#0284c7' }}
                    >
                      <PlusOutlined />
                    </div>
                    <div>
                      <div className={styles.quickActionTitle}>创作 Markdown 文章</div>
                      <div className={styles.quickActionDesc}>进入沉浸式高亮编辑工作台</div>
                    </div>
                  </button>
                </PermissionGuard>

                <PermissionGuard code={PERMISSIONS.SERIES_VIEW}>
                  <button
                    type="button"
                    className={styles.quickActionBtn}
                    onClick={() => navigate('/series')}
                  >
                    <div
                      className={styles.quickActionIcon}
                      style={{ background: '#fdf4ff', color: '#a855f7' }}
                    >
                      <BookOutlined />
                    </div>
                    <div>
                      <div className={styles.quickActionTitle}>管理专栏系列</div>
                      <div className={styles.quickActionDesc}>配置专栏封面与主题色</div>
                    </div>
                  </button>
                </PermissionGuard>

                <PermissionGuard code={PERMISSIONS.LOG_AUDIT_VIEW}>
                  <button
                    type="button"
                    className={styles.quickActionBtn}
                    onClick={() => navigate('/logs')}
                  >
                    <div
                      className={styles.quickActionIcon}
                      style={{ background: '#ecfdf5', color: '#10b981' }}
                    >
                      <SafetyCertificateOutlined />
                    </div>
                    <div>
                      <div className={styles.quickActionTitle}>系统运行日志</div>
                      <div className={styles.quickActionDesc}>查看操作审计与异常堆栈</div>
                    </div>
                  </button>
                </PermissionGuard>
              </Space>

              <div
                style={{
                  fontSize: 14,
                  fontWeight: 700,
                  color: '#0f172a',
                  marginTop: 24,
                  marginBottom: 12,
                }}
              >
                系统技术架构
              </div>

              <div className={styles.systemStatusItem}>
                <span style={{ color: '#475569' }}>后端服务 (.NET 10 WebAPI)</span>
                <Tag color="blue" style={{ borderRadius: 4 }}>
                  ASP.NET CORE
                </Tag>
              </div>

              <div className={styles.systemStatusItem}>
                <span style={{ color: '#475569' }}>分布式缓存 (Redis)</span>
                <Tag color="cyan" style={{ borderRadius: 4 }}>
                  CACHE
                </Tag>
              </div>

              <div className={styles.systemStatusItem}>
                <span style={{ color: '#475569' }}>主关系型数据库 (PostgreSQL)</span>
                <Tag color="geekblue" style={{ borderRadius: 4 }}>
                  NPGSQL / EF
                </Tag>
              </div>
            </div>
          </Col>
        </Row>
      </Spin>
    </div>
  )
}
