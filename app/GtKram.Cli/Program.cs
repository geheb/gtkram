using GtKram.Infrastructure;
using GtKram.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GtKram.Cli;

public sealed class Program
{
    static async Task<int> Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging();
                services.AddHttpClient();

                services.AddPersistence(context.Configuration);
                services.AddSmtp(context.Configuration);
            })
            .ConfigureLogging((context, config) =>
            {
                config.AddConsole();
            })
            .UseConsoleLifetime()
            .Build();

        if (args.Length < 1)
        {
            Console.WriteLine("No arguments provided");
            return -1;
        }

        switch (args[0])
        {
            case "--create-dataprotection-cert": return CreateDataProtectionCert();
            case "--test-email": return await TestEmail(host.Services);
        }

        Console.WriteLine("Unknown arguments detected!");

        return 1;
    }

    static int CreateDataProtectionCert()
    {
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddDnsName(Environment.MachineName);

        const string Name = "DataProtection";

        var distinguishedName = new X500DistinguishedName($"CN={Name}");

        using var rsa = RSA.Create(4096);

        var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

        request.CertificateExtensions.Add(
           new X509EnhancedKeyUsageExtension(
               new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

        request.CertificateExtensions.Add(sanBuilder.Build());

        var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.Date), new DateTimeOffset(DateTime.UtcNow.AddYears(10).Date));
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            certificate.FriendlyName = Name;
        }
        var pfx = certificate.Export(X509ContentType.Pfx);
        File.WriteAllBytes("dataprotection.pfx", pfx);

        return 0;
    }

    private static async Task<int> TestEmail(IServiceProvider serviceProvider)
    {
        using var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var emailSender = serviceScope.ServiceProvider.GetRequiredService<SmtpDispatcher>();

        Console.Write("Email: ");
        var email = Console.ReadLine();

        await emailSender.Send(email!, "Test", "<html><body>Test</body></html>");

        return 0;
    }
}
