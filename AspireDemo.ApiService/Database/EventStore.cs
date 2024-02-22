using System.Reflection;
using System.Text.Json;
using AspireDemo.ApiService.Domain;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace AspireDemo.ApiService.Database;

public interface IEventStore
{
    Task SaveChanges<T>(T aggregateRoot, CancellationToken cancellationToken = default) where T : AggregateRoot;
    Task<T?> Load<T>(Guid id, long? version = null, CancellationToken cancellationToken = default) where T : AggregateRoot, new();
}

public class EventStore(EventStoreDbContext dbContext, IMessageBus messageBus) : IEventStore
{
    private static readonly IReadOnlyDictionary<string, Type> _eventTypes = Assembly.GetAssembly(typeof(Event))!
        .GetTypes()
        .Where(x => x.IsSubclassOf(typeof(Event)))
        .ToDictionary(x => x.Name);

    public async Task SaveChanges<T>(T aggregateRoot, CancellationToken cancellationToken = default) where T : AggregateRoot
    {
        var currentVersion = await dbContext.Events
            .Where(x => x.AggregateId == aggregateRoot.Id)
            .OrderByDescending(x => x.Version)
            .Select(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var changes = aggregateRoot.GetUncommittedChanges();

        if (currentVersion != aggregateRoot.Version)
        {
            throw new InvalidOperationException("Concurrency exception");
        }

        foreach (var change in changes)
        {
            var entity = new EventEntity
            {
                AggregateId = aggregateRoot.Id,
                Version = change.Version ?? throw new InvalidOperationException("Missing event version"),
                EventType = change.GetType().Name,
                Timestamp = DateTime.UtcNow,
                Data = JsonSerializer.Serialize(change as dynamic)
            };

            dbContext.Events.Add(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var change in changes)
        {
            await messageBus.PublishAsync(change);
        }
        aggregateRoot.MarkChangesAsCommitted();
    }

    public async Task<T?> Load<T>(Guid id, long? version = null, CancellationToken cancellationToken = default) where T : AggregateRoot, new()
    {
        var events = await dbContext.Events
            .Where(x => x.AggregateId == id)
            .Where(x => version == null || x.Version <= version)
            .OrderBy(x => x.Version)
            .ToListAsync(cancellationToken);

        if (events.Count == 0) return null;

        var parsedEvents = events
            .Select(x => (JsonSerializer.Deserialize(x.Data, _eventTypes[x.EventType]) as Event)! with
            {
                Version = x.Version
            });

        var aggregate = new T();
        aggregate.LoadFromHistory(parsedEvents);

        return aggregate;
    }
}
