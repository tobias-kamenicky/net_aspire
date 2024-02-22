using AspireDemo.Core.User;
using AspireDemo.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Wolverine;

namespace AspireDemo.Web.Handlers;

public class UserHandlers(IHubContext<ExternalEventsHub, IExternalEventsClient> hubContext) : IWolverineHandler
{
    public async Task HandleAsync(ExternalEvents.UserUpdated @event) => await hubContext.Clients.All.ReceiveEvent(@event);
}
