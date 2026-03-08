using GtKram.Application;
using GtKram.Infrastructure;
using GtKram.Infrastructure.AspNetCore.Bindings;
using GtKram.Infrastructure.AspNetCore.Filters;
using GtKram.Infrastructure.AspNetCore.Routing;
using GtKram.Infrastructure.Security;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System.Net;
using System.Threading.RateLimiting;

void ConfigureApp(WebApplicationBuilder builder)
{
    builder.WebHost.ConfigureKestrel(o =>
    {
        o.AddServerHeader = false;
        o.AllowResponseHeaderCompression = false;
    });

    var services = builder.Services;
    var configuration = builder.Configuration;

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

    services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy(RateLimitPolicies.Login, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress + context.Request.Headers.UserAgent,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        options.AddPolicy(RateLimitPolicies.Registration, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress + context.Request.Headers.UserAgent,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });
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

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseStatusCodePagesWithReExecute("/Error/{0}");

    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseMiddleware<BlockerMiddleware>();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapRazorPages();
    app.MapControllers();

    app.UseNodeGenerator();

    if (app.Environment.IsDevelopment())
    {
        app.MapHealthChecks("/healthz");
    }
}

try
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
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
