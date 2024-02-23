using AspireDemo.ApiService.Database;
using AspireDemo.ApiService.Domain;
using AspireDemo.Core;
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
        settings.IgnoreEmulatorCertificate = true;
        settings.Tracing = true;
    },
    static options => { options.SerializerOptions = new() {PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase}; }
);
builder.Services.AddScoped<IUserProjectionRepository, UserProjectionRepository>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // await using var scope = app.Services.CreateAsyncScope();
    // var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
    // await dbContext.Database.EnsureCreatedAsync();

    // var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    // var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(CosmosConfiguration.DatabaseName);
    // var container = database.Database.GetContainer(CosmosConfiguration.Users.ContainerName);
    // // await container.DeleteContainerAsync();
    // await database.Database.CreateContainerIfNotExistsAsync(CosmosConfiguration.Users.ContainerName, CosmosConfiguration.Users.PartitionKeyPath);
}

app.MapGroup("/api/users").MapUserApi();

app.MapDefaultEndpoints();

await app.RunAsync();

internal static class UserApi
{
    public static void MapUserApi(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (IMessageBus messageBus, [FromBody] CreateUser command, CancellationToken cancellationToken)
            => await HandleCommand(messageBus, command, cancellationToken));

        group.MapPut("/rename", async (IMessageBus messageBus, [FromBody] RenameUser command, CancellationToken cancellationToken)
            => await HandleCommand(messageBus, command, cancellationToken));

        group.MapPost("/disable", async (IMessageBus messageBus, [FromBody] DisableUser command, CancellationToken cancellationToken)
            => await HandleCommand(messageBus, command, cancellationToken));

        group.MapGet("/", async (IMessageBus messageBus, CancellationToken cancellationToken)
            => TypedResults.Ok(await messageBus.InvokeAsync<IAsyncEnumerable<UserListItem>>(new ListUsers(), cancellationToken)));

        group.MapGet("/{id:guid}", async (IMessageBus messageBus, [AsParameters] GetUserDetails query, CancellationToken cancellationToken)
            =>
        {
            var details = await messageBus.InvokeAsync<UserDetails?>(query, cancellationToken);
            return details is null ? TypedResults.NotFound() : TypedResults.Ok(details) as IResult;
        });
    }

    private static async Task<IResult> HandleCommand<T>(IMessageBus messageBus, T command, CancellationToken cancellationToken)
        where T : Command
    {
        var result = await messageBus.InvokeAsync<Result>(command, cancellationToken);
        return result.Match(
            () => TypedResults.Accepted((string?) null),
            error => error.ToResponse());
    }
}

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
