using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GtKram.Infrastructure.AspNetCore.Extensions;

public static class ErrorExtensions
{
    public static void AddError(this ModelStateDictionary modelState, IEnumerable<ErrorOr.Error> errors)
    {
        foreach (var e in errors)
        {
            AddError(modelState, e);
        }
    }

    public static void AddError(this ModelStateDictionary modelState, ErrorOr.Error error)
    {
        if (!modelState.ContainsKey(error.Code))
        {
            modelState.AddModelError(error.Code, error.Description);
        }
    }
}
