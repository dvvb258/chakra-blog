using Content.Application.Categories.DTOs;
using Content.Application.Common;
using Content.Domain.Enums;
using Dapper;
using SharedKernel.Application.CQRS;

namespace Content.Application.Categories.Queries;

/// <summary>
/// 获取分类列表查询处理器
/// </summary>
public sealed class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IContentDbConnectionFactory _connectionFactory;

    public GetCategoriesQueryHandler(IContentDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        // 左关联查询分类，并统计每个分类下处于 Published(1) 状态且未软删除的文章数量
        const string sql = """
            SELECT 
                c.id AS Id,
                c.name AS Name,
                c.description AS Description,
                c.icon AS Icon,
                c.sort AS Sort,
                COUNT(a.id) FILTER (WHERE a.is_del = false AND a.status = @PublishedStatus) AS ArticleCount
            FROM categories c
            LEFT JOIN articles a ON a.category_id = c.id
            WHERE c.is_del = false
            GROUP BY c.id, c.name, c.description, c.icon, c.sort
            ORDER BY c.sort ASC, c.id ASC
            """;

        var command = new CommandDefinition(
            sql,
            new { PublishedStatus = (int)ArticleStatus.Published },
            cancellationToken: cancellationToken);

        var list = await connection.QueryAsync<CategoryDto>(command);
        return list.ToList();
    }
}
