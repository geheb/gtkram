namespace GtKram.Infrastructure.Persistence;

internal static class GuidExtensions
{
    public static string ToChar32(this Guid id) => id.ToString("N");

    public static byte[] ToBinary16(this Guid id) => id.ToByteArray(true);

    public static Guid FromChar32(this string id) => Guid.Parse(id);

    public static Guid FromBinary16(this byte[] id) => new Guid(id, bigEndian: true);
}
