namespace AspireDemo.Core.User;


public interface IUserItem
{
    public Guid Id { get; }
    public long Version { get; }
}

public sealed record UserDetails(Guid Id, string Name, string Email, bool Enabled, long Version) : Response, IUserItem;
public sealed record UserListItem(Guid Id, string Name, bool Enabled, long Version) : Response, IUserItem;
