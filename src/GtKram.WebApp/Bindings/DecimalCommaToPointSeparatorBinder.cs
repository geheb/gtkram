using GtKram.WebApp.I18n;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace GtKram.WebApp.Bindings;

public class DecimalCommaToPointSeparatorBinder : IModelBinderProvider
{
    sealed class DecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            if (decimal.TryParse(value.Replace(",", ".").Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            {
                bindingContext.Result = ModelBindingResult.Success(number);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, LocalizedMessages.FieldMustBeANumberAccessor);
            }

            return Task.CompletedTask;
        }
    }

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(decimal) ||
            context.Metadata.ModelType == typeof(decimal?))
        {
            return new DecimalModelBinder();
        }
        return null;
    }
}
