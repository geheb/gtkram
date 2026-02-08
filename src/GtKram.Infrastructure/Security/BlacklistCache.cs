using Microsoft.Extensions.Caching.Memory;
using System.IO.Hashing;
using System.Net;
using System.Text;

namespace GtKram.Infrastructure.Security;

public sealed class BlacklistCache
{
    private static readonly string _prefix = Guid.NewGuid().ToString("N")[..8];

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;

    public readonly record struct Item(string Key, uint Count, DateTimeOffset LastCall, double AvgSeconds)
    {
        public bool IsSuspicious => Count >= 7 && AvgSeconds <= 0.5;
    }

    public BlacklistCache(IMemoryCache cache, TimeProvider timeProvider)
    {
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public Item? Get(IPAddress address, string? userAgent) =>
        _cache.TryGetValue<Item>(CreateKey(address, userAgent), out var item)
        ? item
        : null;

    public void Update(IPAddress address, string? userAgent, bool isListed)
    {
        var key = CreateKey(address, userAgent);
        if (!_cache.TryGetValue<Item>(key, out var item))
        {
            item = new Item(key, isListed ? 4u : 1u, _timeProvider.GetUtcNow(), 0.0);
            _cache.Set(item.Key, item, DateTimeOffset.UtcNow.AddHours(1));
            return;
        }

        item = Create(item, isListed);
        _cache.Set(item.Key, item, DateTimeOffset.UtcNow.AddHours(1));
    }

    private Item Create(Item item, bool isListed)
    {
        var now = _timeProvider.GetUtcNow();
        var diff = (now - item.LastCall).TotalSeconds;
        var count = isListed ? item.Count + 4 : item.Count + 1;
        if (diff > 10.0)
        {
            return new(item.Key, count, now, 0.0);
        }
        else
        {
            var avg = item.AvgSeconds == 0.0 ? diff : (item.AvgSeconds + diff) / 2.0;
            return new(item.Key, count, now, avg);
        }
    }

    private static string CreateKey(IPAddress address, string? userAgent)
    {
        var addr = address.GetAddressBytes();
        var id = userAgent is null
            ? []
            : XxHash64.Hash(Encoding.UTF8.GetBytes(userAgent));

        return _prefix + Convert.ToHexStringLower([.. addr, .. id]);
    }
}
