using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Annotations;

public sealed class EmailFieldAttribute : DataTypeAttribute
{
    private readonly EmailAddressAttribute _emailAddress = new EmailAddressAttribute();

    public EmailFieldAttribute() : base(DataType.EmailAddress)
    {
        ErrorMessage = "Das Feld '{0}' ist keine gültige E-Mail-Adresse.";
    }

    public override bool IsValid(object? value)
    {
        return _emailAddress.IsValid(value);
    }
}
