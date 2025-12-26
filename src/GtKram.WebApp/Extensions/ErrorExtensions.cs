using GtKram.Domain.Base;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GtKram.WebApp.Extensions;

public static class ErrorExtensions
{
    public static void AddError(this ModelStateDictionary modelState, params Error[] errors)
    {
        foreach (var err in errors)
        {
            if (modelState.ContainsKey(err.Code)) continue;
            modelState.AddModelError(err.Code, err.Message);
        }
    }

    public static void AddError(this ModelStateDictionary modelState, params ErrorOr.Error[] errors)
    {
        foreach (var err in errors)
        {
            if (modelState.ContainsKey(err.Code)) continue;
            modelState.AddModelError(err.Code, err.Description);
        }
    }
}
