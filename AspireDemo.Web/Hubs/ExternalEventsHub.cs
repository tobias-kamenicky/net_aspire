using AspireDemo.Core.User;
using Microsoft.AspNetCore.SignalR;

namespace AspireDemo.Web.Hubs;

public interface IExternalEventsClient
{
    Task ReceiveEvent(ExternalEvent externalEvent);
}

public class ExternalEventsHub : Hub<IExternalEventsClient>
{
    // public async Task SendEvent(ExternalEvent externalEvent)
    // {
    //     await Clients.All.ReceiveEvent(externalEvent);
    // }
}
