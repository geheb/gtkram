using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace GtKram.Core.Database;

public static class DataProtectionExtensions
{
    /// <summary>
    /// Add certificate based DataProtection 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="appName"></param>
    /// <param name="configuration">Access to configuration values: 'PfxFile' and 'PfxPassword'</param>
    /// <returns></returns>
    public static IDataProtectionBuilder AddCertificate(this IDataProtectionBuilder builder, IConfiguration configuration) 
    {
        var certFile = configuration.GetValue<string>("PfxFile");
        var certPass = configuration.GetValue<string>("PfxPassword");

        if (!File.Exists(certFile))
        {
            throw new InvalidProgramException("missing certificate");
        }

        // openssl req -x509 -newkey rsa:4096 -keyout dataprotection.key -out dataprotection.crt -days 3650 -nodes -subj "/CN=app"
        // openssl pkcs12 -export -out dataprotection.pfx -inkey dataprotection.key -in dataprotection.crt -name "app"

        var protectionCert = new X509Certificate2(certFile!, certPass);

        return builder.SetApplicationName("GT Kram")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(7))
            .ProtectKeysWithCertificate(protectionCert)
            .PersistKeysToDbContext<AppDbContext>();
    }
}
