/**
 * 统一 API 响应契约
 */
export interface ApiResult<T> {
  code: number
  message: string
  data: T
  traceId?: string
  timestamp?: string
}

/**
 * 分页数据响应契约
 */
export interface PagedResult<T> {
  items: T[]
  pageIndex: number
  pageSize: number
  total: number
}

/**
 * 系统用户信息（对齐后端 Identity 模块 CurrentUserDto）
 */
export interface UserDto {
  /** 用户唯一雪花ID */
  id: string | number
  /** 登录用户名 */
  username: string
  /** 昵称 */
  nickname: string
  /** 电子邮箱 */
  email?: string | null
  /** 头像地址 */
  avatarUrl?: string | null
  /** 角色名称（如 Admin） */
  role: string
  /** GitHub 关联唯一标识 */
  githubId?: string | null
  /** 最后登录时间 */
  lastLoginAt?: string | null
  /** 创建时间 */
  createdAt?: string | null
  /** 是否激活 */
  isActive?: boolean
  /** 是否已设置独立登录密码 */
  hasPassword?: boolean
  /** 授权可访问的菜单路径列表（如：/、/articles 等） */
  menus?: string[]
  /** 拥有的权限代号集合 */
  permissions?: string[]
}

/**
 * 用户列表项数据传输对象
 */
export interface UserListItemDto {
  id: string | number
  username: string
  nickname?: string
  email?: string | null
  avatarUrl?: string | null
  role: string
  isActive: boolean
  createdAt: string
  lastLoginAt?: string | null
  githubId?: string | null
}

/**
 * 双 Token 身份凭证响应对象
 */
export interface AuthTokenDto {
  /** JWT 访问令牌 */
  accessToken: string
  /** 刷新令牌（用于无感续期） */
  refreshToken: string
  /** 令牌类型（固定 Bearer） */
  tokenType: string
  /** 访问令牌有效秒数 */
  expiresIn: number
  /** 当前登录用户信息 */
  user: UserDto
}

/**
 * 账号密码登录请求参数
 */
export interface LoginRequest {
  /** 账号（支持用户名或邮箱） */
  account: string
  /** 明文密码 */
  password: string
}

/**
 * 刷新 Token 请求参数
 */
export interface RefreshTokenRequest {
  refreshToken: string
}

/**
 * 系统公共特性探测响应
 */
export interface PublicSystemConfigDto {
  features: {
    githubLogin: boolean
    aiAssistant: boolean
    autoGenerateArticle: boolean
    storageProvider: 'Local' | 'S3' | string
    redisEnabled: boolean
  }
  site: {
    siteName: string
    version: string
  }
}

/**
 * 撤销 Token 请求参数
 */
export interface RevokeTokenRequest {
  refreshToken: string
}

/**
 * GitHub 授权跳转信息
 */
export interface GitHubAuthorizeDto {
  /** GitHub OAuth 授权页面 URL */
  url: string
  /** CSRF 校验状态随机串 */
  state: string
}

/**
 * GitHub 登录请求参数
 */
export interface GitHubLoginRequest {
  code: string
  state: string
}

/**
 * 文章审核与发布状态
 */
export const ArticleStatus = {
  Draft: 0,
  Published: 1,
  PendingReview: 2,
  Rejected: 3,
} as const

export type ArticleStatusType = typeof ArticleStatus[keyof typeof ArticleStatus]

/**
 * 文章列表项 DTO
 */
export interface ArticleListItemDto {
  id: string | number
  title: string
  summary?: string
  tags: string[]
  categoryId?: string | number
  categoryName?: string
  author?: string
  readingTime?: number
  coverColor?: string
  coverUrl?: string
  views: number
  seriesId?: string | number | null
  status: number
  createdByUserId?: string | number
  createdAt: string
  updatedAt?: string | null
  version?: number
}

/**
 * 文章详情 DTO
 */
export interface ArticleDto extends ArticleListItemDto {
  content: string
}

/**
 * 专栏系列列表项 DTO
 */
export interface SeriesListItemDto {
  id: string | number
  name: string
  description?: string
  coverUrl?: string
  theme?: string
  author?: string
  articleCount?: number
  createdAt?: string
  updatedAt?: string | null
  version?: number
}

/**
 * 专栏系列详情 DTO
 */
export interface SeriesDetailDto extends SeriesListItemDto {
  articles: ArticleListItemDto[]
}

/**
 * 创建专栏系列请求
 */
export interface CreateSeriesRequest {
  name: string
  description?: string
  coverUrl?: string
  theme?: string
}

/**
 * 更新专栏系列请求
 */
export interface UpdateSeriesRequest {
  name: string
  description?: string
  coverUrl?: string
  theme?: string
  version?: number
}

/**
 * 创建文章请求
 */
export interface CreateArticleRequest {
  title: string
  content: string
  summary?: string
  tags: string[]
  categoryId?: string | number | null
  author?: string
  readingTime?: number
  coverColor?: string
  coverUrl?: string
  seriesId?: string | number | null
  status?: number
}

/**
 * 更新文章请求
 */
export interface UpdateArticleRequest {
  title: string
  content: string
  summary?: string
  tags: string[]
  categoryId?: string | number | null
  author?: string
  readingTime?: number
  coverColor?: string
  coverUrl?: string
  seriesId?: string | number | null
  status?: number
  version?: number
}

/**
 * 系统角色 DTO
 */
export interface RoleDto {
  id: string | number
  code: string
  name: string
  description?: string
  isSystem?: boolean
  menuIds?: (string | number)[]
}

/**
 * 系统权限元数据说明项（由后端直接直出）
 */
export interface PermissionMetadata {
  code: string
  name: string
  module: string
  description?: string | null
}

/**
 * 菜单与权限资源 DTO
 */
export interface MenuDto {
  id: string | number
  parentId?: string | number | null
  name: string
  path: string
  permissionCode: string
  icon?: string | null
  sort: number
  isHidden: boolean
  /** 资源类型：1-目录, 2-菜单/页面, 3-按钮/操作权限 */
  type?: number
  children?: MenuDto[]
}

/**
 * 创建菜单请求参数
 */
export interface CreateMenuRequest {
  parentId?: string | number | null
  name: string
  path: string
  permissionCode?: string
  icon?: string | null
  sort?: number
  isHidden?: boolean
  type?: number
}

/**
 * 更新菜单请求参数
 */
export interface UpdateMenuRequest {
  parentId?: string | number | null
  name: string
  path: string
  permissionCode?: string
  icon?: string | null
  sort?: number
  isHidden?: boolean
  type?: number
}

/**
 * 创建角色请求参数
 */
export interface CreateRoleRequest {
  code: string
  name: string
  description?: string
  menuIds?: (string | number)[]
}

/**
 * 更新角色请求参数
 */
export interface UpdateRoleRequest {
  name: string
  description?: string
  menuIds?: (string | number)[]
}

/**
 * 文章分类 DTO
 */
export interface CategoryDto {
  id: string | number
  name: string
  description?: string
  icon?: string
  sort?: number
  articleCount?: number
}

/**
 * 创建文章分类请求参数
 */
export interface CreateCategoryRequest {
  name: string
  description?: string
  icon?: string
  sort?: number
}

/**
 * 更新文章分类请求参数
 */
export interface UpdateCategoryRequest {
  name: string
  description?: string
  icon?: string
  sort?: number
}
