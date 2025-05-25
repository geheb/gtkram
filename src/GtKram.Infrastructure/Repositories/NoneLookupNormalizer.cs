using Microsoft.AspNetCore.Identity;
using System.Diagnostics.CodeAnalysis;

namespace GtKram.Infrastructure.Repositories;

internal sealed class NoneLookupNormalizer : ILookupNormalizer
{
    [return: NotNullIfNotNull("email")]
    public string? NormalizeEmail(string? email) => email;

    [return: NotNullIfNotNull("name")]
    public string? NormalizeName(string? name) => name;
}
