using System.Security.Cryptography;

namespace GtKram.Infrastructure.Persistence;

/// <summary>
/// generte UUID PKs based on https://mariadb.com/kb/en/guiduuid-performance/
/// </summary>
internal sealed class UuidPkGenerator
{
    static readonly byte[] V1ClockSequenceBytes = new byte[2];
    static readonly byte[] V1NodeBytes = new byte[6];
    static readonly long GregorianCalendarOffset = new DateTimeOffset(1582, 10, 15, 0, 0, 0, TimeSpan.Zero).Ticks;
    
    static long _lastTicks;

    static UuidPkGenerator()
    {
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(V1ClockSequenceBytes);
        rng.GetBytes(V1NodeBytes);
    }

    public Guid Generate()
    {
        var gen = GenerateV1();

        var result = gen.Clone() as byte[];
        if (result == null) throw new NullReferenceException("array clone failed");

        result[0] = gen[7];
        result[1] = gen[6];
        result[2] = gen[5];
        result[3] = gen[4];

        result[4] = gen[3];
        result[5] = gen[2];
        result[6] = gen[1];
        result[7] = gen[0];

        return new Guid(result);
    }

    long FetchNextValue()
    {
        var spin = new SpinWait();

        while (true)
        {
            var init = _lastTicks;
            var ticks = DateTime.UtcNow.Ticks - GregorianCalendarOffset;
            if (Interlocked.CompareExchange(ref _lastTicks, ticks, init) == init)
            {
                return ticks;
            }
            spin.SpinOnce();
        }
    }

    private byte[] GenerateV1()
    {
        var ticks = FetchNextValue();

        var timeBytes = BitConverter.GetBytes(ticks);

        var guidBytes = new byte[16];

        Array.Copy(timeBytes, 0, guidBytes, 0, timeBytes.Length);
        Array.Copy(V1ClockSequenceBytes, 0, guidBytes, 8, V1ClockSequenceBytes.Length);
        Array.Copy(V1NodeBytes, 0, guidBytes, 10, V1NodeBytes.Length);

        var versionIndex = BitConverter.IsLittleEndian ? 7 : 6;
        var variantIndex = 8;

        // set version 1 (time based)
        guidBytes[versionIndex] &= 0x0F;
        guidBytes[versionIndex] |= 0x10;

        // set variant RFC 4122
        guidBytes[variantIndex] &= 0x3f;
        guidBytes[variantIndex] |= 0x80;

        return guidBytes;
    }
}
