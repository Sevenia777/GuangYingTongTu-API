using System.ComponentModel.DataAnnotations;

namespace GuangYingTongTu.Api.Models;

public sealed class LoginRequest
{
    [Required, StringLength(32, MinimumLength = 4)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    [Required, RegularExpression("^[A-Za-z0-9_]{4,32}$")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateAccountRequest
{
    [Required, RegularExpression("^[A-Za-z0-9_]{4,32}$")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }
}

public sealed class ResetPasswordRequest
{
    [Required, StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public sealed class UserResponse
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public DateTime CreateTime { get; init; }
}

public sealed class LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserResponse User { get; init; } = new();
}

public sealed class UserListResponse
{
    public IReadOnlyList<UserResponse> Users { get; init; } = Array.Empty<UserResponse>();
    public long Total { get; init; }
}

public sealed class ErrorResponse
{
    public string Message { get; init; } = string.Empty;
}
