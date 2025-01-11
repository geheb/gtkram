using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Annotations;

/// <summary>
/// for client side validation: 
/// $.validator.addMethod("requiretrue", function (value, element, param) { return value === 'true' });
/// $.validator.unobtrusive.adapters.addBool("requiretrue");
/// </summary>
public class RequireTrueFieldAttribute : ValidationAttribute, IClientModelValidator
{
    public RequireTrueFieldAttribute()
    {
        ErrorMessage = "Das Feld '{0}' wird benötigt.";
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        var errorMessage = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-requiretrue", errorMessage);
    }

    private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (attributes.ContainsKey(key))
        {
            return false;
        }
        attributes.Add(key, value);
        return true;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is bool boolValue)
        {
            if (boolValue)
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult(null);
    }

    public override bool IsValid(object? value)
    {
        if (value is not bool boolValue)
        {
            return false;
        }

        return boolValue;
    }
}
