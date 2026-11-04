using Identity.Application.Common.Interfaces;
using Identity.Application.Users.Commands;
using Identity.Application.Users.DTOs;
using Identity.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using SharedKernel.Api;
using SharedKernel.Application;
using SharedKernel.Application.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Controllers;

/// <summary>
/// 登录与身份认证控制器
/// </summary>
public class AuthController : BaseApiController
{
    private const string GitHubStateCookieName = "GitHubOAuthState";
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly IGitHubOAuthService _gitHubOAuthService;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR 调度器</param>
    /// <param name="currentUser">当前上下文用户解析器</param>
    /// <param name="gitHubOAuthService">GitHub 开放平台通讯服务</param>
    /// <param name="configuration">全局配置</param>
    public AuthController(
        IMediator mediator,
        ICurrentUser currentUser,
        IGitHubOAuthService gitHubOAuthService,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _gitHubOAuthService = gitHubOAuthService;
        _configuration = configuration;
    }

    /// <summary>
    /// 管理员账号密码登录
    /// </summary>
    /// <param name="request">登录参数（支持用户名或邮箱）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>双 Token 凭据与用户基本信息</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(ApiResult<AuthTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new LoginCommand(request.Account, request.Password, ipAddress);

        var result = await _mediator.Send(command, cancellationToken);
        return Success(result, "登录成功");
    }

    /// <summary>
    /// 使用 RefreshToken 刷新访问令牌（无感续期）
    /// </summary>
    /// <param name="request">刷新令牌参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的双 Token 凭据</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AuthTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new RefreshTokenCommand(request.RefreshToken, ipAddress);

        var result = await _mediator.Send(command, cancellationToken);
        return Success(result, "令牌刷新成功");
    }

    /// <summary>
    /// 撤销刷新令牌（主动退出登录）
    /// </summary>
    /// <param name="request">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("revoke")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAsync([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeTokenCommand(request.RefreshToken), cancellationToken);
        return Success("令牌已成功撤销");
    }

    /// <summary>
    /// 获取当前登录人详细信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户信息</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId <= 0)
        {
            throw new BusinessException("用户尚未登录或登录已失效", 401);
        }

        var result = await _mediator.Send(new GetCurrentUserQuery(_currentUser.UserId), cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取当前登录人被授权的菜单与权限资源
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    [HttpGet("my-menus")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<List<MenuDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMenusAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyMenusQuery(_currentUser.UserId), cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取 GitHub OAuth 授权跳转 URL
    /// </summary>
    /// <returns>GitHub 授权页面 URL</returns>
    [HttpGet("github/url")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<GitHubAuthorizeDto>), StatusCodes.Status200OK)]
    public IActionResult GetGitHubAuthorizeUrl()
    {
        if (!_gitHubOAuthService.IsEnabled)
        {
            throw new BusinessException("系统尚未启用 GitHub 第三方登录功能", 400);
        }

        var state = GenerateSignedState();
        Response.Cookies.Append(GitHubStateCookieName, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/",
            IsEssential = true,
        });

        var url = _gitHubOAuthService.GetAuthorizationUrl(state);
        return Success(new GitHubAuthorizeDto { Url = url, State = state });
    }

    /// <summary>
    /// 使用 GitHub 授权码登录后台
    /// </summary>
    /// <param name="request">授权码请求参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>双 Token 凭据与用户基本信息</returns>
    [HttpPost("github")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResult<AuthTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GitHubLoginAsync([FromBody] GitHubLoginRequest request, CancellationToken cancellationToken)
    {
        if (!_gitHubOAuthService.IsEnabled)
        {
            throw new BusinessException("系统尚未启用 GitHub 第三方登录功能", 400);
        }

        ValidateOAuthState(request.State);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new GitHubLoginCommand(request.Code, ipAddress);

        var result = await _mediator.Send(command, cancellationToken);
        return Success(result, "GitHub 登录成功");
    }

    private void ValidateOAuthState(string state)
    {
        if (!ValidateSignedState(state) ||
            !Request.Cookies.TryGetValue(GitHubStateCookieName, out var cookieState) ||
            !FixedTimeEquals(cookieState, state))
        {
            throw new BusinessException("GitHub 授权状态缺失、不匹配或已过期，请重新发起授权", 400);
        }

        Response.Cookies.Delete(GitHubStateCookieName, new CookieOptions
        {
            Secure = Request.IsHttps,
            SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
        });
    }

    private string GenerateSignedState()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = RandomNumberGenerator.GetHexString(16);
        var payload = $"{timestamp}_{nonce}";
        var secret = _configuration["Identity:OAuthStateSecret"]
            ?? _configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("未配置 Identity:OAuthStateSecret 或 JwtSettings:Secret");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        return $"{payload}_{signature}";
    }

    private bool ValidateSignedState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return false;
        }

        var parts = state.Split('_');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!long.TryParse(parts[0], out var timestamp))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - timestamp > 600 || timestamp - now > 60)
        {
            return false;
        }

        var payload = $"{parts[0]}_{parts[1]}";
        var actualSignature = parts[2];
        var secret = _configuration["Identity:OAuthStateSecret"]
            ?? _configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("未配置 Identity:OAuthStateSecret 或 JwtSettings:Secret");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expectedSignature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

        return FixedTimeEquals(expectedSignature, actualSignature);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    /// <summary>
    /// 更新当前登录人基本资料（昵称、头像、邮箱）
    /// </summary>
    /// <param name="request">资料更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的用户数据</returns>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId <= 0)
        {
            throw new BusinessException("用户尚未登录或登录已失效", 401);
        }

        var command = new UpdateProfileCommand(_currentUser.UserId, request);
        var result = await _mediator.Send(command, cancellationToken);
        return Success(result, "个人资料更新成功");
    }

    /// <summary>
    /// 修改当前登录人密码
    /// </summary>
    /// <param name="request">密码修改参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作状态</returns>
    [HttpPut("password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId <= 0)
        {
            throw new BusinessException("用户尚未登录或登录已失效", 401);
        }

        var command = new ChangePasswordCommand(_currentUser.UserId, request);
        var msg = await _mediator.Send(command, cancellationToken);
        return Success(msg);
    }

    /// <summary>
    /// 绑定 GitHub 账号
    /// </summary>
    /// <param name="request">临时授权码参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的用户信息</returns>
    [HttpPost("github/bind")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BindGithubAsync([FromBody] BindGithubRequest request, CancellationToken cancellationToken)
    {
        if (!_gitHubOAuthService.IsEnabled)
        {
            throw new BusinessException("系统尚未启用 GitHub 账号绑定功能", 400);
        }

        if (!_currentUser.IsAuthenticated || _currentUser.UserId <= 0)
        {
            throw new BusinessException("用户尚未登录或登录已失效", 401);
        }

        ValidateOAuthState(request.State);
        var command = new BindGithubCommand(_currentUser.UserId, request.Code);
        var result = await _mediator.Send(command, cancellationToken);
        return Success(result, "GitHub 账号绑定成功");
    }

    /// <summary>
    /// 解除 GitHub 账号绑定
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的用户信息</returns>
    [HttpDelete("github/unbind")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<CurrentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnbindGithubAsync(CancellationToken cancellationToken)
    {
        if (!_gitHubOAuthService.IsEnabled)
        {
            throw new BusinessException("系统尚未启用 GitHub 账号绑定功能", 400);
        }

        if (!_currentUser.IsAuthenticated || _currentUser.UserId <= 0)
        {
            throw new BusinessException("用户尚未登录或登录已失效", 401);
        }

        var command = new UnbindGithubCommand(_currentUser.UserId);
        var result = await _mediator.Send(command, cancellationToken);
        return Success(result, "GitHub 账号已成功解除绑定");
    }
}
