using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Annotations;

public sealed class RequiredFieldAttribute : RequiredAttribute
{
    public RequiredFieldAttribute()
    {
        ErrorMessage = "Das Feld '{0}' wird benötigt.";
    }
}
