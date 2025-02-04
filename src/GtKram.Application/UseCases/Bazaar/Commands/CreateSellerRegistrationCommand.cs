using FluentResults;
using GtKram.Domain.Models;
using Mediator;

namespace GtKram.Application.UseCases.Bazaar.Commands;

public sealed record CreateSellerRegistrationCommand(BazaarSellerRegistration Registration, bool ShouldValidateEvent) : ICommand<Result>;