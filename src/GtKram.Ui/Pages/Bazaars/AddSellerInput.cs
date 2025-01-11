using GtKram.Ui.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Bazaars;

public class AddSellerInput
{
    [RequiredField, StringLength(64, MinimumLength = 2, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? Name { get; set; }

    [RequiredField, EmailLengthField, EmailField]
    public string? Email { get; set; }

    [RequiredField, RegularExpression(@"^(\d{4,15})$", ErrorMessage = "Das Feld '{0}' muss zwischen 4 und 15 Zeichen liegen und darf nur Zahlen enthalten.")]
    public string? Phone { get; set; }
}
