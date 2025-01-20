namespace GtKram.Infrastructure.Email;

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DnsClient;
using GtKram.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

internal sealed class EmailValidatorService : IEmailValidatorService
{
    private readonly string[] _knownProviderList = new[]
    {
        "aol.com",
        "gmx.de",
        "gmx.net",
        "gmx.at",
        "gmx.ch",
        "gmail.com",
        "googlemail.com",
        "mail.com",
        "outlook.de",
        "outlook.com",
        "hotmail.com",
        "hotmail.de",
        "web.de",
        "yahoo.com",
        "yandex.com",
        "icloud.com",
        "me.com",
        "mac.com",
        "freenet.de",
        "t-online.de",
        "o2online.de",
        "posteo.de",
        "unitybox.de",
        "online.de"
    };

    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;

    public EmailValidatorService(
        ILogger<EmailValidatorService> logger,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<bool> Validate(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(email))
        {
            return false;
        }

        email = new IdnMapping().GetAscii(email);

        var emailParts = email.Split('@', StringSplitOptions.RemoveEmptyEntries);
        if (emailParts.Length < 2)
        {
            return false;
        }

        var domain = emailParts[1];

        if (_knownProviderList.Any(e => e.Equals(domain, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (_memoryCache.TryGetValue(domain, out bool isValid))
        {
            if (!isValid)
            {
                _logger.LogError("Suspicous email domain {Domain} found", domain);
            }
            return isValid;
        }

        var client = new LookupClient();

        _logger.LogInformation("Query email domain {Domain}", domain);

        var response = await client.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
        if (response.HasError)
        {
            _logger.LogWarning("Query email domain {Domain} failed: {Error}", domain, response.ErrorMessage);
            _memoryCache.Set(domain, false, DateTimeOffset.UtcNow.AddHours(1));
            return false;
        }

        if (response.Answers.Count < 1)
        {
            _logger.LogError("Suspicous email domain {Domain} found", domain);
            _memoryCache.Set(domain, false, DateTimeOffset.UtcNow.AddHours(1));
            return false;
        }

        var count = 0;

        foreach (var answer in response.Answers.MxRecords())
        {
            var host = answer.Exchange.Value;
            var entry = await client.GetHostEntryAsync(host);
            if (entry == null || entry.AddressList.Length < 1)
            {
                continue;
            }

            foreach (var addr in entry.AddressList)
            {
                if (IPAddress.IsLoopback(addr))
                {
                    continue;
                }

                if (addr.AddressFamily is not
                    AddressFamily.InterNetwork and not AddressFamily.InterNetworkV6)
                {
                    continue;
                }

                count++;

                string ipAddressReversed;
                if (addr.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    var expand = string.Concat(addr.GetAddressBytes().Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
                    ipAddressReversed = string.Join('.', expand.ToCharArray().Reverse());
                }
                else
                {
                    ipAddressReversed = string.Join(".", addr.GetAddressBytes().Reverse());
                }

                var queryDns = ipAddressReversed + ".zen.spamhaus.org";
                entry = await client.GetHostEntryAsync(queryDns);
                if (entry == null || entry.AddressList.Length < 1)
                {
                    continue;
                }
                if (entry.AddressList.Any(e => e.ToString().StartsWith("127.0.0", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogError("Email domain {Domain} is listed at zen.spamhaus", domain);
                    _memoryCache.Set(domain, false, DateTimeOffset.UtcNow.AddHours(3));
                    return false;
                }
            }
        }

        if (count < 1)
        {
            _logger.LogError("Suspicous email domain {Domain} found", domain);
            _memoryCache.Set(domain, false, DateTimeOffset.UtcNow.AddHours(1));
            return false;
        }
        else
        {
            _memoryCache.Set(domain, true, DateTimeOffset.UtcNow.AddHours(6));
        }

        return true;
    }
}
