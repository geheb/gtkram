using Microsoft.AspNetCore.Identity;

namespace GtKram.Application.UseCases.User.Extensions;

public static class IdentityErrorExtensions
{
    public static ErrorOr.Error ToError(this IdentityError error) =>
        ErrorOr.Error.Failure(error.Code, error.Description);

    public static List<ErrorOr.Error> ToError(this IEnumerable<IdentityError> errors) =>
        [.. errors.Select(e => e.ToError())];
}
