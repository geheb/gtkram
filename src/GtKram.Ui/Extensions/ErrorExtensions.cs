using GtKram.Domain.Base;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GtKram.Ui.Extensions;

public static class ErrorExtensions
{
    public static void AddError(this ModelStateDictionary modelState, Error[] errors) =>
        Array.ForEach(errors, e => modelState.AddModelError(string.Empty, e.Message));
}
