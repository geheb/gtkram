namespace GtKram.Infrastructure.Email;

using System.Globalization;
using GtKram.Application.Services;

internal sealed class EmailValidator : IEmailValidator
{
    private readonly string[] _knownProviderList =
    [
        "arcor.de",
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
        "yahoo.de",
        "yahoo.com",
        "yandex.com",
        "icloud.com",
        "me.com",
        "mac.com",
        "posteo.de",
        "freenet.de",
        "t-online.de",
        "o2online.de",
        "posteo.de",
        "unitybox.de",
        "online.de"
    ];

    private readonly IpReputationChecker _reputationChecker;

    public EmailValidator(IpReputationChecker reputationChecker)
    {
        _reputationChecker = reputationChecker;
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

        var isListed = await _reputationChecker.IsListedMx(domain, cancellationToken);

        return !isListed;
    }
}
