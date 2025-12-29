using System.ComponentModel.DataAnnotations;

namespace GtKram.Infrastructure.AspNetCore.Annotations;

public sealed class PhoneFieldAttribute : RegularExpressionAttribute
{
    public PhoneFieldAttribute() : base("^(\\d{4,16})$")
    {
        ErrorMessage = "Das Feld '{0}' muss zwischen 4 und 16 Zeichen liegen und darf nur Zahlen enthalten.";
    }
}
