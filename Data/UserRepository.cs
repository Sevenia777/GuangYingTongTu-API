using GuangYingTongTu.Api.Models;
using GuangYingTongTu.Api.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GuangYingTongTu.Api.Data;

public sealed class UserRepository
{
    private readonly IMongoCollection<UserAccount> _users;

    public UserRepository(
        IMongoClient client,
        IOptions<MongoOptions> options)
    {
        MongoOptions value = options.Value;
        IMongoDatabase database = client.GetDatabase(value.DatabaseName);
        _users = database.GetCollection<UserAccount>(value.UsersCollectionName);
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var keys = Builders<UserAccount>.IndexKeys.Ascending(x => x.Username);
        var model = new CreateIndexModel<UserAccount>(
            keys,
            new CreateIndexOptions
            {
                Unique = true,
                Name = "ux_users_username"
            });
        await _users.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }

    public async Task<UserAccount?> FindByUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default)
    {
        return await _users
            .Find(x => x.Username == normalizedUsername)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserAccount?> FindByIdAsync(
        ObjectId id,
        CancellationToken cancellationToken = default)
    {
        return await _users
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserResponse>> GetUsersAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        List<UserAccount> users = await _users
            .Find(Builders<UserAccount>.Filter.Empty)
            .SortByDescending(x => x.CreateTime)
            .Limit(Math.Clamp(limit, 1, 500))
            .ToListAsync(cancellationToken);
        return users.Select(ToResponse).ToArray();
    }

    public Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return _users.CountDocumentsAsync(
            Builders<UserAccount>.Filter.Empty,
            cancellationToken: cancellationToken);
    }

    public async Task<CreateUserResult> CreateAsync(
        string normalizedUsername,
        string passwordHash,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var user = new UserAccount
        {
            Id = ObjectId.GenerateNewId(),
            Username = normalizedUsername,
            PasswordHash = passwordHash,
            Role = role,
            CreateTime = DateTime.UtcNow
        };

        try
        {
            await _users.InsertOneAsync(user, cancellationToken: cancellationToken);
            return CreateUserResult.Created(ToResponse(user));
        }
        catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return CreateUserResult.DuplicateResult();
        }
    }

    public async Task<bool> UpdatePasswordAsync(
        string normalizedUsername,
        string passwordHash,
        CancellationToken cancellationToken = default)
    {
        UpdateResult result = await _users.UpdateOneAsync(
            x => x.Username == normalizedUsername,
            Builders<UserAccount>.Update.Set(
                x => x.PasswordHash, passwordHash),
            cancellationToken: cancellationToken);
        return result.MatchedCount > 0;
    }

    public static UserResponse ToResponse(UserAccount user)
    {
        return new UserResponse
        {
            Id = user.Id.ToString(),
            Username = user.Username,
            Role = user.Role,
            CreateTime = user.CreateTime
        };
    }
}

public readonly record struct CreateUserResult(
    bool Success,
    bool Duplicate,
    UserResponse? User)
{
    public static CreateUserResult Created(UserResponse user) =>
        new(true, false, user);

    public static CreateUserResult DuplicateResult() =>
        new(false, true, null);
}
