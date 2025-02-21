using GtKram.Domain.Models;

namespace GtKram.Application.Tests;

public static class TestData
{
    public static BazaarEvent CreateEvent(DateTimeOffset now) => new()
    {
        Name = "Kinderbasar",
        MaxSellers = 3,
        StartsOn = now.AddDays(1),
        EndsOn = now.AddDays(2),
        RegisterStartsOn = now,
        RegisterEndsOn = now.AddHours(1),
        EditArticleEndsOn = now.AddHours(2),
        PickUpLabelsStartsOn = now.AddHours(3),
        PickUpLabelsEndsOn = now.AddHours(4)
    };
}
