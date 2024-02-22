using AspireDemo.ApiService.Database;
using AspireDemo.Core.User;
using JetBrains.Annotations;
using Wolverine;

namespace AspireDemo.ApiService.Domain.Users;

[UsedImplicitly]
public class UserListItemHandlers(IUserProjectionRepository repository) : IWolverineHandler
{
    public async Task HandleAsync(UserCreated @event, CancellationToken cancellationToken = default)
    {
        var user = new UserListItem(@event.Id, @event.Name, true, @event.Version!.Value);
        await repository.UpsertAsync(user, cancellationToken);
    }

    public async Task HandleAsync(UserRenamed @event, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetAsync<UserListItem>(@event.Id, cancellationToken);
        if (user is null || user.Version >= @event.Version) return;

        user = user with {Name = @event.Name, Version = @event.Version!.Value};
        await repository.UpsertAsync(user, cancellationToken);
    }

    public async Task HandleAsync(UserDisabled @event, CancellationToken cancellationToken = default)
    {
        var user = await repository.GetAsync<UserListItem>(@event.Id, cancellationToken);
        if (user is null || user.Version >= @event.Version) return;

        user = user with {Enabled = false, Version = @event.Version!.Value };
        await repository.UpsertAsync(user, cancellationToken);
    }
}

public class UserDetailsHandlers(IUserProjectionRepository repository, IMessageBus messageBus) : IWolverineHandler
{
    public async Task HandleAsync(UserCreated @event, CancellationToken cancellationToken = default)
    {
        var userDetails = new UserDetails(@event.Id, @event.Name, @event.Email, true, @event.Version!.Value);
        await repository.UpsertAsync(userDetails, cancellationToken);
        await messageBus.PublishAsync(new ExternalEvents.UserUpdated(userDetails.Id));
    }

    public async Task HandleAsync(UserRenamed @event, CancellationToken cancellationToken = default)
    {
        var userDetails = await repository.GetAsync<UserDetails>(@event.Id, cancellationToken);
        if (userDetails is null || userDetails.Version >= @event.Version) return;

        userDetails = userDetails with {Name = @event.Name, Version = @event.Version!.Value};
        await repository.UpsertAsync(userDetails, cancellationToken);
        await messageBus.PublishAsync(new ExternalEvents.UserUpdated(userDetails.Id));
    }

    public async Task HandleAsync(UserDisabled @event, CancellationToken cancellationToken = default)
    {
        var userDetails = await repository.GetAsync<UserDetails>(@event.Id, cancellationToken);
        if (userDetails is null || userDetails.Version >= @event.Version) return;

        userDetails = userDetails with {Enabled = false, Version = @event.Version!.Value};
        await repository.UpsertAsync(userDetails, cancellationToken);
        await messageBus.PublishAsync(new ExternalEvents.UserUpdated(userDetails.Id));
    }
}
