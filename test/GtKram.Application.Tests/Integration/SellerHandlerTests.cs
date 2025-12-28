using FluentMigrator.Runner;
using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Handlers;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Errors;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Email;
using GtKram.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public sealed class SellerHandlerTests
{
    private const string _mockUserEmail = "foo@bar.baz";

    private readonly CancellationToken _cancellationToken;
    private readonly ServiceFixture _fixture = new();
    private TimeProvider _mockTimeProvider = null!;
    private IServiceProvider _serviceProvider = null!;
   

    public SellerHandlerTests(TestContext context)
    {
        _cancellationToken = context.CancellationTokenSource.Token;
    }

    [TestInitialize]
    public async Task Init()
    {
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockTimeProvider.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);
        _fixture.Services.AddSingleton(_mockTimeProvider);

        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AppSettings() { HeaderTitle = "Header", Organizer = "Organizer", PublicUrl = "http://localhost", Title = "Title", RegisterRulesUrl = "http://localhost" }));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ConfirmEmailDataProtectionTokenProviderOptions()));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions()));

        var mockEmailValidatorService = Substitute.For<IEmailValidatorService>();
        mockEmailValidatorService.Validate(Arg.Any<string>(), _cancellationToken).Returns(true);
        _fixture.Services.AddScoped(_ => mockEmailValidatorService);
        _fixture.Services.AddScoped<IEmailService, EmailService>();

        _fixture.Services.AddScoped<EmailQueues>();
        _fixture.Services.AddScoped<IUsers, Users>();
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
        var result = await users.Create("foo", _mockUserEmail, [UserRoleType.Manager], _cancellationToken);
        var identity = await userManager.FindByEmailAsync(_mockUserEmail);
        identity!.Json.IsEmailConfirmed = true;
        await userManager.UpdateAsync(identity);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task CreateSellerRegistrationCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrations>();
        var sellerReg = await sellerRegRepo.FindByEventIdAndEmail(context.EventId, _mockUserEmail, _cancellationToken);

        sellerReg.IsError.ShouldBeFalse();
        sellerReg.Value.PreferredType.ShouldBe(SellerRegistrationPreferredType.Kita);
        sellerReg.Value.ClothingType!.Length.ShouldBe(2);
    }

    [TestMethod]
    public async Task SameUserTwoTimes_CreateSellerRegistrationCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var result = await sut.Handle(new CreateSellerRegistrationCommand(context, true), _cancellationToken);
        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task LimitExceeded_CreateSellerRegistrationCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var reg = new Domain.Models.SellerRegistration { EventId = context.EventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsError.ShouldBeFalse();
        reg = new Domain.Models.SellerRegistration { EventId = context.EventId, Name = "bar", Phone = "12345", Email = "bar@bar" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsError.ShouldBeFalse();
        reg = new Domain.Models.SellerRegistration { EventId = context.EventId, Name = "baz", Phone = "12345", Email = "baz@baz" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == Domain.Errors.SellerRegistration.LimitExceeded.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventExpired_CreateSellerRegistrationCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var reg = new Domain.Models.SellerRegistration { EventId = context.EventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == Domain.Errors.Event.Expired.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task AcceptSellerRegistrationCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new AcceptSellerRegistrationCommand { SellerRegistrationId = context.Id, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = context.Id };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_AfterAccept_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = context.Id, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(acceptCommand, _cancellationToken);
        result.IsError.ShouldBeFalse();
        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = context.Id };
        result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task DenySellerRegistrationCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new DenySellerRegistrationCommand { SellerRegistrationId = context.Id };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task FindSellerEventByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new FindSellerEventByUserQuery { UserId = context.Seller.IdentityId, SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task EventExpired_FindSellerEventByUserQuery_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        var query = new FindSellerEventByUserQuery { UserId = context.Seller.IdentityId, SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == Domain.Errors.Event.Expired.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EditExpired_FindSellerEventByUserQuery_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddHours(2));
        var query = new FindSellerEventByUserQuery { UserId = context.Seller.IdentityId, SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == SellerArticle.EditExpired.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task OtherUser_FindSellerEventByUserQuery_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new FindSellerEventByUserQuery { UserId = Guid.NewGuid(), SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == Domain.Errors.Internal.InvalidRequest.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CreateSellerArticleByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new CreateArticleByUserCommand(context.Seller.IdentityId, context.Seller.Id, "foo", "bar", 1);

        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task MaxExceeded_CreateSellerArticleByUserCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller); 

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new CreateArticleByUserCommand(context.Seller.IdentityId, context.Seller.Id, "foo", "bar", 1);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == SellerArticle.MaxExceeded.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CreateSellerArticleByUserCommand_ValidateArticles_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);

        articles.Select(a => a.LabelNumber).Distinct().Count().ShouldBe(3);
        articles.Sum(a => a.Price).ShouldBe(1 + 2 + 3);
    }

    [TestMethod]
    public async Task DeleteSellerArticleByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var command = new DeleteArticleByUserCommand(context.Seller.IdentityId, articles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task EditExpired_DeleteSellerArticleByUserCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(1));
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var command = new DeleteArticleByUserCommand(context.Seller.IdentityId, articles[0].Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == SellerArticle.EditExpired.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UpdateSellerArticleByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var command = new UpdateArticleByUserCommand(context.Seller.IdentityId, articles[0].Id, "bar", "baz", 10);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
        articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        articles.Select(a => a.LabelNumber).Distinct().Count().ShouldBe(3);
        articles.Sum(a => a.Price).ShouldBeGreaterThan(10);
    }

    [TestMethod]
    public async Task EditExpired_UpdateSellerArticleByUserCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(1));
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var command = new UpdateArticleByUserCommand(context.Seller.IdentityId, articles[0].Id, "bar", "baz", 10);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == SellerArticle.EditExpired.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task FindSellerArticleByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var query = new FindArticleByUserQuery(context.Seller.IdentityId, articles[0].Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.HasBooked.ShouldBeFalse();
    }

    [TestMethod]
    public async Task EmptyArticles_FindSellerWithEventAndArticlesByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var query = new FindSellerWithEventAndArticlesByUserQuery(context.Seller.IdentityId, context.Seller.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Seller.Id.ShouldBe(context.Seller.Id);
        result.Value.Articles.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task FindSellerWithEventAndArticlesByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var query = new FindSellerWithEventAndArticlesByUserQuery(context.Seller.IdentityId, context.Seller.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Seller.Id.ShouldBe(context.Seller.Id);
        result.Value.Articles.Select(a => a.Article.Id).SequenceEqual(articles.Select(a => a.Id)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyArticles_GetEventsWithSellerAndArticleCountByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new GetEventsWithSellerAndArticleCountByUserQuery(context.Seller.IdentityId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].Seller.Id.ShouldBe(context.Seller.Id);
        result[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetEventsWithSellerAndArticleCountByUserQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new GetEventsWithSellerAndArticleCountByUserQuery(context.Seller.IdentityId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].Seller.Id.ShouldBe(context.Seller.Id);
        result[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task FindSellerWithRegistrationAndArticlesQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new FindSellerWithRegistrationAndArticlesQuery(context.Registration.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Seller.Id.ShouldBe(context.Seller.Id);
        result.Value.Registration.EventId.ShouldBe(context.Seller.EventId);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task GetSellerRegistrationWithArticleCountQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new GetSellerRegistrationWithArticleCountQuery(context.Seller.EventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Seller!.Id.ShouldBe(context.Seller.Id);
        result[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task FindRegistrationWithSellerQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrations>();
        var sellerReg = await sellerRegRepo.FindBySellerId(context.Seller.Id, _cancellationToken);
        sellerReg.IsError.ShouldBeFalse();

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new FindRegistrationWithSellerQuery(sellerReg.Value.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Seller.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task TakeOverSellerArticlesByUserCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.IdentityId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task MaxExceeded_TakeOverSellerArticlesByUserCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.IdentityId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsError.ShouldBeFalse();

        result = await sut.Handle(command, _cancellationToken);
        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == SellerArticle.MaxExceeded.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyArticles_TakeOverSellerArticlesByUserCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.IdentityId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == SellerArticle.Empty.Code).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SoldArticles_TakeOverSellerArticlesByUserCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);
        await CanCreateCheckout(scope, context);
        await CreateCompletedCheckout(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.IdentityId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsError.ShouldBeTrue();
        result.Errors.Any(e => e.Code == SellerArticle.Empty.Code).ShouldBeTrue();
    }

    private async Task CanCreateCheckout(IServiceScope scope, (Domain.Models.Seller seller, Domain.Models.SellerRegistration registration) context)
    {
        var seller = context.seller;
        seller.CanCheckout = true;
        var handler = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new UpdateSellerCommand(context.registration.Id, seller.SellerNumber, seller.Role, seller.CanCheckout);
        var result = await handler.Handle(command, _cancellationToken);
        result.IsError.ShouldBeFalse();
    }

    private async Task<Guid> CreateCompletedCheckout(IServiceScope scope, Domain.Models.Seller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<CheckoutHandler>();
        var checkoutCommand = new CreateCheckoutByUserCommand(seller.IdentityId, seller.EventId);
        var checkoutResult = await sut.Handle(checkoutCommand, _cancellationToken);
        checkoutResult.IsError.ShouldBeFalse();

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticles>();
        var articles = await sellerArticleRepo.GetBySellerId(seller.Id, _cancellationToken);

        foreach (var article in articles)
        {
            var command = new CreateCheckoutArticleByUserCommand(seller.IdentityId, checkoutResult.Value, article.Id);
            var result = await sut.Handle(command, _cancellationToken);
            result.IsError.ShouldBeFalse();
        }

        var completeCommand = new CompleteCheckoutByUserCommand(seller.IdentityId, checkoutResult.Value);
        var completeResult = await sut.Handle(completeCommand, _cancellationToken);
        completeResult.IsError.ShouldBeFalse();

        return checkoutResult.Value;
    }

    private async Task<Domain.Models.SellerRegistration> CreateEventAndRegistration(IServiceScope scope)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var reg = new Domain.Models.SellerRegistration { EventId = eventId, Name = "foo", Phone = "12345", Email = _mockUserEmail, ClothingType = [0,1], PreferredType = SellerRegistrationPreferredType.Kita };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrations>();
        var sellerReg = await sellerRegRepo.FindByEventIdAndEmail(eventId, _mockUserEmail, _cancellationToken);
        sellerReg.IsError.ShouldBeFalse();

        return sellerReg.Value;
    }

    private async Task<(Domain.Models.Seller Seller, Domain.Models.SellerRegistration Registration)> CreateEventAndSeller(IServiceScope scope)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var reg = new Domain.Models.SellerRegistration { EventId = eventId, Name = "foo", Phone = "12345", Email = _mockUserEmail };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrations>();
        var sellerReg = await sellerRegRepo.FindByEventIdAndEmail(eventId, _mockUserEmail, _cancellationToken);
        sellerReg.IsError.ShouldBeFalse();

        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerReg.Value.Id, ConfirmUserCallbackUrl = "http://localhost" };
        result = await sut.Handle(acceptCommand, _cancellationToken);
        result.IsError.ShouldBeFalse();

        sellerReg = await sellerRegRepo.FindByEventIdAndEmail(eventId, _mockUserEmail, _cancellationToken);
        sellerReg.IsError.ShouldBeFalse();

        var sellerRepo = scope.ServiceProvider.GetRequiredService<ISellers>();
        var seller = await sellerRepo.Find(sellerReg.Value.SellerId!.Value, _cancellationToken);
        seller.IsError.ShouldBeFalse();
;
        seller.Value.MaxArticleCount = 3;
        result = await sellerRepo.Update(seller.Value, _cancellationToken);
        result.IsError.ShouldBeFalse();

        return (seller.Value, sellerReg.Value);
    }

    private async Task CreateArticles(IServiceScope scope, Domain.Models.Seller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateArticleByUserCommand(seller.IdentityId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsError.ShouldBeFalse();
        }
    }
}
