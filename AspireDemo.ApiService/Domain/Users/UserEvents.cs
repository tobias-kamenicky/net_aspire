namespace AspireDemo.ApiService.Domain.Users;

public sealed record UserCreated(Guid Id, string Name, string Email) : Event;
public sealed record UserRenamed(Guid Id, string Name) : Event;
public sealed record UserDisabled(Guid Id) : Event;
