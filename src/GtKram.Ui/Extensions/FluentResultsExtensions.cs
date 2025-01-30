using FluentResults;
using GtKram.Domain.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GtKram.Ui.Extensions;

public static class FluentResultsExtensions
{
    public static void AddError(this ModelStateDictionary modelState, List<IError> errors) =>
        errors.ForEach(e => modelState.AddModelError(string.Empty, e.Message));
}
