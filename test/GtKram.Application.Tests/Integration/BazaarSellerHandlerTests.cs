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
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public sealed class BazaarSellerHandlerTests
{
    private readonly CancellationToken _cancellationToken;
    private readonly ServiceFixture _fixture = new();
    private TimeProvider _mockTimeProvider = null!;
    private IServiceProvider _serviceProvider = null!;

    private readonly User _mockUser = new()
    {
        Id = Guid.NewGuid(),
        Name = "Foo",
        Email = "foo@bar.baz",
        Roles = [UserRoleType.Seller],
        IsEmailConfirmed = true
    };

    public BazaarSellerHandlerTests(TestContext context)
    {
        _cancellationToken = context.CancellationTokenSource.Token;
    }

    [TestInitialize]
    public void Init()
    {
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockTimeProvider.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);
        _fixture.Services.AddSingleton(_mockTimeProvider);

        var mockMediator = Substitute.For<IMediator>();
        mockMediator.Send(Arg.Any<CreateUserCommand>(), _cancellationToken).Returns(Result.Ok(Guid.NewGuid()));
        _fixture.Services.AddScoped(_ => mockMediator);

        var mockUserRepo = Substitute.For<IUserRepository>();
        mockUserRepo.Create(Arg.Any<string>(), _mockUser.Email, Arg.Any<UserRoleType[]>(), Arg.Any<CancellationToken>()).Returns(_mockUser.Id);
        mockUserRepo.FindByEmail(_mockUser.Email!, Arg.Any<CancellationToken>()).Returns(Result.Ok(_mockUser));
        mockUserRepo.FindById(_mockUser.Id, Arg.Any<CancellationToken>()).Returns(Result.Ok(_mockUser));

        _fixture.Services.AddScoped(_ => mockUserRepo);
        _fixture.Services.AddScoped(_ => Substitute.For<IdentityErrorDescriber>());

        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AppSettings() { HeaderTitle = "Header", Organizer = "Organizer", PublicUrl = "http://localhost", Title = "Title" }));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ConfirmEmailDataProtectionTokenProviderOptions()));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions()));

        var mockEmailValidatorService = Substitute.For<IEmailValidatorService>();
        mockEmailValidatorService.Validate(Arg.Any<string>(), _cancellationToken).Returns(true);
        _fixture.Services.AddScoped(_ => mockEmailValidatorService);
        _fixture.Services.AddScoped<EmailQueueRepository>();

        _fixture.Services.AddScoped<IBazaarEventRepository, BazaarEventRepository>();
        _fixture.Services.AddScoped<IBazaarSellerRegistrationRepository, BazaarSellerRegistrationRepository>();
        _fixture.Services.AddScoped<IEmailService, EmailService>();
        _fixture.Services.AddScoped<IBazaarSellerRepository, BazaarSellerRepository>();
        _fixture.Services.AddScoped<IBazaarSellerArticleRepository, BazaarSellerArticleRepository>();
        _fixture.Services.AddScoped<IBazaarBillingRepository, BazaarBillingRepository>();
        _fixture.Services.AddScoped<IBazaarBillingArticleRepository, BazaarBillingArticleRepository>();

        _fixture.Services.AddScoped<BazaarSellerHandler>();
        _fixture.Services.AddScoped<BazaarBillingHandler>();

        _serviceProvider = _fixture.Build();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture.Dispose();
    }

    [TestMethod]
    public async Task CreateSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        var sellerReg = await sellerRegRepo.FindByEmailAndBazaarEventId(_mockUser.Email, context.BazaarEventId, _cancellationToken);

        sellerReg.IsSuccess.ShouldBeTrue();
        sellerReg.Value.PreferredType.ShouldBe(SellerRegistrationPreferredType.Kita);
        sellerReg.Value.ClothingType!.Length.ShouldBe(2);
    }

    [TestMethod]
    public async Task SameUserTwoTimes_CreateSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var result = await sut.Handle(new CreateSellerRegistrationCommand(context, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task LimitExceeded_CreateSellerRegistrationCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var reg = new BazaarSellerRegistration { BazaarEventId = context.BazaarEventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        reg = new BazaarSellerRegistration { BazaarEventId = context.BazaarEventId, Name = "bar", Phone = "12345", Email = "bar@bar" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        reg = new BazaarSellerRegistration { BazaarEventId = context.BazaarEventId, Name = "baz", Phone = "12345", Email = "baz@baz" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == EventRegistration.LimitExceeded).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventExpired_CreateSellerRegistrationCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var reg = new BazaarSellerRegistration { BazaarEventId = context.BazaarEventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task AcceptSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new AcceptSellerRegistrationCommand { SellerRegistrationId = context.Id, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = context.Id };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_AfterAccept_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = context.Id, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(acceptCommand, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = context.Id };
        result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DenySellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new DenySellerRegistrationCommand { SellerRegistrationId = context.Id };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task FindSellerEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var query = new FindSellerEventByUserQuery { UserId = context.Seller.UserId, SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventExpired_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        var query = new FindSellerEventByUserQuery { UserId = context.Seller.UserId, SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EditExpired_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddHours(2));
        var query = new FindSellerEventByUserQuery { UserId = context.Seller.UserId, SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.EditExpired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task OtherUser_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var query = new FindSellerEventByUserQuery { UserId = Guid.NewGuid(), SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Internal.InvalidRequest).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CreateSellerArticleByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new CreateSellerArticleByUserCommand(context.Seller.UserId, context.Seller.Id, "foo", "bar", 1);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task MaxExceeded_CreateSellerArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller); 

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new CreateSellerArticleByUserCommand(context.Seller.UserId, context.Seller.Id, "foo", "bar", 1);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.MaxExceeded).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CreateSellerArticleByUserCommand_ValidateArticles_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(context.Seller.Id, _cancellationToken);

        articles.Select(a => a.LabelNumber).Distinct().Count().ShouldBe(3);
        articles.Sum(a => a.Price).ShouldBe(1 + 2 + 3);
    }

    [TestMethod]
    public async Task DeleteSellerArticleByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(context.Seller.Id, _cancellationToken);
        var command = new DeleteSellerArticleByUserCommand(context.Seller.UserId, articles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task EditExpired_DeleteSellerArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(1));
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(context.Seller.Id, _cancellationToken);
        var command = new DeleteSellerArticleByUserCommand(context.Seller.UserId, articles[0].Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.EditExpired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UpdateSellerArticleByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(context.Seller.Id, _cancellationToken);
        var command = new UpdateSellerArticleByUserCommand(context.Seller.UserId, articles[0].Id, "bar", "baz", 10);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        articles = await sellerArticleRepo.GetByBazaarSellerId(context.Seller.Id, _cancellationToken);
        articles.Select(a => a.LabelNumber).Distinct().Count().ShouldBe(3);
        articles.Sum(a => a.Price).ShouldBeGreaterThan(10);
    }

    [TestMethod]
    public async Task EditExpired_UpdateSellerArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(1));
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(context.Seller.Id, _cancellationToken);
        var command = new UpdateSellerArticleByUserCommand(context.Seller.UserId, articles[0].Id, "bar", "baz", 10);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.EditExpired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task FindSellerArticleByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(context.Seller.Id, _cancellationToken);
        var query = new FindSellerArticleByUserQuery(context.Seller.UserId, articles[0].Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.IsBooked.ShouldBeFalse();
    }

    [TestMethod]
    public async Task FindSellerWithEventAndArticlesByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(context.Seller.Id, _cancellationToken);
        var query = new FindSellerWithEventAndArticlesByUserQuery(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Seller.Id.ShouldBe(context.Seller.Id);
        result.Value.Articles.Select(a => a.SellerArticle.Id).SequenceEqual(articles.Select(a => a.Id)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyArticles_GetEventsWithSellerAndArticleCountByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var query = new GetEventsWithSellerAndArticleCountByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].Seller.Id.ShouldBe(context.Seller.Id);
        result[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetEventsWithSellerAndArticleCountByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var query = new GetEventsWithSellerAndArticleCountByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.BazaarEventId);
        result[0].Seller.Id.ShouldBe(context.Seller.Id);
        result[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task FindSellerWithRegistrationAndArticlesQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var query = new FindSellerWithRegistrationAndArticlesQuery(context.Seller.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Seller.Id.ShouldBe(context.Seller.Id);
        result.Value.Registration.BazaarEventId.ShouldBe(context.Seller.BazaarEventId);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task GetSellerRegistrationWithArticleCountQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var query = new GetSellerRegistrationWithArticleCountQuery(context.Seller.BazaarEventId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Seller!.Id.ShouldBe(context.Seller.Id);
        result[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task FindRegistrationWithSellerQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        var sellerReg = await sellerRegRepo.FindByBazaarSellerId(context.Seller.Id, _cancellationToken);
        sellerReg.IsSuccess.ShouldBeTrue();

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var query = new FindRegistrationWithSellerQuery(sellerReg.Value.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Seller.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task TakeOverSellerArticlesByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task MaxExceeded_TakeOverSellerArticlesByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        result = await sut.Handle(command, _cancellationToken);
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.MaxExceeded).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyArticles_TakeOverSellerArticlesByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.IsEmpty).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SoldArticles_TakeOverSellerArticlesByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);
        await CanCreateBillings(scope, context);
        await CreateCompletedBilling(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.IsEmpty).ShouldBeTrue();
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

    private async Task<Guid> CreateCompletedBilling(IServiceScope scope, BazaarSeller seller)
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

        var completeCommand = new CompleteBillingByUserCommand(seller.UserId, billingResult.Value);
        var completeResult = await sut.Handle(completeCommand, _cancellationToken);
        completeResult.IsSuccess.ShouldBeTrue();

        return billingResult.Value;
    }

    private async Task<BazaarSellerRegistration> CreateEventAndRegistration(IServiceScope scope)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = _mockUser.Email, ClothingType = [0,1], PreferredType = SellerRegistrationPreferredType.Kita };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        var sellerReg = await sellerRegRepo.FindByEmailAndBazaarEventId(_mockUser.Email, eventId, _cancellationToken);
        sellerReg.IsSuccess.ShouldBeTrue();

        return sellerReg.Value;
    }

    private async Task<(BazaarSeller Seller, BazaarSellerRegistration Registration)> CreateEventAndSeller(IServiceScope scope)
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
;
        seller.Value.MaxArticleCount = 3;
        result = await sellerRepo.Update(seller.Value, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        return (seller.Value, sellerReg.Value);
    }

    private async Task CreateArticles(IServiceScope scope, BazaarSeller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }
    }
}
