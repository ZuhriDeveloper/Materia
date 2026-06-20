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

// ── Mail (development) ─────────────────────────────────────────────────────────
// Mailpit is a local mail catcher: the API sends confirmation / password-reset emails
// to it over SMTP, and you read them in its web UI (http endpoint, port 8025) — no real
// mail account needed. In production the Email__* settings are overridden to point at a
// real SMTP provider instead. The dynamically-assigned SMTP port is injected into the API.
var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithEndpoint(targetPort: 1025, scheme: "tcp", name: "smtp")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "http")
    .WithLifetime(ContainerLifetime.Persistent);

var mailpitSmtp = mailpit.GetEndpoint("smtp");

var webApi = builder.AddProject<Projects.Materia_WebApi>("webapi")
    .WithReference(postgres)
    .WithReference(materiadb)
    .WithReference(redis)
    .WithEnvironment("Email__Host", mailpitSmtp.Property(EndpointProperty.Host))
    .WithEnvironment("Email__Port", mailpitSmtp.Property(EndpointProperty.Port))
    .WaitFor(materiadb)
    .WaitFor(redis)
    .WaitFor(mailpit);

builder.AddProject<Projects.Materia_WebUi>("webui")
    .WithReference(webApi)
    .WaitFor(webApi);

builder.Build().Run();
