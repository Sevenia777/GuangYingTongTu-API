using GuangYingTongTu.Api.Data;
using GuangYingTongTu.Api.Models;
using GuangYingTongTu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuangYingTongTu.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly UserRepository _users;

    public AdminUsersController(UserRepository users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<UserListResponse>> List(
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserResponse> users = await _users.GetUsersAsync(
            limit, cancellationToken);
        long total = await _users.CountAsync(cancellationToken);
        return Ok(new UserListResponse { Users = users, Total = total });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        string username = UsernameNormalizer.Normalize(request.Username);
        CreateUserResult created = await _users.CreateAsync(
            username,
            PasswordService.Hash(request.Password),
            request.Role,
            cancellationToken);
        if (!created.Success)
            return Conflict(new ErrorResponse { Message = "用户名已存在" });

        return CreatedAtAction(
            nameof(List),
            new { },
            created.User);
    }

    [HttpPut("{username}/password")]
    public async Task<IActionResult> ResetPassword(
        string username,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        string normalized = UsernameNormalizer.Normalize(username);
        bool updated = await _users.UpdatePasswordAsync(
            normalized,
            PasswordService.Hash(request.Password),
            cancellationToken);
        return updated
            ? NoContent()
            : NotFound(new ErrorResponse { Message = "账号不存在" });
    }
}
