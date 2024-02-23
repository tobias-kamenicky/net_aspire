using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Host.UseWolverine(x =>
{
    x.ListenToRabbitQueue("external-events-functions-queue").UseForReplies();
    x.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!))
        .UseListenerConnectionOnly()
        .AutoProvision()
        .EnableWolverineControlQueues();
});

var app = builder.Build();

await Task.Delay(TimeSpan.FromSeconds(2));

await app.RunAsync();
