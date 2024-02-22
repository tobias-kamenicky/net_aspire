namespace AspireDemo.ApiService.Database;

public class EventEntity
{
    public long Id { get; set; }
    public Guid AggregateId { get; set; }
    public long Version { get; set; }
    public string EventType { get; set; } = null!;
    public string Data { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}
