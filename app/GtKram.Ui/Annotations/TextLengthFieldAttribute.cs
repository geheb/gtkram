using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Annotations;

/// <summary>
/// min 2 and max 256 chars
/// </summary>
public sealed class TextLengthFieldAttribute : StringLengthAttribute
{
    public TextLengthFieldAttribute() : base(256)
    {
        MinimumLength = 2;
        ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.";
    }

    public TextLengthFieldAttribute(int max) : base(max)
    {
        MinimumLength = 2;
        ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.";
    }
}
