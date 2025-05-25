using GtKram.Domain.Models;

namespace GtKram.Application.UseCases.Bazaar.Models;

public record struct ArticleWithEvent(Article Article, Event Event, bool HasBooked);