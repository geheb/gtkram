using GtKram.Ui.I18n;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace GtKram.Ui.Bindings;

public static class DefaultModelBindingExtensions
{
    public static void SetLocale(this DefaultModelBindingMessageProvider provider)
    {
        provider.SetAttemptedValueIsInvalidAccessor((a, b) =>
            string.Format(LocalizedMessages.AttemptedValueIsInvalidAccessor, a, b));
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(a =>
            string.Format(LocalizedMessages.NonPropertyAttemptedValueIsInvalidAccessor, a));
        provider.SetValueMustBeANumberAccessor(a =>
            string.Format(LocalizedMessages.FieldMustBeANumberAccessor, a));
    }
}
