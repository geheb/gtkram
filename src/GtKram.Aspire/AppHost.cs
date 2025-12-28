var builder = DistributedApplication.CreateBuilder(args);

var superUserSecret = builder.AddParameter("SuperUser", true);

var sqliteDir = new DirectoryInfo("/data/gtkram/sqlite");
if (!sqliteDir.Exists)
{
    sqliteDir.Create();
}

var mailpitDir = new DirectoryInfo("/data/gtkram/mailpit");
if (!mailpitDir.Exists)
{
    mailpitDir.Create();
}

const string databaseFileName = "gtkram.sqlite";

var sqlite = builder.AddSqlite("SQLite", sqliteDir.FullName, databaseFileName)
    .WithSqliteWeb(c => c.WithArgs(databaseFileName));

var mailpit = builder.AddMailPit("mailpit")
    .WithDataBindMount(mailpitDir.FullName);

builder.AddProject<Projects.GtKram_WebApp>("webapp")
    .WithReference(sqlite)
    .WithReference(mailpit)
    .WithHttpHealthCheck("/healthz")
    .WithEnvironment(c =>
    {
        var endpoint = mailpit.GetEndpoint("smtp");
        c.EnvironmentVariables["SMTP__SERVER"] = endpoint.Host;
        c.EnvironmentVariables["SMTP__PORT"] = endpoint.Port;

        c.EnvironmentVariables["BOOTSTRAP__SUPERUSER__PASSWORD"] = superUserSecret;
    });

using var app = builder.Build();

await app.RunAsync();
