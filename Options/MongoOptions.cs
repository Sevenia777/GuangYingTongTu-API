using System.ComponentModel.DataAnnotations;

namespace GuangYingTongTu.Api.Options;

public sealed class MongoOptions
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = "GuangYingTongTu";

    [Required]
    public string UsersCollectionName { get; set; } = "users";
}
