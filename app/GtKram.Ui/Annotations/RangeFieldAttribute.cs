using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Annotations;

public sealed class RangeFieldAttribute : RangeAttribute
{
    public RangeFieldAttribute(int minimum, int maximum) 
        : base(minimum, maximum)
    {
        ErrorMessage = "Das Feld '{0}' muss zwischen {1} und {2} liegen.";
    }

    public RangeFieldAttribute(double minimum, double maximum)
        : base(minimum, maximum)
    {
        ErrorMessage = "Das Feld '{0}' muss zwischen {1} und {2} liegen.";
    }
}
