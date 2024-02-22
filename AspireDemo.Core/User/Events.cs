namespace AspireDemo.Core.User;

public abstract record ExternalEvent;

public static class ExternalEvents
{
    public record UserUpdated(Guid UserId) : ExternalEvent;
}
