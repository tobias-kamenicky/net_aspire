var builder = DistributedApplication.CreateBuilder(args);

var postgresDb = builder.AddPostgres("Postgress")
    .AddDatabase("PostgresDb");

// Currently does not ignore certificate so difficult to use a container
var cosmosDb = builder.AddAzureCosmosDB("CosmosDb");

var rabbitMq = builder.AddRabbitMQ("RabbitMQ");

var apiService = builder
    .AddProject<Projects.AspireDemo_ApiService>("apiservice")
    .WithReference(postgresDb)
    .WithReference(cosmosDb)
    .WithReference(rabbitMq);

builder.AddProject<Projects.AspireDemo_Functions>("functions")
    .WithReference(apiService)
    .WithReference(rabbitMq);

var webService = builder.AddProject<Projects.AspireDemo_Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(rabbitMq);

await builder.Build().RunAsync();
