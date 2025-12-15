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
        _fixture.Services.AddScoped<EmailQueueRepository>();

        _fixture.Services.AddScoped<IUserRepository, UserRepository>();

        _fixture.Services.AddScoped<IEventRepository, EventRepository>();
        _fixture.Services.AddScoped<ISellerRegistrationRepository, SellerRegistrationRepository>();
        _fixture.Services.AddScoped<IEmailService, EmailService>();
        _fixture.Services.AddScoped<ISellerRepository, SellerRepository>();
        _fixture.Services.AddScoped<IArticleRepository, ArticleRepository>();
        _fixture.Services.AddScoped<ICheckoutRepository, CheckoutRepository>();

        _serviceProvider = _fixture.Build();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Infrastructure.Database.Entities.Identity>>();
        var result = await userRepo.Create("foo", _mockUserEmail, [UserRoleType.Manager], _cancellationToken);
        var identity = await userManager.FindByEmailAsync(_mockUserEmail);
        identity!.IsEmailConfirmed = true;
        await userManager.UpdateAsync(identity);
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

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrationRepository>();
        var sellerReg = await sellerRegRepo.FindByEventIdAndEmail(context.EventId, _mockUserEmail, _cancellationToken);

        sellerReg.IsSuccess.ShouldBeTrue();
        sellerReg.Value.PreferredType.ShouldBe(SellerRegistrationPreferredType.Kita);
        sellerReg.Value.ClothingType!.Length.ShouldBe(2);
    }

    [TestMethod]
    public async Task SameUserTwoTimes_CreateSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var result = await sut.Handle(new CreateSellerRegistrationCommand(context, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task LimitExceeded_CreateSellerRegistrationCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var reg = new Domain.Models.SellerRegistration { EventId = context.EventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        reg = new Domain.Models.SellerRegistration { EventId = context.EventId, Name = "bar", Phone = "12345", Email = "bar@bar" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        reg = new Domain.Models.SellerRegistration { EventId = context.EventId, Name = "baz", Phone = "12345", Email = "baz@baz" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.SellerRegistration.LimitExceeded).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventExpired_CreateSellerRegistrationCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var reg = new Domain.Models.SellerRegistration { EventId = context.EventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task AcceptSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new AcceptSellerRegistrationCommand { SellerRegistrationId = context.Id, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = context.Id };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_AfterAccept_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndRegistration(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
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

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new DenySellerRegistrationCommand { SellerRegistrationId = context.Id };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task FindSellerEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new FindSellerEventByUserQuery { UserId = context.Seller.UserId, SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventExpired_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        var query = new FindSellerEventByUserQuery { UserId = context.Seller.UserId, SellerId = context.Seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EditExpired_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
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

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
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

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new CreateArticleByUserCommand(context.Seller.UserId, context.Seller.Id, "foo", "bar", 1);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task MaxExceeded_CreateSellerArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller); 

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new CreateArticleByUserCommand(context.Seller.UserId, context.Seller.Id, "foo", "bar", 1);
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

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);

        articles.Select(a => a.LabelNumber).Distinct().Count().ShouldBe(3);
        articles.Sum(a => a.Price).ShouldBe(1 + 2 + 3);
    }

    [TestMethod]
    public async Task DeleteSellerArticleByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var command = new DeleteArticleByUserCommand(context.Seller.UserId, articles[0].Id);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
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
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var command = new DeleteArticleByUserCommand(context.Seller.UserId, articles[0].Id);
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

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var command = new UpdateArticleByUserCommand(context.Seller.UserId, articles[0].Id, "bar", "baz", 10);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        articles.Select(a => a.LabelNumber).Distinct().Count().ShouldBe(3);
        articles.Sum(a => a.Price).ShouldBeGreaterThan(10);
    }

    [TestMethod]
    public async Task EditExpired_UpdateSellerArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(1));
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var command = new UpdateArticleByUserCommand(context.Seller.UserId, articles[0].Id, "bar", "baz", 10);
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

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var query = new FindArticleByUserQuery(context.Seller.UserId, articles[0].Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.HasBooked.ShouldBeFalse();
    }

    [TestMethod]
    public async Task EmptyArticles_FindSellerWithEventAndArticlesByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var query = new FindSellerWithEventAndArticlesByUserQuery(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Seller.Id.ShouldBe(context.Seller.Id);
        result.Value.Articles.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task FindSellerWithEventAndArticlesByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(context.Seller.Id, _cancellationToken);
        var query = new FindSellerWithEventAndArticlesByUserQuery(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(context.Seller.EventId);
        result.Value.Seller.Id.ShouldBe(context.Seller.Id);
        result.Value.Articles.Select(a => a.Article.Id).SequenceEqual(articles.Select(a => a.Id)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EmptyArticles_GetEventsWithSellerAndArticleCountByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new GetEventsWithSellerAndArticleCountByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].Seller.Id.ShouldBe(context.Seller.Id);
        result[0].ArticleCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task GetEventsWithSellerAndArticleCountByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new GetEventsWithSellerAndArticleCountByUserQuery(context.Seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);

        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(context.Seller.EventId);
        result[0].Seller.Id.ShouldBe(context.Seller.Id);
        result[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task FindSellerWithRegistrationAndArticlesQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var query = new FindSellerWithRegistrationAndArticlesQuery(context.Registration.Id);
        var result = await sut.Handle(query, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Seller.Id.ShouldBe(context.Seller.Id);
        result.Value.Registration.EventId.ShouldBe(context.Seller.EventId);
        result.Value.Articles.Length.ShouldBe(3);
    }

    [TestMethod]
    public async Task GetSellerRegistrationWithArticleCountQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
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
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrationRepository>();
        var sellerReg = await sellerRegRepo.FindBySellerId(context.Seller.Id, _cancellationToken);
        sellerReg.IsSuccess.ShouldBeTrue();

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
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
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
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
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
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
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.Empty).ShouldBeTrue();
    }

    [TestMethod]
    public async Task SoldArticles_TakeOverSellerArticlesByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var context = await CreateEventAndSeller(scope);
        await CreateArticles(scope, context.Seller);
        await CanCreateCheckout(scope, context);
        await CreateCompletedCheckout(scope, context.Seller);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddYears(1));
        context = await CreateEventAndSeller(scope);
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new TakeOverSellerArticlesByUserCommand(context.Seller.UserId, context.Seller.Id);
        var result = await sut.Handle(command, _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.Empty).ShouldBeTrue();
    }

    private async Task CanCreateCheckout(IServiceScope scope, (Domain.Models.Seller seller, Domain.Models.SellerRegistration registration) context)
    {
        var seller = context.seller;
        seller.CanCheckout = true;
        var handler = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var command = new UpdateSellerCommand(context.registration.Id, seller.SellerNumber, seller.Role, seller.CanCheckout);
        var result = await handler.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    private async Task<Guid> CreateCompletedCheckout(IServiceScope scope, Domain.Models.Seller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<CheckoutHandler>();
        var checkoutCommand = new CreateCheckoutByUserCommand(seller.UserId, seller.EventId);
        var checkoutResult = await sut.Handle(checkoutCommand, _cancellationToken);
        checkoutResult.IsSuccess.ShouldBeTrue();

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IArticleRepository>();
        var articles = await sellerArticleRepo.GetBySellerId(seller.Id, _cancellationToken);

        foreach (var article in articles)
        {
            var command = new CreateCheckoutArticleByUserCommand(seller.UserId, checkoutResult.Value, article.Id);
            var result = await sut.Handle(command, _cancellationToken);
            result.IsSuccess.ShouldBeTrue();
        }

        var completeCommand = new CompleteCheckoutByUserCommand(seller.UserId, checkoutResult.Value);
        var completeResult = await sut.Handle(completeCommand, _cancellationToken);
        completeResult.IsSuccess.ShouldBeTrue();

        return checkoutResult.Value;
    }

    private async Task<Domain.Models.SellerRegistration> CreateEventAndRegistration(IServiceScope scope)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var reg = new Domain.Models.SellerRegistration { EventId = eventId, Name = "foo", Phone = "12345", Email = _mockUserEmail, ClothingType = [0,1], PreferredType = SellerRegistrationPreferredType.Kita };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrationRepository>();
        var sellerReg = await sellerRegRepo.FindByEventIdAndEmail(eventId, _mockUserEmail, _cancellationToken);
        sellerReg.IsSuccess.ShouldBeTrue();

        return sellerReg.Value;
    }

    private async Task<(Domain.Models.Seller Seller, Domain.Models.SellerRegistration Registration)> CreateEventAndSeller(IServiceScope scope)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();
        var reg = new Domain.Models.SellerRegistration { EventId = eventId, Name = "foo", Phone = "12345", Email = _mockUserEmail };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrationRepository>();
        var sellerReg = await sellerRegRepo.FindByEventIdAndEmail(eventId, _mockUserEmail, _cancellationToken);
        sellerReg.IsSuccess.ShouldBeTrue();

        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerReg.Value.Id, ConfirmUserCallbackUrl = "http://localhost" };
        result = await sut.Handle(acceptCommand, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        sellerReg = await sellerRegRepo.FindByEventIdAndEmail(eventId, _mockUserEmail, _cancellationToken);
        sellerReg.IsSuccess.ShouldBeTrue();

        var sellerRepo = scope.ServiceProvider.GetRequiredService<ISellerRepository>();
        var seller = await sellerRepo.Find(sellerReg.Value.SellerId!.Value, _cancellationToken);
        seller.IsSuccess.ShouldBeTrue();
;
        seller.Value.MaxArticleCount = 3;
        result = await sellerRepo.Update(seller.Value, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        return (seller.Value, sellerReg.Value);
    }

    private async Task CreateArticles(IServiceScope scope, Domain.Models.Seller seller)
    {
        var sut = scope.ServiceProvider.GetRequiredService<SellerHandler>();

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }
    }
}
