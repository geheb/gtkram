namespace GtKram.WebApp.Converter;

public sealed class ClothingConverter
{
    public string[] MapToString(int[] clothing)
    {
        if (clothing == null || !clothing.Any()) return Array.Empty<string>();

        return clothing.Select(v => IndexToString(v)).ToArray();
    }

    public string IndexToString(int index)
    {
        return index switch
        {
            0 => "Neugeborene",
            1 => "Von 62 bis 68",
            2 => "Von 74 bis 80",
            3 => "Von 86 bis 92",
            4 => "Von 98 bis 110",
            5 => "Von 116 bis 176",
            _ => $"Unbekannt: {index}"
        };
    }
}
