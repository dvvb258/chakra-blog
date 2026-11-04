namespace Content.Domain.Enums;

/// <summary>
/// 文章状态枚举
/// </summary>
public enum ArticleStatus
{
    /// <summary>
    /// 草稿（仅作者本人与管理员可见，前台不展示）
    /// </summary>
    Draft = 0,

    /// <summary>
    /// 已发布 / 审核通过（公开前台展示）
    /// </summary>
    Published = 1,

    /// <summary>
    /// 待审核（已提交，等待管理员审核）
    /// </summary>
    PendingReview = 2,

    /// <summary>
    /// 已驳回 / 退回（管理员审核未通过，待作者修改重新提交）
    /// </summary>
    Rejected = 3,
}
