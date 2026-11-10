using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GuangYingTongTu.Api.Models;

[BsonIgnoreExtraElements]
public sealed class UserAccount
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("password")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("role")]
    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; }

    [BsonElement("create_time")]
    public DateTime CreateTime { get; set; }
}
