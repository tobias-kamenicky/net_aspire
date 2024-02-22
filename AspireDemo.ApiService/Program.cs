using AspireDemo.ApiService.Configuration;
using AspireDemo.ApiService.Database;
using AspireDemo.ApiService.Domain;
using AspireDemo.Core.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseWolverine(opts =>
{
    opts.PublishMessage<ExternalEvents.UserUpdated>().ToRabbitExchange("external-exchange", exchange =>
    {
        exchange.ExchangeType = ExchangeType.Fanout;
        exchange.BindQueue("external-events-functions-queue", "exchange2functions");
        exchange.BindQueue("external-events-web-queue", "exchange2web");
    });
    // opts.ListenToRabbitQueue("external-events-queue").UseForReplies();

    opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("RabbitMQ")!))
        .UseSenderConnectionOnly()
        .AutoProvision()
        .EnableWolverineControlQueues();
});


builder.AddNpgsqlDbContext<EventStoreDbContext>("PostgresDb", static settings =>
{
    settings.HealthChecks = true;
    settings.Tracing = true;
});
builder.Services.AddScoped<IEventStore, EventStore>();

builder.AddAzureCosmosDB("CosmosDb",
    static settings =>
    {
        settings.Tracing = true;
    },
    static options =>
    {
        options.SerializerOptions = new() {PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase};
    });
builder.Services.AddScoped<IUserProjectionRepository, UserProjectionRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(CosmosConfiguration.DatabaseName);
    var container = database.Database.GetContainer(CosmosConfiguration.Users.ContainerName);
    await container.DeleteContainerAsync();
    await database.Database.CreateContainerIfNotExistsAsync(CosmosConfiguration.Users.ContainerName, CosmosConfiguration.Users.PartitionKeyPath);
}

app.MapPost("/api/users", async (IMessageBus messageBus, [FromBody] CreateUser command, CancellationToken cancellationToken) =>
{
    var result = await messageBus.InvokeAsync<Result<Guid>>(command, cancellationToken);
    return result.Match(
        userId => TypedResults.Created($"/api/users/{userId}"),
        error => error.ToResponse());
});

app.MapPut("/api/users/rename", async (IMessageBus messageBus, [FromBody] RenameUser command, CancellationToken cancellationToken)
    =>
{
    var result = await messageBus.InvokeAsync<Result>(command, cancellationToken);
    return result.Match(
        () => TypedResults.Accepted((string?) null),
        error => error.ToResponse());
});

app.MapPost("/api/users/disable", async (IMessageBus messageBus, [FromBody] DisableUser command, CancellationToken cancellationToken)
    =>
{
    var result = await messageBus.InvokeAsync<Result>(command, cancellationToken);
    return result.Match(
        () => TypedResults.Accepted((string?) null),
        error => error.ToResponse());
});

app.MapGet("/api/users", async (IMessageBus messageBus, CancellationToken cancellationToken)
    => TypedResults.Ok(await messageBus.InvokeAsync<IAsyncEnumerable<UserListItem>>(new ListUsers(), cancellationToken)));

app.MapGet("/api/users/{id:guid}", async (IMessageBus messageBus, [AsParameters] GetUserDetails query, CancellationToken cancellationToken)
    =>
{
    var details = await messageBus.InvokeAsync<UserDetails?>(query, cancellationToken);
    return details is null ? TypedResults.NotFound() : TypedResults.Ok(details) as IResult;
});


app.MapDefaultEndpoints();

await app.RunAsync();

internal static class ResultExtensions
{
    public static IResult ToResponse(this Error error) => error.Type switch
    {
        ErrorType.NotFound => TypedResults.NotFound(error.Message),
        ErrorType.Conflict => TypedResults.Conflict(error.Message),
        ErrorType.ValidationProblem => TypedResults.ValidationProblem(error.Errors ?? new Dictionary<string, string[]>(), error.Message),
        _ => throw new ArgumentException("Unknown error type", nameof(error))
    };
}
