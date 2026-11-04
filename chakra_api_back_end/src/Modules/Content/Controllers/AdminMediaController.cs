using Content.Application.Common;
using Content.Application.Media.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api;
using SharedKernel.Application;
using SharedKernel.Application.Exceptions;

namespace Content.Controllers;

/// <summary>
/// 管理端媒体与图床管理控制器（对接通用 S3 兼容对象存储）
/// </summary>
[Authorize]
[Route("api/admin/media")]
public class AdminMediaController : BaseApiController
{
    private readonly IStorageService _storageService;

    // 允许上传的图片 MIME 类型与后缀白名单（排除 svg 防止存储型 XSS 脚本执行）
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"
    };

    // 单个文件最大大小限制：15MB
    private const long MaxFileSize = 15 * 1024 * 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminMediaController"/> class.
    /// </summary>
    /// <param name="storageService">对象存储服务</param>
    public AdminMediaController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// 上传媒体图片文件至 S3 兼容对象存储
    /// </summary>
    /// <param name="file">上传的图片文件（multipart/form-data）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传后的公共 URL 与元数据</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<UploadMediaResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessException("请选择需要上传的文件", 400);
        }

        if (file.Length > MaxFileSize)
        {
            throw new BusinessException($"文件大小不能超过 {MaxFileSize / (1024 * 1024)}MB", 400);
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new BusinessException($"仅支持上传图片类型文件（{string.Join(", ", AllowedExtensions)}）", 400);
        }

        using var stream = file.OpenReadStream();
        if (!IsValidImageHeader(stream))
        {
            throw new BusinessException("文件头校验失败，上传文件非有效合法图片", 400);
        }

        var url = await _storageService.UploadAsync(stream, file.FileName, file.ContentType, cancellationToken);

        var result = new UploadMediaResultDto
        {
            Url = url,
            FileName = file.FileName,
            Size = file.Length,
        };

        return Success(result, "图片上传成功");
    }

    private static bool IsValidImageHeader(Stream stream)
    {
        if (stream.Length < 12)
        {
            return false;
        }

        var header = new byte[12];
        var originalPosition = stream.Position;
        try
        {
            var bytesRead = stream.Read(header, 0, 12);
            if (bytesRead < 12)
            {
                return false;
            }

            // JPEG (FF D8 FF)
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return true;
            }

            // PNG (89 50 4E 47 0D 0A 1A 0A)
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            {
                return true;
            }

            // GIF (GIF87a / GIF89a: 47 49 46 38)
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
            {
                return true;
            }

            // BMP (42 4D)
            if (header[0] == 0x42 && header[1] == 0x4D)
            {
                return true;
            }

            // WebP (RIFF....WEBP: header[0..3] == "RIFF" && header[8..11] == "WEBP")
            if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            {
                return true;
            }

            return false;
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }
}
