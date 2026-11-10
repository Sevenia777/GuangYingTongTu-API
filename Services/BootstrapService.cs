using GuangYingTongTu.Api.Data;
using GuangYingTongTu.Api.Models;
using GuangYingTongTu.Api.Options;
using Microsoft.Extensions.Options;

namespace GuangYingTongTu.Api.Services;

public sealed class BootstrapService
{
    private readonly UserRepository _users;
    private readonly BootstrapOptions _options;
    private readonly ILogger<BootstrapService> _logger;

    public BootstrapService(
        UserRepository users,
        IOptions<BootstrapOptions> options,
        ILogger<BootstrapService> logger)
    {
        _users = users;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _users.EnsureIndexesAsync(cancellationToken);
        await EnsureAccountAsync(
            _options.AdminUsername,
            _options.AdminPassword,
            UserRole.admin,
            cancellationToken);
        await EnsureAccountAsync(
            _options.VipUsername,
            _options.VipPassword,
            UserRole.vip,
            cancellationToken);
    }

    private async Task EnsureAccountAsync(
        string username,
        string password,
        UserRole role,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        string normalized = UsernameNormalizer.Normalize(username);
        UserAccount? existing = await _users.FindByUsernameAsync(
            normalized, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation(
                "Bootstrap account already exists: {Username} ({Role})",
                existing.Username,
                existing.Role);
            return;
        }

        CreateUserResult result = await _users.CreateAsync(
            normalized,
            PasswordService.Hash(password),
            role,
            cancellationToken);
        if (result.Success)
        {
            _logger.LogInformation(
                "Bootstrap account created: {Username} ({Role})",
                normalized,
                role);
        }
        else
        {
            _logger.LogWarning(
                "Bootstrap account could not be created: {Username}",
                normalized);
        }
    }
}
