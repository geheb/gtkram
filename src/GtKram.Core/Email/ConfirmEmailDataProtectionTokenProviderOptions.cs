using Microsoft.AspNetCore.Identity;

namespace GtKram.Core.Email;

public sealed class ConfirmEmailDataProtectionTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public static readonly string ProviderName = nameof(ConfirmEmailDataProtectionTokenProviderOptions);

    public ConfirmEmailDataProtectionTokenProviderOptions()
    {
        Name = ProviderName;
        TokenLifespan = TimeSpan.FromDays(3);
    }
}
