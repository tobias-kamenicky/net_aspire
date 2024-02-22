namespace AspireDemo.ApiService.Domain;

public abstract record Event
{
    public long? Version { get; init; }
};

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }
    public long Version { get; private set; }
    private readonly List<Event> _events = [];

    public IReadOnlyCollection<Event> GetUncommittedChanges() => _events.AsReadOnly();
    public void MarkChangesAsCommitted()
    {
        Version = _events.LastOrDefault()?.Version ?? Version;
        _events.Clear();
    }

    protected void ApplyChange<T>(T @event) where T : Event
    {
        if (@event.Version is null)
        {
            var lastVersion = _events.LastOrDefault()?.Version ?? Version;
            @event = @event with {Version = lastVersion + 1};
            _events.Add(@event);
        }
        Apply(@event);
    }

    protected abstract void Apply(Event @event);

    public void LoadFromHistory(IEnumerable<Event> history)
    {
        foreach (var @event in history)
        {
            if (@event.Version is null) throw new InvalidOperationException("Event must have a version");

            ApplyChange(@event);
            Version = @event.Version.Value;
        }
    }
}
