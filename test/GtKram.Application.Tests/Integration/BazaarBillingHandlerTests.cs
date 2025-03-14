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
using NSubstitute.Exceptions;
using Shouldly;
using static System.Formats.Asn1.AsnWriter;

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
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingTotalsQuery();
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.BazaarEventId);
        result[0].CommissionTotal.ShouldBe(0m);
        result[0].SoldTotal.ShouldBe(0m);
        result[0].BillingCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetEventsWithBillingTotalsQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await SellerCanCreateBillings(scope, context.Id);
        await CreateBilling(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingTotalsQuery();
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.BazaarEventId);
        result[0].CommissionTotal.ShouldBe(0m);
        result[0].SoldTotal.ShouldBe(0m);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_GetBillingsWithTotalsAndEventQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventQuery(context.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetBillingsWithTotalsAndEventQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await SellerCanCreateBillings(scope, context.Id);
        await CreateBilling(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventQuery(context.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(1);
        result.Value.Billings[0].CreatedBy.ShouldBe(_mockUser.Name);
        result.Value.Billings[0].Total.ShouldBe(0);
        result.Value.Billings[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyEvent_GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await SellerCanCreateBillings(scope, context.Id);
        await CreateBilling(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.BazaarEventId);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_And_SellerCanCreateBillings_GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await SellerCanCreateBillings(scope, context.Id);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.BazaarEventId);
        result[0].BillingCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task SellerCanCreateBillings_GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await SellerCanCreateBillings(scope, context.Id);
        await CreateBilling(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.BazaarEventId);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_GetBillingsWithTotalsAndEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventByUserQuery(context.UserId, context.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetBillingsWithTotalsAndEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await SellerCanCreateBillings(scope, context.Id);
        await CreateBilling(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventByUserQuery(context.UserId, context.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(1);
        result.Value.Billings[0].CreatedBy.ShouldBe(_mockUser.Name);
        result.Value.Billings[0].Total.ShouldBe(0);
        result.Value.Billings[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Seller_CreateBillingByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.UserId, context.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Seller.BillingNotAllowed).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerWithBilling_CreateBillingByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await SellerCanCreateBillings(scope, context.Id);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.UserId, context.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerWithBilling_And_EventExpired_CreateBillingByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await SellerCanCreateBillings(scope, context.Id);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.UserId, context.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerIsManager_CreateBillingByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        _mockUser.Roles = [UserRoleType.Seller, UserRoleType.Manager];

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.UserId, context.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    private async Task<BazaarSeller> CreateEventAndSeller(IServiceScope scope)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var handler = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "Foo", Phone = "12345", Email = _mockUser.Email! };
        var result = await handler.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        var sellerReg = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        var command = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerReg.Id, ConfirmUserCallbackUrl = "http://localhost" };
        result = await handler.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        sellerReg = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var seller = await sellerRepo.Find(sellerReg.BazaarSellerId!.Value, _cancellationToken);
        seller.IsSuccess.ShouldBeTrue();

        return seller.Value;
    }

    private async Task SellerCanCreateBillings(IServiceScope scope, Guid sellerId)
    {
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var seller = await sellerRepo.Find(sellerId, _cancellationToken);
        seller.IsSuccess.ShouldBeTrue();
        seller.Value.CanCreateBillings = true;

        var handler = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        var command = new UpdateSellerCommand(seller.Value);
        var result = await handler.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    private async Task<Guid> CreateBilling(IServiceScope scope, BazaarSeller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(seller.UserId, seller.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }
}
