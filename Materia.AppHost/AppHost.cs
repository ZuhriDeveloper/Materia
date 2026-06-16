var builder = DistributedApplication.CreateBuilder(args);

// ── Databases ──────────────────────────────────────────────────────────────────
// Pass the password via Aspire's parameter system so it is used consistently
// in BOTH the container env var (POSTGRES_PASSWORD) AND the injected connection strings.
// Using WithEnvironment("POSTGRES_PASSWORD", ...) alone only sets the container side
// and causes a mismatch with Aspire's generated connection strings.
var pgPassword = builder.AddParameter("postgres-password", "YYYjzk}ppk*CUP.65!X}!~!", secret: true); 

var postgres = builder.AddPostgres("postgres", password: pgPassword)
    .WithDataVolume()
    .WithHostPort(54050)
    .WithLifetime(ContainerLifetime.Persistent);

var materiadb = postgres.AddDatabase("materiadb");

// ── Cache ────────────────────────────────────────────────────────────────────
// Redis backs the PoS product-name autocomplete (cache-aside over the catalog).
var redis = builder.AddRedis("cache")
    .WithLifetime(ContainerLifetime.Persistent);

var webApi = builder.AddProject<Projects.Materia_WebApi>("webapi")
    .WithReference(postgres)
    .WithReference(materiadb)
    .WithReference(redis)
    .WaitFor(materiadb)
    .WaitFor(redis);

builder.AddProject<Projects.Materia_WebUi>("webui")
    .WithReference(webApi)
    .WaitFor(webApi);

builder.Build().Run();
