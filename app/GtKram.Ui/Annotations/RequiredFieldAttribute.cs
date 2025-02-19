using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Annotations;

public sealed class RequiredFieldAttribute : RequiredAttribute
{
    public RequiredFieldAttribute()
    {
        ErrorMessage = "Das Feld '{0}' wird benötigt.";
    }
}
