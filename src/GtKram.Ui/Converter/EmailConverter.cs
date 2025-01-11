using System.Globalization;

namespace GtKram.Ui.Converter;

public sealed class EmailConverter
{
    private readonly IdnMapping _idn = new IdnMapping();

    public string Anonymize(string email)
    {
        var emailSplit = email.Split('@');
        var emailUser = emailSplit[0];

        var emailDomain = _idn.GetUnicode(emailSplit[1]);
        return emailUser[0] + "***@" + emailDomain;
    }

    public string Normalize(string email)
    {
        var emailSplit = email.Split('@');
        return emailSplit[0] + "@" + _idn.GetUnicode(emailSplit[1]);
    }
}
