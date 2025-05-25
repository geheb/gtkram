using GtKram.Domain.Models;

namespace GtKram.Application.Tests;

public static class TestData
{
    public static Event CreateEvent(DateTimeOffset now) => new()
    {
        Name = "Kinderbasar",
        MaxSellers = 3,
        Start = now.AddDays(1),
        End = now.AddDays(2),
        RegisterStart = now,
        RegisterEnd = now.AddHours(1),
        EditArticleEnd = now.AddHours(2),
        PickUpLabelsStart = now.AddHours(3),
        PickUpLabelsEnd = now.AddHours(4)
    };
}
