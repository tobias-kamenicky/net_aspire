using AspireDemo.Core.User;
using Wolverine;

namespace AspireDemo.Functions;

public class UserHandlers(ILogger<UserHandlers> logger) : IWolverineHandler
{
    public async Task HandleAsync(ExternalEvents.UserUpdated @event)
    {
        logger.LogInformation("Processed event {Event}", @event);
    }
}
