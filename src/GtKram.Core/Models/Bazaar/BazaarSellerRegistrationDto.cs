using GtKram.Core.Entities;
using GtKram.Core.Extensions;
using System.Globalization;

namespace GtKram.Core.Models.Bazaar;

public class BazaarSellerRegistrationDto
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int[]? Clothing { get; set; }
    public bool? Accepted { get; set; }
    public Guid? BazaarSellerId { get; set; }
    public SellerRole? Role { get; set; }
    public int? SellerNumber { get; set; }
    public bool HasKita { get; set; }
    public int? ArticleCount { get; set; }
    public bool IsEventExpired { get; set; }

    public BazaarSellerRegistrationDto()
    {

    }

    internal BazaarSellerRegistrationDto(BazaarSellerRegistration entity, int articleCount, IdnMapping idn)
    {
        Id = entity.Id;
        Name = entity.Name;

        var email = entity.Email!.Split('@');
        Email = email[0] + "@" + idn.GetUnicode(email[1]);

        Phone = entity.Phone;
        Clothing = entity.Clothing?.Split(';').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        Accepted = entity.Accepted;

        BazaarSellerId = entity.BazaarSellerId;

        if (entity.BazaarSeller != null)
        {
            Role = (SellerRole)entity.BazaarSeller.Role;
            SellerNumber = entity.BazaarSeller.SellerNumber;
            ArticleCount = articleCount;
        }

        HasKita = entity.PreferredType == 1;
        IsEventExpired = entity.BazaarEvent != null && DateTimeOffset.UtcNow > entity.BazaarEvent.EndDate;
    }

    internal bool To(BazaarSellerRegistration entity)
    {
        if (Id.HasValue) entity.Id = Id.Value;
        var count = 0;
        if (entity.SetValue(e => e.Name, Name)) count++;
        if (entity.SetValue(e => e.Email, Email)) count++;
        if (entity.SetValue(e => e.Phone, Phone)) count++;
        var clothing = Clothing != null && Clothing.Length > 0 ? string.Join(";", Clothing) : null;
        if (entity.SetValue(e => e.Clothing, clothing)) count++;
        if (entity.SetValue(e => e.Accepted, Accepted)) count++;
        if (entity.SetValue(e => e.PreferredType, HasKita ? 1 : 0)) count++;
        return count > 0;
    }
}
