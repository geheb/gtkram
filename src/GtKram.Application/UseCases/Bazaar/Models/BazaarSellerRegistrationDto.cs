namespace GtKram.Application.UseCases.Bazaar.Models;

public sealed class BazaarSellerRegistrationDto
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
}
