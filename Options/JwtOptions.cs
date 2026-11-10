using System.ComponentModel.DataAnnotations;

namespace GuangYingTongTu.Api.Options;

public sealed class JwtOptions
{
    [Required]
    public string Issuer { get; set; } = "GuangYingTongTu.Api";

    [Required]
    public string Audience { get; set; } = "GuangYingTongTu.Unity";

    [Required, MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Range(5, 10080)]
    public int ExpirationMinutes { get; set; } = 720;
}
