using Microsoft.AspNetCore.Identity;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GtKram.Infrastructure.Database.Repositories;

internal sealed class UpperLookupNormalizer : ILookupNormalizer
{
    [return: NotNullIfNotNull("email")]
    public string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email)
        ? email
        : new IdnMapping().GetAscii(email.ToUpperInvariant());

    [return: NotNullIfNotNull("name")]
    public string? NormalizeName(string? name) => 
        name?.ToUpperInvariant();
}
