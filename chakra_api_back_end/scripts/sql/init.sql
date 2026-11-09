
-- ============================================================================
-- 博客系统 PostgreSQL 全量数据库初始化脚本
-- ============================================================================
-- 本脚本仅用于初始化数据库，可重复执行；不会更新或覆盖已存在的业务数据。
-- 后续结构变更请通过独立、显式、人工执行的版本化 SQL 脚本完成。

BEGIN;

CREATE TABLE IF NOT EXISTS system_stats (
    key VARCHAR(50) PRIMARY KEY,
    value BIGINT NOT NULL DEFAULT 0 CHECK (value >= 0)
);

-- 1. 系列表 (series)
CREATE TABLE IF NOT EXISTS series (
    id BIGINT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500) NOT NULL DEFAULT '',
    cover_url VARCHAR(500) NOT NULL DEFAULT '',
    theme VARCHAR(50) NOT NULL DEFAULT '',
    author VARCHAR(100) NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_series_name ON series (name);
CREATE INDEX IF NOT EXISTS ix_series_author ON series (author);
CREATE INDEX IF NOT EXISTS ix_series_is_del ON series (is_del);

-- 2. 文章分类表 (categories)
CREATE TABLE IF NOT EXISTS categories (
    id BIGINT PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(200) NOT NULL DEFAULT '',
    icon VARCHAR(50) NOT NULL DEFAULT '',
    sort INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_categories_sort ON categories (sort);

-- 3. 文章表 (articles)
CREATE TABLE IF NOT EXISTS articles (
    id BIGINT PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    summary VARCHAR(500) NOT NULL DEFAULT '',
    reading_time INT NOT NULL DEFAULT 0,
    tags TEXT[] NOT NULL DEFAULT '{}',
    category_id BIGINT NOT NULL DEFAULT 0 REFERENCES categories(id) ON DELETE RESTRICT,
    author VARCHAR(100) NOT NULL DEFAULT '',
    cover_color VARCHAR(50) NOT NULL DEFAULT '',
    cover_url VARCHAR(500) NOT NULL DEFAULT '',
    views BIGINT NOT NULL DEFAULT 0,
    likes BIGINT NOT NULL DEFAULT 0,
    series_id BIGINT NULL REFERENCES series(id) ON DELETE SET NULL,
    status INT NOT NULL DEFAULT 1,
    created_by_user_id BIGINT NOT NULL DEFAULT 0,
    source_link VARCHAR(500) NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_articles_category_id ON articles (category_id);
CREATE INDEX IF NOT EXISTS ix_articles_created_at ON articles (created_at);
CREATE INDEX IF NOT EXISTS ix_articles_is_del ON articles (is_del);
CREATE INDEX IF NOT EXISTS ix_articles_series_id ON articles (series_id);
CREATE INDEX IF NOT EXISTS ix_articles_status ON articles (status);
CREATE INDEX IF NOT EXISTS ix_articles_created_by_user_id ON articles (created_by_user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ix_articles_source_link ON articles (source_link) WHERE source_link IS NOT NULL;

-- 4. 用户表 (users)
CREATE TABLE IF NOT EXISTS users (
    id BIGINT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NULL,
    password_hash VARCHAR(256) NULL,
    nickname VARCHAR(50) NOT NULL DEFAULT '',
    avatar_url VARCHAR(500) NULL,
    role VARCHAR(30) NOT NULL DEFAULT 'Admin',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    github_id VARCHAR(100) NULL,
    last_login_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_username ON users (username);
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON users (email) WHERE email IS NOT NULL AND is_del = FALSE;
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_github_id ON users (github_id) WHERE github_id IS NOT NULL AND is_del = FALSE;
CREATE INDEX IF NOT EXISTS ix_users_is_del ON users (is_del);

-- 5. 刷新令牌表 (refresh_tokens)
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(256) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMPTZ NULL,
    replaced_by_token VARCHAR(256) NULL,
    created_by_ip VARCHAR(50) NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_refresh_tokens_token ON refresh_tokens (token);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON refresh_tokens (user_id);

-- 6. 操作审计日志表 (audit_logs)
CREATE TABLE IF NOT EXISTS audit_logs (
    id BIGINT PRIMARY KEY,
    user_id BIGINT NULL,
    username VARCHAR(50) NULL,
    module VARCHAR(50) NOT NULL DEFAULT '',
    action VARCHAR(100) NOT NULL DEFAULT '',
    method VARCHAR(10) NOT NULL DEFAULT '',
    path VARCHAR(256) NOT NULL DEFAULT '',
    parameters TEXT NULL,
    client_ip VARCHAR(50) NULL,
    duration_ms BIGINT NOT NULL DEFAULT 0,
    is_success BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT NULL,
    trace_id VARCHAR(64) NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_audit_logs_created_at ON audit_logs (created_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_logs_module ON audit_logs (module);
CREATE INDEX IF NOT EXISTS ix_audit_logs_username ON audit_logs (username);
CREATE INDEX IF NOT EXISTS ix_audit_logs_is_success ON audit_logs (is_success);
CREATE INDEX IF NOT EXISTS ix_audit_logs_trace_id ON audit_logs (trace_id);

-- 7. 系统未处理异常日志表 (exception_logs)
CREATE TABLE IF NOT EXISTS exception_logs (
    id BIGINT PRIMARY KEY,
    exception_type VARCHAR(200) NOT NULL DEFAULT '',
    message TEXT NOT NULL DEFAULT '',
    stack_trace TEXT NULL,
    path VARCHAR(256) NOT NULL DEFAULT '',
    method VARCHAR(10) NOT NULL DEFAULT '',
    query_string VARCHAR(500) NULL,
    request_body TEXT NULL,
    client_ip VARCHAR(50) NULL,
    user_id BIGINT NULL,
    username VARCHAR(50) NULL,
    trace_id VARCHAR(64) NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_exception_logs_created_at ON exception_logs (created_at DESC);
CREATE INDEX IF NOT EXISTS ix_exception_logs_exception_type ON exception_logs (exception_type);
CREATE INDEX IF NOT EXISTS ix_exception_logs_trace_id ON exception_logs (trace_id);

-- 8. 角色表 (roles)
CREATE TABLE IF NOT EXISTS roles (
    id BIGINT PRIMARY KEY,
    code VARCHAR(30) UNIQUE NOT NULL,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(200) NOT NULL DEFAULT '',
    is_system BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_roles_code ON roles (code);

-- 9. 菜单与权限资源表 (menus)
CREATE TABLE IF NOT EXISTS menus (
    id BIGINT PRIMARY KEY,
    parent_id BIGINT NULL,
    name VARCHAR(50) NOT NULL,
    path VARCHAR(100) NOT NULL,
    permission_code VARCHAR(50) NOT NULL,
    icon VARCHAR(50) NULL,
    sort INT NOT NULL DEFAULT 0,
    is_hidden BOOLEAN NOT NULL DEFAULT FALSE,
    type INT NOT NULL DEFAULT 2,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_menus_path ON menus (path);
CREATE INDEX IF NOT EXISTS ix_menus_sort ON menus (sort);

-- 10. 角色与菜单关联映射表 (role_menus)
CREATE TABLE IF NOT EXISTS role_menus (
    id BIGINT PRIMARY KEY,
    role_id BIGINT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    menu_id BIGINT NOT NULL REFERENCES menus(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_role_menus_role_menu ON role_menus (role_id, menu_id);

-- 11. 站点设置表 (site_settings)：全站单行 JSON 配置，仅保存对代码默认值的覆盖项
CREATE TABLE IF NOT EXISTS site_settings (
    id BIGINT PRIMARY KEY,
    config JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

-- ============================================================================
-- 预置种子数据 (Seed Data)
-- ============================================================================

-- 预置文章分类
INSERT INTO categories (id, name, description, icon, sort, created_at, is_del)
VALUES
    (1, '.NET / C#', '现代 .NET 10、C# 语法与云原生微服务开发实践', 'Code2', 1, NOW(), FALSE),
    (2, 'AI Agent', '大模型协同、语义检索与智能体架构落地探索', 'Bot', 2, NOW(), FALSE),
    (3, '通用技术', '通用软件架构、设计模式与工程实践随笔', 'Sparkles', 3, NOW(), FALSE)
ON CONFLICT (id) DO NOTHING;

-- 预置系统基础角色（Admin: 超级管理员, Editor: 内容编辑者）
INSERT INTO roles (id, code, name, description, is_system, created_at, is_del)
VALUES 
    (1, 'Admin', '超级管理员', '系统最高管理权限，负责文章审核、用户角色分配与系统维护', TRUE, NOW(), FALSE),
    (2, 'Editor', '内容编辑者', '文章创作权限，创建文章草稿、提交管理员审核，仅可管理自身文章', TRUE, NOW(), FALSE)
ON CONFLICT (code) DO NOTHING;

-- 预置首次登录管理员（用户名 admin，初始密码 AdminPassword123!；生产部署必须修改）
INSERT INTO users (id, username, email, password_hash, nickname, role, is_active, created_at, is_del)
VALUES (
    1,
    'admin',
    'admin@example.com',
    'AQAAAAIAAYagAAAAED4vKwJ83Y7zWnMFeOMCrlfYDxK5aGAocSlcR4RxXzhhkfgkfkpxB5gOSWwi81vU8g==',
    '超级管理员',
    'Admin',
    TRUE,
    NOW(),
    FALSE)
ON CONFLICT (username) DO NOTHING;

-- 预置系统核心菜单与细粒度按钮权限项 (type: 1-目录, 2-菜单/页面, 3-按钮/操作)
INSERT INTO menus (id, parent_id, name, path, permission_code, icon, sort, is_hidden, type, created_at, is_del)
VALUES
    (100, NULL, '仪表盘', '/', 'dashboard:view', 'DashboardOutlined', 1, FALSE, 2, NOW(), FALSE),
    (200, NULL, '文章管理', '/articles', 'article:manage', 'FileTextOutlined', 2, FALSE, 2, NOW(), FALSE),
    (250, NULL, '分类管理', '/categories', 'category:manage', 'FolderOutlined', 3, FALSE, 2, NOW(), FALSE),
    (300, NULL, '专栏管理', '/series', 'series:manage', 'BookOutlined', 4, FALSE, 2, NOW(), FALSE),
    (400, NULL, '系统日志', '/logs', 'log:view', 'SafetyCertificateOutlined', 5, FALSE, 2, NOW(), FALSE),
    (500, NULL, '角色权限', '/roles', 'role:manage', 'TeamOutlined', 6, FALSE, 2, NOW(), FALSE),
    (550, NULL, '菜单管理', '/menus', 'menu:manage', 'AppstoreOutlined', 7, FALSE, 2, NOW(), FALSE),
    (600, NULL, '个人中心', '/users', 'user:manage', 'UserOutlined', 8, FALSE, 2, NOW(), FALSE),
    (650, NULL, '站点设置', '/settings', 'settings:manage', 'SettingOutlined', 9, FALSE, 2, NOW(), FALSE),

    -- 文章管理细粒度按钮/操作 (type = 3)
    (201, 200, '查看文章', '', 'content:article:view', NULL, 1, TRUE, 3, NOW(), FALSE),
    (202, 200, '新建文章', '', 'content:article:create', NULL, 2, TRUE, 3, NOW(), FALSE),
    (203, 200, '编辑文章', '', 'content:article:edit', NULL, 3, TRUE, 3, NOW(), FALSE),
    (204, 200, '删除文章', '', 'content:article:delete', NULL, 4, TRUE, 3, NOW(), FALSE),
    (205, 200, '审核发布', '', 'content:article:approve', NULL, 5, TRUE, 3, NOW(), FALSE),
    (206, 200, '提交审核', '', 'content:article:submit', NULL, 6, TRUE, 3, NOW(), FALSE),

    -- 分类管理细粒度按钮/操作 (type = 3)
    (251, 250, '查看分类', '', 'content:category:view', NULL, 1, TRUE, 3, NOW(), FALSE),
    (252, 250, '新建分类', '', 'content:category:create', NULL, 2, TRUE, 3, NOW(), FALSE),
    (253, 250, '编辑分类', '', 'content:category:edit', NULL, 3, TRUE, 3, NOW(), FALSE),
    (254, 250, '删除分类', '', 'content:category:delete', NULL, 4, TRUE, 3, NOW(), FALSE),

    -- 专栏管理细粒度按钮/操作 (type = 3)
    (301, 300, '查看专栏', '', 'content:series:view', NULL, 1, TRUE, 3, NOW(), FALSE),
    (302, 300, '新建专栏', '', 'content:series:create', NULL, 2, TRUE, 3, NOW(), FALSE),
    (303, 300, '编辑专栏', '', 'content:series:edit', NULL, 3, TRUE, 3, NOW(), FALSE),
    (304, 300, '删除专栏', '', 'content:series:delete', NULL, 4, TRUE, 3, NOW(), FALSE),

    -- 系统日志操作项 (type = 3)
    (401, 400, '审计日志', '', 'system:log:audit:view', NULL, 1, TRUE, 3, NOW(), FALSE),
    (402, 400, '异常日志', '', 'system:log:exception:view', NULL, 2, TRUE, 3, NOW(), FALSE),

    -- 角色管理操作项 (type = 3)
    (501, 500, '查看角色', '', 'system:role:view', NULL, 1, TRUE, 3, NOW(), FALSE),
    (502, 500, '维护角色', '', 'system:role:manage', NULL, 2, TRUE, 3, NOW(), FALSE),

    -- 菜单管理操作项 (type = 3)
    (551, 550, '查看菜单', '', 'system:menu:view', NULL, 1, TRUE, 3, NOW(), FALSE),
    (552, 550, '维护菜单', '', 'system:menu:manage', NULL, 2, TRUE, 3, NOW(), FALSE),

    -- 个人中心操作项 (type = 3)
    (601, 600, '查看用户', '', 'system:user:view', NULL, 1, TRUE, 3, NOW(), FALSE),
    (602, 600, '分配角色', '', 'system:user:role:update', NULL, 2, TRUE, 3, NOW(), FALSE),

    -- 站点设置操作项 (type = 3)
    (651, 650, '维护站点设置', '', 'system:settings:manage', NULL, 1, TRUE, 3, NOW(), FALSE)
ON CONFLICT (id) DO NOTHING;

-- 预置默认角色与菜单关联 (Admin: 超管全权; Editor: 仅限个人文章创作、编辑与提审)
INSERT INTO role_menus (id, role_id, menu_id, created_at, is_del)
VALUES
    (1001, 1, 100, NOW(), FALSE),
    (1002, 1, 200, NOW(), FALSE),
    (1003, 1, 300, NOW(), FALSE),
    (1004, 1, 400, NOW(), FALSE),
    (1005, 1, 500, NOW(), FALSE),
    (1006, 1, 600, NOW(), FALSE),
    (1007, 1, 550, NOW(), FALSE),
    (1008, 1, 250, NOW(), FALSE),
    -- Editor 角色权限分配（只能管理自己创作的文章，严禁分配审核发布权限 205）
    (2001, 2, 200, NOW(), FALSE),
    (2002, 2, 201, NOW(), FALSE),
    (2003, 2, 202, NOW(), FALSE),
    (2004, 2, 203, NOW(), FALSE),
    (2005, 2, 204, NOW(), FALSE),
    (2006, 2, 206, NOW(), FALSE),
    (2007, 2, 600, NOW(), FALSE),
    (2008, 2, 601, NOW(), FALSE),
    -- Admin 站点设置（超管本身具备通配特权，此处用于角色权限界面展示）
    (1009, 1, 650, NOW(), FALSE),
    (1010, 1, 651, NOW(), FALSE)
ON CONFLICT (id) DO NOTHING;

COMMIT;
