using System.Text.Json;

namespace AspireDemo.ApiService.Configuration;

public static class CosmosConfiguration
{
    public const string DatabaseName = "aspire-demo";

    public static class Users
    {
        public const string ContainerName = "users";
        public const string PartitionKeyPath = "/partitionKey";
    }

    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
