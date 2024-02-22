namespace AspireDemo.Core.User;

public sealed record GetUserDetails(Guid Id) : Query;
public sealed record ListUsers : Query;
