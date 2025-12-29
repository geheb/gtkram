using System.Globalization;
using System.Text;

namespace GtKram.Infrastructure.Email;

internal sealed class CalendarEvent
{
    public const string MimeType = "text/calendar";

    private const string _dateFormat = "yyyyMMddTHHmmss";

    public string Create(string title, string? location, DateTimeOffset start, DateTimeOffset end)
    {
        var startDate = start.ToString(_dateFormat, CultureInfo.InvariantCulture);
        var endDate = end.ToString(_dateFormat, CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.Append(Wrap("BEGIN:VCALENDAR"));
        sb.Append(Wrap("PRODID:-//GT Kram//NONSGML Event//EN"));
        sb.Append(Wrap("VERSION:2.0"));
        sb.Append(Wrap("BEGIN:VEVENT"));
        sb.Append(Wrap($"LOCATION:{Escape(location)}"));
        sb.Append(Wrap($"DTSTAMP:{startDate}"));
        sb.Append(Wrap($"DTSTART:{startDate}"));
        sb.Append(Wrap($"DTEND:{endDate}"));
        sb.Append(Wrap("SEQUENCE:0"));
        sb.Append(Wrap($"SUMMARY:{Escape(title)}"));
        sb.Append(Wrap($"UID:{Guid.NewGuid()}"));
        sb.Append(Wrap("END:VEVENT"));
        sb.Append(Wrap("BEGIN:VTIMEZONE"));
        sb.Append(Wrap("TZID:Europe/Berlin"));
        sb.Append(Wrap("X-LIC-LOCATION:Europe/Berlin"));
        sb.Append(Wrap("END:VTIMEZONE"));
        sb.Append(Wrap("END:VCALENDAR"));

        return sb.ToString();
    }

    private static string Wrap(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        const string lineBreak = "\r\n";

        var trimmed = value.Trim();
        if (trimmed.Length <= 75)
        {
            return trimmed + "\r\n";
        }

        const int takeLimit = 74;

        var firstLine = trimmed.Substring(0, takeLimit);
        var remainder = trimmed.Substring(takeLimit, trimmed.Length - takeLimit);

        var chunkedRemainder = string.Join($"{lineBreak} ", Chunk(remainder));
        return firstLine + $"{lineBreak} " + chunkedRemainder + lineBreak;
    }

    private static IEnumerable<string> Chunk(string str, int chunkSize = 73)
    {
        for (var index = 0; index < str.Length; index += chunkSize)
        {
            yield return str.Substring(index, Math.Min(chunkSize, str.Length - index));
        }
    }

    private static string? Escape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value
            .Replace("\r\n", @"\n")
            .Replace("\r", @"\n")
            .Replace(";", @"\;")
            .Replace(",", @"\,");
    }
}
