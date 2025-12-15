using GtKram.Application;
using GtKram.Infrastructure;
using GtKram.WebApp.Bindings;
using GtKram.WebApp.Filters;
using GtKram.WebApp.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System.Net;

void ConfigureApp(WebApplicationBuilder builder)
{
    builder.WebHost.ConfigureKestrel(o =>
    {
        o.AddServerHeader = false;
        o.AllowResponseHeaderCompression = false;
    });

    var services = builder.Services;
    var configuration = builder.Configuration;

    services.AddHealthChecks();
    services.AddSerilog();
    services.AddPersistence(configuration);
    services.AddAuth(configuration, Policies.TwoFactorAuth);

    services.AddControllers();
    services.AddRazorPages()
        .AddMvcOptions(options =>
        {
            options.ModelBinderProviders.Insert(0, new DecimalCommaToPointSeparatorBinder());
            options.ModelBindingMessageProvider.SetLocale();
            options.Filters.Add<OperationCancelledExceptionFilter>();
        });

    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    services.AddSingleton<NodeGeneratorService>();
    services.AddInfrastructure(configuration);
    services.AddApplication(configuration);
}

void ConfigurePipeline(WebApplication app)
{
    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging(o =>
    {
        // Customize the message template
        o.MessageTemplate = "{RemoteIpAddress} @ {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        // Attach additional properties to the request completion event
        o.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RemoteIpAddress", httpContext?.Connection.RemoteIpAddress ?? IPAddress.None);
        };
    });

    app.UseRequestLocalization("de-DE");

    // Configure the HTTP request pipeline.
    app.UseExceptionHandler("/Error/500");

    app.UseMiddleware<CspMiddleware>();

    app.UseStatusCodePagesWithReExecute("/Error/{0}");

    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorPages();
    app.MapControllers();

    app.UseNodeGenerator();

    app.MapHealthChecks("/healthz");
}

try
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("logging.json")
        .Build();

    var logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();

    Log.Logger = logger;

    Log.Information("Application started");

    var builder = WebApplication.CreateBuilder(args);

    ConfigureApp(builder);
    using var app = builder.Build();
    
    ConfigurePipeline(app);

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("Application exited");
    await Log.CloseAndFlushAsync();
}
