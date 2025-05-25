using GtKram.Application;
using GtKram.Infrastructure;
using GtKram.Ui.Bindings;
using GtKram.Ui.Filters;
using GtKram.Ui.Middlewares;
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
    });

    services.AddSingleton<NodeGeneratorService>();
    services.AddInfrastructure(configuration);
    services.AddApplication(configuration);
}

void ConfigurePipeline(WebApplication app)
{
    app.UseNodeGenerator();

    app.UseSerilogRequestLogging(o =>
    {
        // Customize the message template
        o.MessageTemplate = "{RemoteIpAddress} @ {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        // Attach additional properties to the request completion event
        o.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext?.Request.Host.Value ?? "-");
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
    app.UseForwardedHeaders();
}

try
{
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .CreateBootstrapLogger();

    Log.Information("Application started");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    ConfigureApp(builder);
    using var app = builder.Build();
    
    ConfigurePipeline(app);
    app.MapRazorPages();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException && ex.Source != "Microsoft.EntityFrameworkCore.Design") // see https://github.com/dotnet/efcore/issues/29923
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("Application exited");
    Log.CloseAndFlush();
}
