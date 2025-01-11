using GtKram.Core.Converter;
using GtKram.Core.Entities;

namespace GtKram.Core.Models.Bazaar;

public class BazaarSellerDto
{
    public Guid? Id { get; set; }
    public string? EventNameAndDescription { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string? EventAddress { get; set; }
    public int ArticleCount { get; set; }
    public int MaxArticleCount { get; }
    public Guid? UserId { get; set; }
    public int SellerNumber { get; set; }
    public SellerRole Role { get; set; }
    public string? RegistrationName { get; set; }
    public string? RegistrationEmail { get; set; }
    public string? RegistrationPhone { get; set; }
    public bool IsRegisterAccepted { get; set; }
    public bool CanAddArticle => ArticleCount < MaxArticleCount;
    public bool CanCreateBillings { get; set; }
    public bool EditArticleExpired { get; private set; }
    public DateTimeOffset EditArticleEndDate { get; set; }
    public int Commission { get; set; }
    public bool IsEventExpired { get; set; }

    public string FormatEvent(GermanDateTimeConverter dc)
    {
        return EventNameAndDescription + ", " + dc.Format(StartDate, EndDate);
    }

    public BazaarSellerDto()
    {
    }

    internal BazaarSellerDto(BazaarSeller entity, int articleCount, GermanDateTimeConverter dc)
    {
        Id = entity.Id;

        var @event = entity.BazaarEvent!;

        var editArticleEndDateUtc = @event.EditArticleEndDate ?? @event.StartDate;
        EditArticleEndDate = dc.ToLocal(editArticleEndDateUtc);
        EditArticleExpired = DateTimeOffset.UtcNow > editArticleEndDateUtc;

        EventNameAndDescription = @event.Name + (string.IsNullOrEmpty(@event.Description) ? string.Empty : (" - " + @event.Description));
        StartDate = dc.ToLocal(@event.StartDate);
        EndDate = dc.ToLocal(@event.EndDate);
        EventAddress = @event.Address;
        Commission = @event.Commission;
        IsEventExpired = DateTimeOffset.UtcNow > @event.EndDate;

        UserId = entity.UserId;
        SellerNumber = entity.SellerNumber;
        Role = (SellerRole)entity.Role;
        ArticleCount = articleCount;
        MaxArticleCount = entity.MaxArticleCount;

        RegistrationName = entity.BazaarSellerRegistration?.Name;
        RegistrationEmail = entity.BazaarSellerRegistration?.Email;
        RegistrationPhone = entity.BazaarSellerRegistration?.Phone;
        IsRegisterAccepted = entity.BazaarSellerRegistration?.Accepted == true;
        CanCreateBillings = entity.CanCreateBillings;
    }
}
