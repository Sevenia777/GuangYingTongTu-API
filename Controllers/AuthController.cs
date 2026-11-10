using GuangYingTongTu.Api.Data;
using GuangYingTongTu.Api.Models;
using GuangYingTongTu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MongoDB.Bson;

namespace GuangYingTongTu.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserRepository _users;
    private readonly TokenService _tokens;

    public AuthController(UserRepository users, TokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        string username = UsernameNormalizer.Normalize(request.Username);
        UserAccount? user = await _users.FindByUsernameAsync(
            username, cancellationToken);
        if (user == null || !PasswordService.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ErrorResponse { Message = "用户名或密码错误" });

        (string token, DateTime expiresAt) = _tokens.Create(user);
        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = UserRepository.ToResponse(user)
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        string username = UsernameNormalizer.Normalize(request.Username);
        CreateUserResult created = await _users.CreateAsync(
            username,
            PasswordService.Hash(request.Password),
            UserRole.player,
            cancellationToken);
        if (!created.Success)
            return Conflict(new ErrorResponse { Message = "用户名已存在" });

        UserAccount user = new()
        {
            Id = ObjectId.Parse(created.User!.Id),
            Username = created.User.Username,
            Role = created.User.Role,
            CreateTime = created.User.CreateTime
        };
        (string token, DateTime expiresAt) = _tokens.Create(user);
        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = created.User
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        string? id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!ObjectId.TryParse(id, out ObjectId userId))
            return Unauthorized(new ErrorResponse { Message = "无效的登录凭证" });

        UserAccount? user = await _users.FindByIdAsync(userId, cancellationToken);
        return user == null
            ? Unauthorized(new ErrorResponse { Message = "账号不存在" })
            : Ok(UserRepository.ToResponse(user));
    }
}
