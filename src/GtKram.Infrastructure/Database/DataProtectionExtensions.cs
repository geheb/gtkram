namespace GtKram.Infrastructure.Database;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

internal static class DataProtectionExtensions
{
    public static void AddCertificate(this IDataProtectionBuilder builder, IConfiguration config) 
    {
        var dataProtection = config.GetSection("DataProtection");

        // openssl req -x509 -newkey rsa:4096 -keyout dataprotection.key -out dataprotection.crt -days 3650 -nodes -subj "/CN=app"
        // openssl pkcs12 -export -out dataprotection.pfx -inkey dataprotection.key -in dataprotection.crt -name "app"

        var certFile = dataProtection.GetValue<string>("PfxFile");
        var certPass = dataProtection.GetValue<string>("PfxPassword");
        var keysDir = dataProtection.GetValue<string>("KeysDirectory");

        if (!File.Exists(certFile))
        {
            throw new InvalidProgramException("missing certificate");
        }

        var keysDirInfo = new DirectoryInfo(keysDir!);
        if (!keysDirInfo.Exists)
        {
            keysDirInfo.Create();
        }

        var protectionCert = X509CertificateLoader.LoadPkcs12FromFile(certFile, certPass);

        builder.SetApplicationName("GT Kram")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(7))
            .ProtectKeysWithCertificate(protectionCert)
            .PersistKeysToFileSystem(keysDirInfo);
    }
}
