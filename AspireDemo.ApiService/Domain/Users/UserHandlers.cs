using AspireDemo.ApiService.Database;
using AspireDemo.Core.User;
using Wolverine;

namespace AspireDemo.ApiService.Domain.Users;

public class UserHandlers(IEventStore eventStore, IUserProjectionRepository userRepository) : IWolverineHandler
{
    public async Task<Result<Guid>> HandleAsync(CreateUser command, CancellationToken cancellationToken = default)
    {
        var result = User.Create(command.Name, command.Email);

        return await result.IfOk(async user =>
        {
            await eventStore.SaveChanges(user, cancellationToken);
            return user.Id;
        });
    }

    public async Task<Result> HandleAsync(RenameUser command, CancellationToken cancellationToken = default)
    {
        var user = await eventStore.Load<User>(command.Id, cancellationToken: cancellationToken);
        if (user is null) return new Error(ErrorType.NotFound, $"User with Id '{command.Id}' not found");

        var result = user.Rename(command.Name);

        return await result.IfOk(_ => eventStore.SaveChanges(user, cancellationToken));
    }

    public async Task<Result> HandleAsync(DisableUser command, CancellationToken cancellationToken = default)
    {
        var user = await eventStore.Load<User>(command.Id, cancellationToken: cancellationToken);
        if (user is null) return new Error(ErrorType.NotFound, $"User with Id '{command.Id}' not found");

        var result = user.Disable();

        return await result.IfOk(_ => eventStore.SaveChanges(user, cancellationToken));
    }

    public IAsyncEnumerable<UserListItem> HandleAsync(ListUsers command, CancellationToken cancellationToken = default)
        => userRepository.GetAllAsync<UserListItem>(cancellationToken);

    public Task<UserDetails?> HandleAsync(GetUserDetails command, CancellationToken cancellationToken = default)
        => userRepository.GetAsync<UserDetails>(command.Id, cancellationToken);
}
