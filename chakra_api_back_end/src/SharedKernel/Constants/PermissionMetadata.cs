namespace SharedKernel.Constants;

/// <summary>
/// 权限项元数据说明对象
/// </summary>
/// <param name="Code">权限代码</param>
/// <param name="Name">权限显示名称</param>
/// <param name="Module">所属模块分组</param>
/// <param name="Description">权限详细描述</param>
public sealed record PermissionMetadata(
    string Code,
    string Name,
    string Module,
    string? Description = null);
