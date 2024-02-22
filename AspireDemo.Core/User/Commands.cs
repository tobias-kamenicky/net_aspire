namespace AspireDemo.Core.User;

public sealed record CreateUser(string Name, string Email) : Command;
public sealed record RenameUser(Guid Id, string Name) : Command;
public sealed record DisableUser(Guid Id) : Command;
