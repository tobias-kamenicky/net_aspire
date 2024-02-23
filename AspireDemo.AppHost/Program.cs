var builder = DistributedApplication.CreateBuilder(args);

//dotnet user-secrets init
//dotnet user-secrets set postgrespw test123"
//dotnet user-secrets set rabbitpw test321"
var postgrespw = builder.Configuration["postgrespw"];
var rabbitpw = builder.Configuration["rabbitpw"];

var postgresDb = builder
    // .AddPostgres("Postgres") // enough for not persistent storage
    // Port needed only for db explorer
    .AddPostgresContainer("Postgress", port: 58503, password: postgrespw)
    .WithVolumeMount("VolumeMount.postgres.data", "/var/lib/postgresql/data", VolumeMountType.Named)
    .AddDatabase("PostgresDb");

// Currently does not ignore certificate so difficult to use a container
// This will work in Preview 4
// var cosmosDb = builder.AddAzureCosmosDB("Cosmos").UseEmulator().AddDatabase("CosmosDb");
var cosmosDb = builder.AddAzureCosmosDB("CosmosDb");

var rabbitMq = builder
    // .AddRabbitMQ("RabbitMQ"); // enough for not persistent storage
    .AddRabbitMQContainer("RabbitMQ", password: rabbitpw)
    .WithEnvironment("NODENAME", "rabbit@localhost")
    .WithVolumeMount("VolumeMount.rabbitmq.data", "/var/lib/rabbitmq", VolumeMountType.Named)
    ;

var apiService = builder
    .AddProject<Projects.AspireDemo_ApiService>("apiservice")
    .WithReference(postgresDb)
    .WithReference(cosmosDb)
    .WithReference(rabbitMq)
    .WithReplicas(2);

builder.AddProject<Projects.AspireDemo_Functions>("functions")
    .WithReference(rabbitMq);

var webService = builder.AddProject<Projects.AspireDemo_Web>("webfrontend")
    .WithReference(apiService)
    .WithReference(rabbitMq);

var app = builder.Build();

// Some projects may fail, because the images are spinning up, and I haven't configured more tolerant Retry policy on various clients
await app.RunAsync();
