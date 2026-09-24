var builder = DistributedApplication.CreateBuilder(args);

var superUserSecret = builder.AddParameter("gtkram-superuser", true);
var pgSecret = builder.AddParameter("gtkram-pguser", true);

var postgres = builder
    .AddPostgres("postgres", password: pgSecret)
    .WithImageTag("18-alpine")
    .WithContainerName("postgres")
    .WithDataVolume("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(containerName: "pgadmin");

var db = postgres.AddDatabase("gtkram");

var mailpit = builder.AddMailPit("mailpit")
    .WithContainerName("mailpit")
    .WithDataVolume("mailpit")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.GtKram_WebApp>("webapp")
    .WithReference(db)
    .WithReference(mailpit)
    .WithHttpHealthCheck("/healthz")
    .WithEnvironment(c =>
    {
        var endpoint = mailpit.GetEndpoint("smtp");
        c.EnvironmentVariables["SMTP__SERVER"] = endpoint.Host;
        c.EnvironmentVariables["SMTP__PORT"] = endpoint.Port;

        c.EnvironmentVariables["BOOTSTRAP__SUPERUSER__PASSWORD"] = superUserSecret;
    })
    .WaitFor(postgres)
    .WaitFor(mailpit);

using var app = builder.Build();

await app.RunAsync();
