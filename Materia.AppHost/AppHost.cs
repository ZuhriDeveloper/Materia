var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("materia");

var apiService = builder.AddProject<Projects.Materia_ApiService>("apiservice")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.AddProject<Projects.Materia_WebUi>("webui")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
