namespace GtKram.Application.Converter;

using System.Globalization;
using System.Runtime.InteropServices;

public sealed class GermanDateTimeConverter
{
    private const string IsoDateTimeFormat = "yyyy-MM-ddTHH:mm";
    private const string IsoDateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH\\:mm";
    private static readonly CultureInfo _culture = CultureInfo.CreateSpecificCulture("de-DE");
    private static readonly TimeZoneInfo _westEuropeTimeZone;

    static GermanDateTimeConverter()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var timezone = isWindows ? "Central European Standard Time" : "Europe/Berlin";
        _westEuropeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
    }

    public string? ToDateTime(DateTimeOffset date) => date.ToString("dd.MM.yyyy HH\\:mm", _culture);

    public string ToDateTimeShort(DateTimeOffset date) => date.ToString("dd.MM. HH\\:mm", _culture);

    public string ToDate(DateTimeOffset date) => date.ToString("dd.MM.yyyy", _culture);

    public string ToDate(DateOnly date) => date.ToString("dd.MM.yyyy", _culture);

    public string ToTime(DateTimeOffset date) => date.ToString(TimeFormat, _culture);

    public DateTimeOffset ToUtc(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var offset = new DateTimeOffset(dateTime, _westEuropeTimeZone.GetUtcOffset(dateTime));
        return offset.ToUniversalTime();
    }

    public DateTimeOffset ToLocal(DateTimeOffset date) => TimeZoneInfo.ConvertTime(date, _westEuropeTimeZone);

    public DateTimeOffset? FromIsoDateTime(string? isoDate)
    {
        if (!DateTime.TryParseExact(isoDate, IsoDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return default;
        }
        return TimeZoneInfo.ConvertTimeToUtc(dateTime, _westEuropeTimeZone);
    }

    public string ToIso(DateTimeOffset date) => date.ToString(IsoDateTimeFormat, CultureInfo.InvariantCulture);

    public string ToIso(DateOnly date) => date.ToString(IsoDateFormat, CultureInfo.InvariantCulture);

    public string ToIso(TimeOnly time) => time.ToString(TimeFormat, CultureInfo.InvariantCulture);

    public TimeOnly? FromIsoTime(string? isoTime)
    {
        if (!TimeOnly.TryParseExact(isoTime, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return default;
        }
        return time;
    }

    public DateOnly? FromIsoDate(string? isoDate)
    {
        if (!DateOnly.TryParseExact(isoDate, IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return default;
        }
        return date;
    }

    public string FormatShort(DateTimeOffset start, DateTimeOffset end)
    {
        var nowYear = ToLocal(DateTimeOffset.UtcNow).Year;

        if (start.Date == end.Date)
        {
            if (nowYear == start.Year)
            {
                return ToDateTimeShort(start) + "-" + ToTime(end);
            }
            return ToDateTime(start) + "-" + ToTime(end);
        }

        if (start.Year == end.Year && nowYear == start.Year)
        {
            return ToDateTimeShort(start) + " - " + ToDateTimeShort(end);
        }

        return ToDateTime(start) + " - " + ToDateTime(end);
    }

    public string FormatFull(DateTimeOffset start, DateTimeOffset end)
    {
        var from = ToDate(start) + " um " + ToTime(start);
        var to = ToDate(end) + " um " + ToTime(end);
        if (start == end)
        {
            return from + " Uhr";
        }
        else if (start.Date == end.Date)
        {
            return from + " - " + ToTime(end) + " Uhr";
        }
        return from + " Uhr - " + to + " Uhr";
    }

    public string Format(TimeSpan span) => span.TotalDays > 1 ? $"{span.TotalDays} Tage" : $"{span.TotalHours} Stunden";
}
