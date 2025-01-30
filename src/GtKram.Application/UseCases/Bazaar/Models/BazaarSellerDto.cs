using GtKram.Application.Converter;
using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed class BazaarSellerDto
{
    public Guid? Id { get; set; }
    public string? EventNameAndDescription { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string? EventAddress { get; set; }
    public int ArticleCount { get; set; }
    public int MaxArticleCount { get; set; }
    public Guid? UserId { get; set; }
    public int SellerNumber { get; set; }
    public SellerRole Role { get; set; }
    public string? RegistrationName { get; set; }
    public string? RegistrationEmail { get; set; }
    public string? RegistrationPhone { get; set; }
    public bool IsRegisterAccepted { get; set; }
    public bool CanAddArticle => ArticleCount < MaxArticleCount;
    public bool CanCreateBillings { get; set; }
    public bool EditArticleExpired { get; set; }
    public DateTimeOffset EditArticleEndDate { get; set; }
    public int Commission { get; set; }
    public bool IsEventExpired { get; set; }

    public string FormatEvent(GermanDateTimeConverter dc)
    {
        return EventNameAndDescription + ", " + dc.FormatShort(StartDate, EndDate);
    }
}
