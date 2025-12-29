namespace GtKram.Infrastructure.Email;

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DnsClient;
using DnsClient.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

internal sealed class IpReputationChecker
{
    private static readonly string[] Servers = ["zen.spamhaus.org", "bl.blocklist.de"];
    private readonly LookupClient _lookupClient;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly IMemoryCache _memoryCache;

    public IpReputationChecker(
        ILogger<IpReputationChecker> logger,
        IMemoryCache memoryCache)
    {
        var options = new LookupClientOptions
        {
            UseCache = true
        };
        _lookupClient = new LookupClient(options);
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public async Task<bool> IsListed(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) ||
            (address.AddressFamily is not AddressFamily.InterNetwork and not AddressFamily.InterNetworkV6))
        {
            return false;
        }

        string ipAddressReversed;
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var expand = string.Concat(address.GetAddressBytes().Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            ipAddressReversed = string.Join('.', expand.ToCharArray().Reverse());
        }
        else
        {
            ipAddressReversed = string.Join(".", address.GetAddressBytes().Reverse());
        }

        var key = "blacklist-" + address;
        if (_memoryCache.TryGetValue(key, out bool isListed))
        {
            return isListed;
        }

        foreach (var server in Servers)
        {
            var querydns = ipAddressReversed + "." + server;
            var entry = await _lookupClient.GetHostEntryAsync(querydns);

            if (entry is null ||
                entry.AddressList.Length == 0 ||
                !entry.AddressList.Any(e => e.ToString().StartsWith("127.0.0", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _logger.LogInformation("Address {Address} is listed at {Server}", address, server);

            _memoryCache.Set(key, true, DateTimeOffset.UtcNow.AddHours(1));
            return true;
        }

        _memoryCache.Set(key, false, DateTimeOffset.UtcNow.AddHours(1));
        return false;
    }

    public async Task<bool> IsListedMx(string domain, CancellationToken cancellationToken)
    {
        var key = "mx-" + domain;

        if (_memoryCache.TryGetValue(key, out bool isListed))
        {
            return isListed;
        }

        var response = await _lookupClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
        if (response.HasError)
        {
            return false;
        }

        // do not trust server without mx
        if (response.Answers.Count < 1)
        {
            _memoryCache.Set(key, true, DateTimeOffset.UtcNow.AddHours(3));
            return true;
        }

        foreach (var answer in response.Answers.MxRecords())
        {
            var host = answer.Exchange.Value;
            var entry = await _lookupClient.GetHostEntryAsync(host);
            if (entry == null || entry.AddressList.Length == 0)
            {
                continue;
            }

            foreach (var addr in entry.AddressList)
            {
                if (await IsListed(addr))
                {
                    _memoryCache.Set(key, true, DateTimeOffset.UtcNow.AddHours(1));
                    return true;
                }
            }
        }

        _memoryCache.Set(key, false, DateTimeOffset.UtcNow.AddHours(1));
        return false;
    }
}
