using AspireDemo.Core.User;
using AspireDemo.Web.Components;
using AspireDemo.Web.Hubs;
using Microsoft.AspNetCore.ResponseCompression;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseWolverine(x =>
{
    x.ListenToRabbitQueue("external-events-web-queue").UseForReplies();
    x.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!))
        .UseListenerConnectionOnly()
        .AutoProvision()
        .EnableWolverineControlQueues();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR(c => c.EnableDetailedErrors = true);
builder.Services.AddOutputCache();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

builder.Services.AddHttpClient<UserClient>(client => client.BaseAddress = new("http://apiservice"));

var app = builder.Build();

app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();

app.UseAntiforgery();

app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<ExternalEventsHub>("/hubs/events");

app.MapDefaultEndpoints();

await app.RunAsync();
