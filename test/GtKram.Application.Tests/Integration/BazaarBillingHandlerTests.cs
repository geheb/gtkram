using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Handlers;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Commands;
using GtKram.Domain.Base;
using GtKram.Domain.Errors;
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

        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AppSettings() { HeaderTitle = "Header", Organizer = "Organizer", PublicUrl = "http://localhost", Title = "Title", RegisterRulesUrl = "http://localhost" }));
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
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].CommissionTotal.ShouldBe(0m);
        result[0].SoldTotal.ShouldBe(0m);
        result[0].BillingCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyBilling_GetEventsWithBillingTotalsQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingTotalsQuery();
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].CommissionTotal.ShouldBe(0m);
        result[0].SoldTotal.ShouldBe(0m);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetEventsWithBillingTotalsQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingTotalsQuery();
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].CommissionTotal.ShouldBe(1.2m);
        result[0].SoldTotal.ShouldBe(6);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_GetBillingsWithTotalsAndEventQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventQuery(context.Seller.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyBilling_GetBillingsWithTotalsAndEventQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventQuery(context.Seller.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(1);
        result.Value.Billings[0].CreatedBy.ShouldBe(_mockUser.Name);
        result.Value.Billings[0].Total.ShouldBe(0);
        result.Value.Billings[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetBillingsWithTotalsAndEventQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventQuery(context.Seller.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(1);
        result.Value.Billings[0].CreatedBy.ShouldBe(_mockUser.Name);
        result.Value.Billings[0].Total.ShouldBe(6);
        result.Value.Billings[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task EmptyEvent_GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EmptyBilling_GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_And_SellerCanCreateBillings_GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].BillingCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyBilling_And_SellerCanCreateBillings_GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task SellerCanCreateBillings_GetEventsWithBillingByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetEventsWithBillingByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].BillingCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_GetBillingsWithTotalsAndEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventByUserQuery(context.Seller.UserId, context.Seller.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyBilling_GetBillingsWithTotalsAndEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventByUserQuery(context.Seller.UserId, context.Seller.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(1);
        result.Value.Billings[0].CreatedBy.ShouldBe(_mockUser.Name);
        result.Value.Billings[0].Total.ShouldBe(0);
        result.Value.Billings[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetBillingsWithTotalsAndEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingsWithTotalsAndEventByUserQuery(context.Seller.UserId, context.Seller.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billings.Length.ShouldBe(1);
        result.Value.Billings[0].CreatedBy.ShouldBe(_mockUser.Name);
        result.Value.Billings[0].Total.ShouldBe(6);
        result.Value.Billings[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task SellerWithoutBilling_CreateBillingByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.Seller.UserId, context.Seller.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Seller.BillingNotAllowed).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerWithBilling_CreateBillingByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.Seller.UserId, context.Seller.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerWithBilling_And_EventExpired_CreateBillingByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.Seller.UserId, context.Seller.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerIsManager_CreateBillingByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        _mockUser.Roles = [UserRoleType.Seller, UserRoleType.Manager];

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.Seller.UserId, context.Seller.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerIsManager_And_EventExpired_CreateBillingByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        _mockUser.Roles = [UserRoleType.Seller, UserRoleType.Manager];

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(context.Seller.UserId, context.Seller.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyBilling_GetBillingArticlesWithBillingAndEventQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingArticlesWithBillingAndEventQuery(billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billing.Id.ShouldBe(billingId);
        result.Value.Articles.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetBillingArticlesWithBillingAndEventQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingArticlesWithBillingAndEventQuery(billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billing.Id.ShouldBe(billingId);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task EmptyBilling_CancelBillingByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldNotBeEmpty();

        var command = new CancelBillingByUserCommand(context.Seller.UserId, billingId);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task CompletedBilling_CancelBillingByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldNotBeEmpty();

        var command = new CancelBillingByUserCommand(context.Seller.UserId, billingId);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Billing.StatusCompleted).ShouldBeTrue();
    }

    [TestMethod]
    public async Task OpenBilling_CancelBillingByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldNotBeEmpty();

        var command = new CancelBillingByUserCommand(context.Seller.UserId, billingId);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task OpenBilling_And_EventExpired_CancelBillingByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldNotBeEmpty();

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new CancelBillingByUserCommand(context.Seller.UserId, billingId);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyBilling_CancelBillingCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldNotBeEmpty();

        var command = new CancelBillingCommand(billingId);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task OpenBilling_CancelBillingCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldNotBeEmpty();

        var command = new CancelBillingCommand(billingId);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task CompletedBilling_CancelBillingCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldNotBeEmpty();

        var command = new CancelBillingCommand(billingId);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task CompletedBilling_CreateBillingArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller, 3);
        var billingId = await CreateCompletedBilling(scope, context.Seller, 1);

        var billingArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingArticleRepository>();
        var billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        var article = articles.First(a => !billingArticles.Any(b => b.BazaarSellerArticleId == a.Id));

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingArticleByUserCommand(context.Seller.UserId, billingId, article.Id);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Billing.StatusCompleted).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CompletedBilling_And_EventExpired_CancelBillingCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldNotBeEmpty();

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new CancelBillingCommand(billingId);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        billings = await billingRepo.GetByBazaarEventId(context.Seller.BazaarEventId, _cancellationToken);
        billings.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EmptyBilling_CompleteBillingByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CompleteBillingByUserCommand(context.Seller.UserId, billingId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Billing.Empty).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyBilling_CompleteBillingCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CompleteBillingCommand(billingId);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Billing.Empty).ShouldBeTrue();
    }

    [TestMethod]
    public async Task OpenBilling_And_EventExpired_CompleteBillingCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var billingRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingRepository>();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new CompleteBillingCommand(billingId);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyBilling_FindBillingTotalQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new FindBillingTotalQuery(billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ArticleCount.ShouldBe(0);
        result.Value.Total.ShouldBe(0);
    }

    [TestMethod]
    public async Task OpenBilling_FindBillingTotalQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new FindBillingTotalQuery(billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ArticleCount.ShouldBe(0);
        result.Value.Total.ShouldBe(0);
    }

    [TestMethod]
    public async Task CompletedBilling_FindBillingTotalQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new FindBillingTotalQuery(billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ArticleCount.ShouldBe(3);
        result.Value.Total.ShouldBe(6);
    }

    [TestMethod]
    public async Task EmptyBilling_GetBillingArticlesWithBillingAndEventByUserQuery_IsSuccees()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingArticlesWithBillingAndEventByUserQuery(context.Seller.UserId, billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billing.Id.ShouldBe(billingId);
        result.Value.Articles.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task OpenBilling_GetBillingArticlesWithBillingAndEventByUserQuery_IsSuccees()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingArticlesWithBillingAndEventByUserQuery(context.Seller.UserId, billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billing.Id.ShouldBe(billingId);
        result.Value.Billing.Status.ShouldBe(BillingStatus.InProgress);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task CompletedBilling_GetBillingArticlesWithBillingAndEventByUserQuery_IsSuccees()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new GetBillingArticlesWithBillingAndEventByUserQuery(context.Seller.UserId, billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Billing.Id.ShouldBe(billingId);
        result.Value.Billing.Status.ShouldBe(BillingStatus.Completed);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task EmptyBilling_FindEventByBillingQuery_IsSuccees()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var billingId = await CreateEmptyBilling(scope, context.Seller);
 
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var query = new FindEventByBillingQuery(billingId);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(context.Seller.BazaarEventId);
    }

    [TestMethod]
    public async Task CreateBillingArticleManuallyByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingArticleManuallyByUserCommand(context.Seller.UserId, billingId, context.Seller.SellerNumber, articles[0].LabelNumber);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SomeArticleTwoTimes_CreateBillingArticleManuallyByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingArticleManuallyByUserCommand(context.Seller.UserId, billingId, context.Seller.SellerNumber, articles[0].LabelNumber);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == BillingArticle.AlreadyBooked);
    }

    [TestMethod]
    public async Task SomeArticleTwoTimes_CreateBillingArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateEmptyBilling(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingArticleByUserCommand(context.Seller.UserId, billingId, articles[0].Id);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == BillingArticle.AlreadyBooked);
    }

    [TestMethod]
    public async Task OpenBilling_DeleteBillingArticleByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var billingArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingArticleRepository>();
        var billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(3);

        var command = new DeleteBillingArticleByUserCommand(context.Seller.UserId, billingArticles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(2);
    }

    [TestMethod]
    public async Task OpenBilling_DeleteBillingArticleCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var billingArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingArticleRepository>();
        var billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(3);

        var command = new DeleteBillingArticleCommand(billingArticles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(2);
    }

    [TestMethod]
    public async Task CompletedBilling_DeleteBillingArticleCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var billingArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingArticleRepository>();
        var billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(3);

        var command = new DeleteBillingArticleCommand(billingArticles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(2);
    }

    [TestMethod]
    public async Task CompletedBilling_And_EventExpired_DeleteBillingArticleCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var billingArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingArticleRepository>();
        var billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(3);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new DeleteBillingArticleCommand(billingArticles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task OpenBilling_And_EventExpired_DeleteBillingArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateOpenBilling(scope, context.Seller);

        var billingArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingArticleRepository>();
        var billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(3);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new DeleteBillingArticleByUserCommand(context.Seller.UserId, billingArticles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CompletedBilling_DeleteBillingArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateBillings(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var billingId = await CreateCompletedBilling(scope, context.Seller);

        var billingArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarBillingArticleRepository>();
        var billingArticles = await billingArticleRepo.GetByBazaarBillingId(billingId, _cancellationToken);
        billingArticles.Length.ShouldBe(3);

        var command = new DeleteBillingArticleByUserCommand(context.Seller.UserId, billingArticles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Billing.StatusCompleted).ShouldBeTrue();
    }

    private async Task<(BazaarSeller Seller, BazaarSellerRegistration Registration)> CreateEventAndSeller(IServiceScope scope, int maxArticleCount = 3)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = _mockUser.Email };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        var sellerReg = await sellerRegRepo.FindByEmailAndBazaarEventId(_mockUser.Email, eventId, _cancellationToken);
        sellerReg.IsSuccess.ShouldBeTrue();

        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerReg.Value.Id, ConfirmUserCallbackUrl = "http://localhost" };
        result = await sut.Handle(acceptCommand, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        sellerReg = await sellerRegRepo.FindByEmailAndBazaarEventId(_mockUser.Email, eventId, _cancellationToken);
        sellerReg.IsSuccess.ShouldBeTrue();

        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var seller = await sellerRepo.Find(sellerReg.Value.BazaarSellerId!.Value, _cancellationToken);
        seller.IsSuccess.ShouldBeTrue();
        
        seller.Value.MaxArticleCount = maxArticleCount;
        result = await sellerRepo.Update(seller.Value, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        return (seller.Value, sellerReg.Value);
    }

    private async Task<BazaarSellerArticle[]> CreateSellerArticles(IServiceScope scope, BazaarSeller seller, int count = 3)
    {
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        foreach (var i in Enumerable.Range(1, count))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        return await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
    }

    private async Task CanCreateBillings(IServiceScope scope, (BazaarSeller seller, BazaarSellerRegistration registration) context)
    {
        var seller = context.seller;
        seller.CanCreateBillings = true;
        var handler = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new UpdateSellerCommand(context.registration.Id, seller.SellerNumber, seller.Role, seller.CanCreateBillings);
        var result = await handler.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    private async Task<Guid> CreateEmptyBilling(IServiceScope scope, BazaarSeller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var command = new CreateBillingByUserCommand(seller.UserId, seller.BazaarEventId);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private async Task<Guid> CreateOpenBilling(IServiceScope scope, BazaarSeller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var billingCommand = new CreateBillingByUserCommand(seller.UserId, seller.BazaarEventId);
        var billingResult = await sut.Handle(billingCommand, _cancellationToken);
        billingResult.IsSuccess.ShouldBeTrue();

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);

        foreach (var article in articles)
        {
            var command = new CreateBillingArticleByUserCommand(seller.UserId, billingResult.Value, article.Id);
            var result = await sut.Handle(command, _cancellationToken);
            result.IsSuccess.ShouldBeTrue();
        }

        return billingResult.Value;
    }

    private async Task<Guid> CreateCompletedBilling(IServiceScope scope, BazaarSeller seller, int articleCount = 3)
    {
        var sut = scope.ServiceProvider.GetRequiredService<BazaarBillingHandler>();
        var billingCommand = new CreateBillingByUserCommand(seller.UserId, seller.BazaarEventId);
        var billingResult = await sut.Handle(billingCommand, _cancellationToken);
        billingResult.IsSuccess.ShouldBeTrue();

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);

        foreach (var article in articles.Take(articleCount))
        {
            var command = new CreateBillingArticleByUserCommand(seller.UserId, billingResult.Value, article.Id);
            var result = await sut.Handle(command, _cancellationToken);
            result.IsSuccess.ShouldBeTrue();
        }

        var completeCommand = new CompleteBillingByUserCommand(seller.UserId, billingResult.Value);
        var completeResult = await sut.Handle(completeCommand, _cancellationToken);
        completeResult.IsSuccess.ShouldBeTrue();

        return billingResult.Value;
    }
}
