using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Annotations;

/// <summary>
/// min 8 and max 100 chars
/// </summary>
public sealed class PasswordLengthFieldAttribute : StringLengthAttribute
{
    public const int MinLen = 10;
    public PasswordLengthFieldAttribute() : base(100)
    {
        MinimumLength = MinLen;
        ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.";
    }
}
