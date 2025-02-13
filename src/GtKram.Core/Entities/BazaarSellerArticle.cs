﻿namespace GtKram.Core.Entities;

internal sealed class BazaarSellerArticle : ChangedOn
{
    public Guid Id { get; set; }
    public Guid? BazaarSellerId { get; set; }
    public BazaarSeller? BazaarSeller { get; set; }
    public int LabelNumber { get; set; }
    public string? Name { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public int Status { get; set; }
    public BazaarBillingArticle? BazaarBillingArticle { get; set; }
}
