-- ============================================================================
-- 升级脚本 001：新增「站点设置」表与菜单权限
-- 适用场景：数据库卷已存在（init.sql 不会重复执行）的部署
-- 执行方式：
--   docker compose exec -T postgres psql -U postgres -d blog < chakra_api_back_end/scripts/sql/upgrade/001_site_settings.sql
-- ============================================================================

BEGIN;

-- 1. 站点设置表（全站单行 JSON 配置，仅保存对代码默认值的覆盖项）
CREATE TABLE IF NOT EXISTS site_settings (
    id BIGINT PRIMARY KEY,
    config JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NULL,
    version INT NOT NULL DEFAULT 0,
    is_del BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL
);

-- 2. 菜单与操作权限项
INSERT INTO menus (id, parent_id, name, path, permission_code, icon, sort, is_hidden, type, created_at, is_del)
VALUES
    (650, NULL, '站点设置', '/settings', 'settings:manage', 'SettingOutlined', 9, FALSE, 2, NOW(), FALSE),
    (651, 650, '维护站点设置', '', 'system:settings:manage', NULL, 1, TRUE, 3, NOW(), FALSE)
ON CONFLICT (id) DO NOTHING;

-- 3. 超级管理员角色关联（超管本身具备通配特权，此处用于角色权限界面展示）
INSERT INTO role_menus (id, role_id, menu_id, created_at, is_del)
VALUES
    (1009, 1, 650, NOW(), FALSE),
    (1010, 1, 651, NOW(), FALSE)
ON CONFLICT DO NOTHING;

COMMIT;
