using Dapper;
using Npgsql;

namespace Chakra.Tests.Integration;

internal static class PostgresTestData
{
    public static Task<int> InsertCategoryAsync(
        NpgsqlConnection connection,
        long id,
        string name,
        bool isDeleted = false,
        int sort = 0)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO categories (id, name, description, icon, sort, created_at, is_del)
            VALUES (@Id, @Name, @Description, '', @Sort, @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                Name = name,
                Description = $"{name} description",
                Sort = sort,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = isDeleted,
            });
    }

    public static Task<int> InsertSeriesAsync(
        NpgsqlConnection connection,
        long id,
        string name,
        bool isDeleted = false)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO series (id, name, description, cover_url, theme, author, created_at, is_del)
            VALUES (@Id, @Name, @Description, '', 'tech', 'tester', @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                Name = name,
                Description = $"{name} description",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = isDeleted,
            });
    }

    public static Task<int> InsertArticleAsync(
        NpgsqlConnection connection,
        long id,
        string title,
        long categoryId,
        int status,
        string[]? tags = null,
        long? seriesId = null,
        long views = 0,
        string? sourceLink = null,
        bool isDeleted = false,
        DateTime? createdAt = null)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO articles (
                id, title, content, summary, reading_time, tags, category_id, author,
                cover_color, cover_url, views, likes, series_id, is_pinned, status,
                created_by_user_id, source_link, created_at, is_del)
            VALUES (
                @Id, @Title, @Content, @Summary, 3, @Tags, @CategoryId, 'tester',
                '#000000', '', @Views, 0, @SeriesId, false, @Status,
                0, @SourceLink, @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                Title = title,
                Content = $"content for {title}",
                Summary = $"summary for {title}",
                Tags = tags ?? [],
                CategoryId = categoryId,
                SeriesId = seriesId,
                Views = views,
                Status = status,
                SourceLink = sourceLink,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                IsDeleted = isDeleted,
            });
    }

    public static Task<int> InsertUserAsync(
        NpgsqlConnection connection,
        long id,
        string username,
        string role,
        bool isActive = true,
        bool isDeleted = false,
        string? passwordHash = "hash",
        string? email = null)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO users (
                id, username, email, password_hash, nickname, avatar_url, role,
                is_active, github_id, last_login_at, created_at, is_del)
            VALUES (
                @Id, @Username, @Email, @PasswordHash, @Nickname, NULL, @Role,
                @IsActive, NULL, NULL, @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                Username = username,
                Email = email ?? $"{username}@example.com",
                PasswordHash = passwordHash,
                Nickname = username,
                Role = role,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = isDeleted,
            });
    }

    public static Task<int> InsertRoleAsync(
        NpgsqlConnection connection,
        long id,
        string code,
        bool isSystem = false,
        bool isDeleted = false)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO roles (id, code, name, description, is_system, created_at, is_del)
            VALUES (@Id, @Code, @Name, @Description, @IsSystem, @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                Code = code,
                Name = code,
                Description = $"{code} role",
                IsSystem = isSystem,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = isDeleted,
            });
    }

    public static Task<int> InsertMenuAsync(
        NpgsqlConnection connection,
        long id,
        string name,
        string path,
        string permissionCode,
        int type,
        int sort = 0,
        bool isDeleted = false)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO menus (id, parent_id, name, path, permission_code, icon, sort, is_hidden, type, created_at, is_del)
            VALUES (@Id, NULL, @Name, @Path, @PermissionCode, NULL, @Sort, false, @Type, @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                Name = name,
                Path = path,
                PermissionCode = permissionCode,
                Sort = sort,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = isDeleted,
            });
    }

    public static Task<int> InsertRoleMenuAsync(
        NpgsqlConnection connection,
        long id,
        long roleId,
        long menuId,
        bool isDeleted = false)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO role_menus (id, role_id, menu_id, created_at, is_del)
            VALUES (@Id, @RoleId, @MenuId, @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                RoleId = roleId,
                MenuId = menuId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = isDeleted,
            });
    }

    public static Task<int> InsertAuditLogAsync(
        NpgsqlConnection connection,
        long id,
        string module,
        string username,
        bool isSuccess,
        DateTime createdAt,
        bool isDeleted = false)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO audit_logs (
                id, user_id, username, module, action, method, path, parameters,
                client_ip, duration_ms, is_success, error_message, trace_id, created_at, is_del)
            VALUES (
                @Id, 1, @Username, @Module, 'test', 'GET', '/api/test', '{}',
                '127.0.0.1', 12, @IsSuccess, NULL, @TraceId, @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                Module = module,
                Username = username,
                IsSuccess = isSuccess,
                TraceId = $"trace-{id}",
                CreatedAt = createdAt,
                IsDeleted = isDeleted,
            });
    }

    public static Task<int> InsertExceptionLogAsync(
        NpgsqlConnection connection,
        long id,
        string exceptionType,
        string message,
        string path,
        DateTime createdAt,
        bool isDeleted = false)
    {
        return connection.ExecuteAsync(
            """
            INSERT INTO exception_logs (
                id, exception_type, message, stack_trace, path, method, query_string,
                request_body, client_ip, user_id, username, trace_id, created_at, is_del)
            VALUES (
                @Id, @ExceptionType, @Message, 'stack trace', @Path, 'GET', NULL,
                NULL, '127.0.0.1', 1, 'tester', @TraceId, @CreatedAt, @IsDeleted);
            """,
            new
            {
                Id = id,
                ExceptionType = exceptionType,
                Message = message,
                Path = path,
                TraceId = $"trace-{id}",
                CreatedAt = createdAt,
                IsDeleted = isDeleted,
            });
    }
}
