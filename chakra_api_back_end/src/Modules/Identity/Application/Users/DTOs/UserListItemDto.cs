using SharedKernel.Constants;

namespace Identity.Application.Users.DTOs;

/// <summary>
/// 用户列表项数据传输对象
/// </summary>
public record UserListItemDto
{
    public long Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = RoleNames.Editor;
    public bool IsActive { get; init; }
    public bool HasPassword { get; init; }
    public string? GithubId { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
