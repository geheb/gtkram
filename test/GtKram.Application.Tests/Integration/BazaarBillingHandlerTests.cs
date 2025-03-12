using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Handlers;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Base;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Repositories;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public sealed class BazaarBillingHandlerTests
{
    private readonly ServiceFixture _fixture = new();
    private IServiceProvider _serviceProvider = null!;
    private TimeProvider _mockTimeProvider = null!;
    private CancellationToken _cancellationToken;

    private readonly User _mockUser = new()
    {
        Id = Guid.NewGuid(),
        Name = "Foo",
        Email = "foo@bar.baz",
        Roles = [UserRoleType.Seller],
        IsEmailConfirmed = true
    };

    public BazaarBillingHandlerTests(TestContext context)
    {
        _cancellationToken = context.CancellationTokenSource.Token;
    }

    [TestInitialize]
    public void Init()
    {
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockTimeProvider.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);
        _fixture.Services.AddSingleton(_mockTimeProvider);
 
        var mockUserRepo = Substitute.For<IUserRepository>();
        mockUserRepo.GetAll(Arg.Any<CancellationToken>()).Returns([_mockUser]);
        mockUserRepo.FindById(_mockUser.Id, Arg.Any<CancellationToken>()).Returns(Result.Ok(_mockUser));
        mockUserRepo.FindByEmail(_mockUser.Email!, Arg.Any<CancellationToken>()).Returns(Result.Ok(_mockUser));
        _fixture.Services.AddScoped(_ => mockUserRepo);

        var mockEmailValidatorService = Substitute.For<IEmailValidatorService>();
        mockEmailValidatorService.Validate(Arg.Any<string>(), _cancellationToken).Returns(true);
        _fixture.Services.AddScoped(_ => mockEmailValidatorService);
        _fixture.Services.AddScoped<EmailQueueRepository>();

        _fixture.Services.AddScoped(_ => Substitute.For<IdentityErrorDescriber>());

        var mockMediator = Substitute.For<IMediator>();
        mockMediator.Send(Arg.Any<CreateUserCommand>(), _cancellationToken).Returns(Result.Ok(Guid.NewGuid()));
        _fixture.Services.AddScoped(_ => mockMediator);

        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AppSettings() { HeaderTitle = "Header", Organizer = "Organizer", PublicUrl = "http://localhost", Title = "Title" }));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ConfirmEmailDataProtectionTokenProviderOptions()));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions()));

        _fixture.Services.AddScoped<IEmailService, EmailService>();
        _fixture.Services.AddScoped<IBazaarEventRepository, BazaarEventRepository>();
        _fixture.Services.AddScoped<IBazaarSellerRegistrationRepository, BazaarSellerRegistrationRepository>();
        _fixture.Services.AddScoped<IBazaarSellerRepository, BazaarSellerRepository>();
        _fixture.Services.AddScoped<IBazaarSellerArticleRepository, BazaarSellerArticleRepository>();
        _fixture.Services.AddScoped<IBazaarBillingRepository, BazaarBillingRepository>();
        _fixture.Services.AddScoped<IBazaarBillingArticleRepository, BazaarBillingArticleRepository>();

        _fixture.Services.AddScoped<BazaarBillingHandler>();
        _fixture.Services.AddScoped<BazaarSellerHandler>();

        _serviceProvider = _fixture.Build();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture.Dispose();
    }

    [TestMethod]
    public async Task EmptyEvent_GetEventsWithBillingTotalsQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingTotalsQuery();
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].CommissionTotal.ShouldBe(0m);
        result[0].SoldTotal.ShouldBe(0m);
        result[0].BillingCount.ShouldBe(0);
    }

    private async Task CreateEventAndSeller(IServiceScope scope)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var handler = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "Foo", Phone = "12345", Email = _mockUser.Email! };
        var result = await handler.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;

        var command = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        result = await handler.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }
}
