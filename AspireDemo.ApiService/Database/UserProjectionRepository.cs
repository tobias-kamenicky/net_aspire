using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AspireDemo.ApiService.Configuration;
using AspireDemo.Core.User;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using static AspireDemo.ApiService.Database.PartitionKeyExtensions;

namespace AspireDemo.ApiService.Database;

public interface IUserProjectionRepository
{
    Task<T> UpsertAsync<T>(T item, CancellationToken cancellationToken = default) where T : IUserItem;
    Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : IUserItem;
    IAsyncEnumerable<T> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : IUserItem;
}

public class UserProjectionRepository(CosmosClient client) : IUserProjectionRepository
{
    private readonly Container _container = client.GetContainer(CosmosConfiguration.DatabaseName, CosmosConfiguration.Users.ContainerName);

    public async Task<T> UpsertAsync<T>(T item, CancellationToken cancellationToken = default) where T : IUserItem
    {
        var entity = new CosmosDocument<T>
        {
            Id = item.Id.ToString(),
            PartitionKey = CreatePartitionKey<T>(item.Id),
            Data = item
        };

        var response = await _container.UpsertItemAsync(entity, new(entity.PartitionKey), cancellationToken: cancellationToken);
        if (response.StatusCode is not HttpStatusCode.OK and not HttpStatusCode.Created)
        {
            throw new InvalidOperationException("Failed to create entity of type " + typeof(T).Name);
        }

        return response.Resource.Data;
    }

    public async Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : IUserItem
    {
        using var response = await _container.ReadItemStreamAsync(id.ToString(), new(CreatePartitionKey<T>(id)), cancellationToken: cancellationToken);
        return response.StatusCode is HttpStatusCode.NotFound
            ? default
            : JsonSerializer.Deserialize<CosmosDocument<T>>(response.Content, CosmosConfiguration.SerializerOptions)!.Data;
    }

    public async IAsyncEnumerable<T> GetAllAsync<T>([EnumeratorCancellation] CancellationToken cancellationToken = default) where T : IUserItem
    {
        var iterator = _container
            .GetItemLinqQueryable<CosmosDocument<T>>()
            .Where(x => x.EntityType == typeof(T).Name)
            .ToFeedIterator();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                yield return item.Data;
            }
        }
    }
}
public static class PartitionKeyExtensions {
    public static string CreatePartitionKey<T>(Guid id) where T : IUserItem => typeof(T).Name + id;
}

public class CosmosDocument<T>
{
    public required string Id { get; init; }
    public required string PartitionKey { get; init; }
    public string EntityType => typeof(T).Name;
    public required T Data { get; init; }
}
