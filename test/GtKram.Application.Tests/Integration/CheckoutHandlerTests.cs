using FluentMigrator.Runner;
using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
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
public sealed class CheckoutHandlerTests
{
    private const string _mockUserSeller = "foo@bar.baz";
    private const string _mockUserManager = "bar@foo.baz";
    private readonly ServiceFixture _fixture = new();
    private IServiceProvider _serviceProvider = null!;
    private TimeProvider _mockTimeProvider = null!;
    private CancellationToken _cancellationToken;

    public CheckoutHandlerTests(TestContext context)
    {
        _cancellationToken = context.CancellationTokenSource.Token;
    }

    [TestInitialize]
    public async Task Init()
    {
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockTimeProvider.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);
        _fixture.Services.AddSingleton(_mockTimeProvider);
 
        var mockEmailValidatorService = Substitute.For<IEmailValidator>();
        mockEmailValidatorService.Validate(Arg.Any<string>(), _cancellationToken).Returns(true);
        _fixture.Services.AddScoped(_ => mockEmailValidatorService);

        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AppSettings() { HeaderTitle = "Header", Organizer = "Organizer", PublicUrl = "http://localhost", Title = "Title", RegisterRulesUrl = "http://localhost" }));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ConfirmEmailDataProtectionTokenProviderOptions()));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions()));

        _fixture.Services.AddScoped<EmailQueues>();
        _fixture.Services.AddScoped<IUsers, Users>();
        _fixture.Services.AddScoped<IEmailService, EmailService>();
        _fixture.Services.AddScoped<IEvents, Events>();
        _fixture.Services.AddScoped<ISellerRegistrations, SellerRegistrations>();
        _fixture.Services.AddScoped<ISellers, Sellers>();
        _fixture.Services.AddScoped<IArticles, Articles>();
        _fixture.Services.AddScoped<ICheckouts, Checkouts>();

        _serviceProvider = _fixture.Build();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();

        var users = scope.ServiceProvider.GetRequiredService<IUsers>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Infrastructure.Database.Models.Identity>>();
        var result = await users.Create("foo", _mockUserSeller, [UserRoleType.Seller], _cancellationToken);
        var identity = await userManager.FindByEmailAsync(_mockUserSeller);
        identity!.Json.IsEmailConfirmed = true;
        await userManager.UpdateAsync(identity);

        result = await users.Create("bar", _mockUserManager, [UserRoleType.Manager], _cancellationToken);
        identity = await userManager.FindByEmailAsync(_mockUserManager);
        identity!.Json.IsEmailConfirmed = true;
        await userManager.UpdateAsync(identity);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task EmptyEvent_GetEventWithCheckoutTotalsQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutTotalsQuery();
        var result = await sut.Send(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].CommissionTotal.ShouldBe(0m);
        result[0].SoldTotal.ShouldBe(0m);
        result[0].CheckoutCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyCheckout_GetEventWithCheckoutTotalsQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutTotalsQuery();
        var result = await sut.Send(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].CommissionTotal.ShouldBe(0m);
        result[0].SoldTotal.ShouldBe(0m);
        result[0].CheckoutCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetEventWithCheckoutTotalsQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutTotalsQuery();
        var result = await sut.Send(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].CommissionTotal.ShouldBe(1.2m);
        result[0].SoldTotal.ShouldBe(6);
        result[0].CheckoutCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_GetCheckoutWithTotalsAndEventQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetCheckoutWithTotalsAndEventQuery(context.Seller.EventId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkouts.Length.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyCheckout_GetCheckoutWithTotalsAndEventQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetCheckoutWithTotalsAndEventQuery(context.Seller.EventId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkouts.Length.ShouldBe(1);
        result.Value.Checkouts[0].CreatedBy.ShouldBe("foo");
        result.Value.Checkouts[0].Total.ShouldBe(0);
        result.Value.Checkouts[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetCheckoutWithTotalsAndEventQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetCheckoutWithTotalsAndEventQuery(context.Seller.EventId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkouts.Length.ShouldBe(1);
        result.Value.Checkouts[0].CreatedBy.ShouldBe("foo");
        result.Value.Checkouts[0].Total.ShouldBe(6);
        result.Value.Checkouts[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task EmptyEvent_GetEventWithCheckoutCountByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutCountByUserQuery(context.Seller.IdentityId);
        var result = await sut.Send(query, _cancellationToken);

        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EmptyCheckout_GetEventWithCheckoutCountByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutCountByUserQuery(context.Seller.IdentityId);
        var result = await sut.Send(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].CheckoutCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task GetEventWithCheckoutCountByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutCountByUserQuery(context.Seller.IdentityId);
        var result = await sut.Send(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].CheckoutCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_And_SellerCanCreateCheckout_GetEventWithCheckoutCountByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutCountByUserQuery(context.Seller.IdentityId);
        var result = await sut.Send(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].CheckoutCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyCheckout_And_SellerCanCreateCheckout_GetEventWithCheckoutCountByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutCountByUserQuery(context.Seller.IdentityId);
        var result = await sut.Send(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].CheckoutCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task SellerCanCreateCheckout_GetEventWithCheckoutCountByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetEventWithCheckoutCountByUserQuery(context.Seller.IdentityId);
        var result = await sut.Send(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].CheckoutCount.ShouldBe(1);
    }

    [TestMethod]
    public async Task EmptyEvent_GetCheckoutWithTotalsAndEventByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetCheckoutWithTotalsAndEventByUserQuery(context.Seller.IdentityId, context.Seller.EventId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkouts.Length.ShouldBe(0);
    }

    [TestMethod]
    public async Task EmptyCheckout_GetCheckoutWithTotalsAndEventByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetCheckoutWithTotalsAndEventByUserQuery(context.Seller.IdentityId, context.Seller.EventId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkouts.Length.ShouldBe(1);
        result.Value.Checkouts[0].CreatedBy.ShouldBe("foo");
        result.Value.Checkouts[0].Total.ShouldBe(0);
        result.Value.Checkouts[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetCheckoutWithTotalsAndEventByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetCheckoutWithTotalsAndEventByUserQuery(context.Seller.IdentityId, context.Seller.EventId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkouts.Length.ShouldBe(1);
        result.Value.Checkouts[0].CreatedBy.ShouldBe("foo");
        result.Value.Checkouts[0].Total.ShouldBe(6);
        result.Value.Checkouts[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task SellerWithoutCheckout_CreateCheckoutByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutByUserCommand(context.Seller.IdentityId, context.Seller.EventId);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Seller.CheckoutNotAllowed).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerWithCheckout_CreateCheckoutByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutByUserCommand(context.Seller.IdentityId, context.Seller.EventId);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task SellerWithCheckout_And_EventExpired_CreateCheckoutByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutByUserCommand(context.Seller.IdentityId, context.Seller.EventId);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SellerIsManager_CreateCheckoutByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope, _mockUserManager);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutByUserCommand(context.Seller.IdentityId, context.Seller.EventId);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task SellerIsManager_And_EventExpired_CreateCheckoutByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope, _mockUserManager);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutByUserCommand(context.Seller.IdentityId, context.Seller.EventId);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task EmptyCheckout_GetArticlesWithCheckoutAndEventQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetArticlesWithCheckoutAndEventQuery(checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkout.Id.ShouldBe(checkoutId);
        result.Value.Articles.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetArticlesWithCheckoutAndEventQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetArticlesWithCheckoutAndEventQuery(checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkout.Id.ShouldBe(checkoutId);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task EmptyCheckout_CancelCheckoutByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldNotBeEmpty();

        var command = new CancelCheckoutByUserCommand(context.Seller.IdentityId, checkoutId);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();

        checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task CompletedCheckout_CancelCheckoutByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldNotBeEmpty();

        var command = new CancelCheckoutByUserCommand(context.Seller.IdentityId, checkoutId);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Checkout.StatusCompleted).ShouldBeTrue();
    }

    [TestMethod]
    public async Task OpenCheckout_CancelCheckoutByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldNotBeEmpty();

        var command = new CancelCheckoutByUserCommand(context.Seller.IdentityId, checkoutId);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task OpenCheckout_And_EventExpired_CancelCheckoutByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldNotBeEmpty();

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new CancelCheckoutByUserCommand(context.Seller.IdentityId, checkoutId);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyCheckout_CancelCheckoutCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldNotBeEmpty();

        var command = new CancelCheckoutCommand(checkoutId);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();

        checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task OpenCheckout_CancelCheckoutCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldNotBeEmpty();

        var command = new CancelCheckoutCommand(checkoutId);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();

        checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task CompletedCheckout_CancelCheckoutCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldNotBeEmpty();

        var command = new CancelCheckoutCommand(checkoutId);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();

        checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task CompletedCheckout_CreateCheckoutArticleByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller, 3);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller, 1);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        var articleNotChecked = articles.First(a => !checkout.Value.ArticleIds.Contains(a.Id));

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutArticleByUserCommand(context.Seller.IdentityId, checkoutId, articleNotChecked.Id);

        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Checkout.StatusCompleted).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CompletedCheckout_And_EventExpired_CancelCheckoutCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldNotBeEmpty();

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new CancelCheckoutCommand(checkoutId);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();

        checkouts = await checkoutRepo.GetByEventId(context.Seller.EventId, _cancellationToken);
        checkouts.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EmptyCheckout_CompleteCheckoutByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CompleteCheckoutByUserCommand(context.Seller.IdentityId, checkoutId);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Checkout.Empty).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyCheckout_CompleteCheckoutCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CompleteCheckoutCommand(checkoutId);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Checkout.Empty).ShouldBeTrue();
    }

    [TestMethod]
    public async Task OpenCheckout_And_EventExpired_CompleteCheckoutCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new CompleteCheckoutCommand(checkoutId);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task EmptyCheckout_FindCheckoutTotalQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new FindCheckoutTotalQuery(checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.ArticleCount.ShouldBe(0);
        result.Value.Total.ShouldBe(0);
    }

    [TestMethod]
    public async Task OpenCheckout_FindCheckoutTotalQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new FindCheckoutTotalQuery(checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.ArticleCount.ShouldBe(0);
        result.Value.Total.ShouldBe(0);
    }

    [TestMethod]
    public async Task CompletedCheckout_FindCheckoutTotalQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new FindCheckoutTotalQuery(checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.ArticleCount.ShouldBe(3);
        result.Value.Total.ShouldBe(6);
    }

    [TestMethod]
    public async Task EmptyCheckout_GetArticlesWithCheckoutAndEventByUserQuery_IsSuccees()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetArticlesWithCheckoutAndEventByUserQuery(context.Seller.IdentityId, checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkout.Id.ShouldBe(checkoutId);
        result.Value.Articles.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task OpenCheckout_GetArticlesWithCheckoutAndEventByUserQuery_IsSuccees()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetArticlesWithCheckoutAndEventByUserQuery(context.Seller.IdentityId, checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkout.Id.ShouldBe(checkoutId);
        result.Value.Checkout.Status.ShouldBe(CheckoutStatus.InProgress);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task CompletedCheckout_GetArticlesWithCheckoutAndEventByUserQuery_IsSuccees()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new GetArticlesWithCheckoutAndEventByUserQuery(context.Seller.IdentityId, checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Checkout.Id.ShouldBe(checkoutId);
        result.Value.Checkout.Status.ShouldBe(CheckoutStatus.Completed);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task EmptyCheckout_FindEventByCheckoutQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);
 
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var query = new FindEventByCheckoutQuery(checkoutId);
        var result = await sut.Send(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldBe(context.Seller.EventId);
    }

    [TestMethod]
    public async Task CreateCheckoutArticleManuallyByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutArticleManuallyByUserCommand(context.Seller.IdentityId, checkoutId, context.Seller.SellerNumber, articles[0].LabelNumber);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task SomeArticleTwoTimes_CreateCheckoutArticleManuallyByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutArticleManuallyByUserCommand(context.Seller.IdentityId, checkoutId, context.Seller.SellerNumber, articles[0].LabelNumber);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();
        result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Checkout.AlreadyBooked);
    }

    [TestMethod]
    public async Task SomeArticleTwoTimes_CreateCheckoutArticleByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutArticleByUserCommand(context.Seller.IdentityId, checkoutId, articles[0].Id);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();
        result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Checkout.AlreadyBooked);
    }

    [TestMethod]
    public async Task SomeArticleTwoCheckouts_CreateCheckoutArticleByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        await CreateCompletedCheckout(scope, context.Seller, 2);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutArticleByUserCommand(context.Seller.IdentityId, checkoutId, articles[0].Id);
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Checkout.AlreadyBooked);
    }

    [TestMethod]
    public async Task SomeArticleTwoCheckouts_CreateCheckoutArticleManuallyByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        await CreateOpenCheckout(scope, context.Seller, 2);
        var checkoutId = await CreateEmptyCheckout(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutArticleManuallyByUserCommand(context.Seller.IdentityId, checkoutId, context.Seller.SellerNumber, articles[0].LabelNumber);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeTrue();

        result.Errors.Any(e => e == Domain.Errors.Checkout.AlreadyBooked);
    }


    [TestMethod]
    public async Task OpenCheckout_DeleteCheckoutArticleByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(3);

        var command = new DeleteCheckoutArticleByUserCommand(context.Seller.IdentityId, checkout.Value.Id, articles[2].Id);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
        checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(2);
    }

    
    [TestMethod]
    public async Task OpenCheckout_DeleteCheckoutArticleCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(3);
        checkout.Value.Total.ShouldBe(0);

        var command = new DeleteCheckoutArticleCommand(checkoutId, articles[1].Id);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
        checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(2);
    }
    
    [TestMethod]
    public async Task CompletedCheckout_DeleteCheckoutArticleCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(3);
        checkout.Value.Total.ShouldBe(articles.Sum(a => a.Price));

        var command = new DeleteCheckoutArticleCommand(checkoutId, articles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
        checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(2);
        checkout.Value.Total.ShouldBe(articles.Skip(1).Sum(a => a.Price));
    }

    [TestMethod]
    public async Task CompletedCheckout_And_EventExpired_DeleteCheckoutArticleCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(3);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new DeleteCheckoutArticleCommand(checkoutId, articles[1].Id);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task OpenCheckout_And_EventExpired_DeleteCheckoutArticleByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateOpenCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(3);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var command = new DeleteCheckoutArticleByUserCommand(context.Seller.IdentityId, checkoutId, articles[2].Id);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CompletedCheckout_DeleteCheckoutArticleByUserCommand_IsError()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CanCreateCheckout(scope, context);
        var articles = await CreateSellerArticles(scope, context.Seller);
        var checkoutId = await CreateCompletedCheckout(scope, context.Seller);

        var checkoutRepo = scope.ServiceProvider.GetRequiredService<ICheckouts>();
        var checkout = await checkoutRepo.Find(checkoutId, _cancellationToken);
        checkout.Value.ArticleIds.Count.ShouldBe(3);

        var command = new DeleteCheckoutArticleByUserCommand(context.Seller.IdentityId, checkoutId, articles[1].Id);
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Checkout.StatusCompleted).ShouldBeTrue();
    }

    private async Task<(Domain.Models.Seller Seller, SellerRegistration Registration)> CreateEventAndSeller(IServiceScope scope, string email = _mockUserSeller)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var reg = new SellerRegistration { EventId = eventId, Name = "name", Phone = "12345", Email = email };
        var result = await sut.Send(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrations>();
        var sellerReg = await sellerRegRepo.FindByEventIdAndEmail(eventId, email, _cancellationToken);
        sellerReg.IsError.ShouldBeFalse();

        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerReg.Value.Id, ConfirmUserCallbackUrl = "http://localhost" };
        result = await sut.Send(acceptCommand, _cancellationToken);
        result.IsError.ShouldBeFalse();

        sellerReg = await sellerRegRepo.FindByEventIdAndEmail(eventId, email, _cancellationToken);
        sellerReg.IsError.ShouldBeFalse();

        var sellerRepo = scope.ServiceProvider.GetRequiredService<ISellers>();
        var seller = await sellerRepo.Find(sellerReg.Value.SellerId!.Value, _cancellationToken);
        seller.IsError.ShouldBeFalse();
        
        seller.Value.MaxArticleCount = 3;
        result = await sellerRepo.Update(seller.Value, _cancellationToken);
        result.IsError.ShouldBeFalse();

        return (seller.Value, sellerReg.Value);
    }

    private async Task<Article[]> CreateSellerArticles(IServiceScope scope, Domain.Models.Seller seller, int count = 3)
    {
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        foreach (var i in Enumerable.Range(1, count))
        {
            var c = new CreateArticleByUserCommand(seller.IdentityId, seller.Id, "foo", "bar", i);
            var r = await sut.Send(c, _cancellationToken);
            r.IsError.ShouldBeFalse();
        }

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        return await sellerArticleRepo.GetBySellerId(seller.Id, _cancellationToken);
    }

    private async Task CanCreateCheckout(IServiceScope scope, (Domain.Models.Seller seller, SellerRegistration registration) context)
    {
        var seller = context.seller;
        seller.CanCheckout = true;
        var handler = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new UpdateSellerCommand(context.registration.Id, seller.SellerNumber, seller.Role, seller.CanCheckout, null);
        var result = await handler.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();
    }

    private async Task<Guid> CreateEmptyCheckout(IServiceScope scope, Domain.Models.Seller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCheckoutByUserCommand(seller.IdentityId, seller.EventId);
        var result = await sut.Send(command, _cancellationToken);
        result.IsError.ShouldBeFalse();
        return result.Value;
    }

    private async Task<Guid> CreateOpenCheckout(IServiceScope scope, Domain.Models.Seller seller, int articleCount = 3)
    {
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var checkoutCommand = new CreateCheckoutByUserCommand(seller.IdentityId, seller.EventId);
        var checkoutResult = await sut.Send(checkoutCommand, _cancellationToken);
        checkoutResult.IsError.ShouldBeFalse();

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(seller.Id, _cancellationToken);

        foreach (var article in articles.Take(articleCount))
        {
            var command = new CreateCheckoutArticleByUserCommand(seller.IdentityId, checkoutResult.Value, article.Id);
            var result = await sut.Send(command, _cancellationToken);
            result.IsError.ShouldBeFalse();
        }

        return checkoutResult.Value;
    }

    private async Task<Guid> CreateCompletedCheckout(IServiceScope scope, Domain.Models.Seller seller, int articleCount = 3)
    {
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var checkoutCommand = new CreateCheckoutByUserCommand(seller.IdentityId, seller.EventId);
        var checkoutResult = await sut.Send(checkoutCommand, _cancellationToken);
        checkoutResult.IsError.ShouldBeFalse();

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(seller.Id, _cancellationToken);

        foreach (var article in articles.Take(articleCount))
        {
            var command = new CreateCheckoutArticleByUserCommand(seller.IdentityId, checkoutResult.Value, article.Id);
            var result = await sut.Send(command, _cancellationToken);
            result.IsError.ShouldBeFalse();
        }

        var completeCommand = new CompleteCheckoutByUserCommand(seller.IdentityId, checkoutResult.Value);
        var completeResult = await sut.Send(completeCommand, _cancellationToken);
        completeResult.IsError.ShouldBeFalse();

        return checkoutResult.Value;
    }
}
