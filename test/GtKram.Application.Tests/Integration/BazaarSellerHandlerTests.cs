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
public sealed class BazaarSellerHandlerTests
{
    private readonly CancellationToken _cancellationToken;
    private readonly ServiceFixture _fixture = new();
    private TimeProvider _mockTimeProvider = null!;
    private IServiceProvider _serviceProvider = null!;

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

        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AppSettings() { HeaderTitle = "Header", Organizer = "Organizer", PublicUrl = "http://localhost", Title = "Title" }));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ConfirmEmailDataProtectionTokenProviderOptions()));
        _fixture.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions()));

        var mockUserManager = Substitute.For<MockUserManager>();
        mockUserManager.CreateAsync(Arg.Any<Infrastructure.Persistence.Entities.IdentityUserGuid>()).Returns(IdentityResult.Success);
        mockUserManager.AddToRolesAsync(Arg.Any<Infrastructure.Persistence.Entities.IdentityUserGuid>(), Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
        _fixture.Services.AddScoped<UserManager<Infrastructure.Persistence.Entities.IdentityUserGuid>>(_ => mockUserManager);
        _fixture.Services.AddScoped(_ => Substitute.For<IdentityErrorDescriber>());
        _fixture.Services.AddScoped<IUserRepository, UserRepository>();

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
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SameUserTwoTimes_CreateSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task LimitExceeded_CreateSellerRegistrationCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "bar", Phone = "12345", Email = "bar@bar" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "baz", Phone = "12345", Email = "baz@baz" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foobar", Phone = "12345", Email = "foo@bar" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == EventRegistration.LimitExceeded).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventExpired_CreateSellerRegistrationCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), _cancellationToken);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task AcceptSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;

        var command = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;

        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_AfterAccept_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(acceptCommand, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();

        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DenySellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;

        var command = new DenySellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        var result = await sut.Handle(command, _cancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task FindSellerEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // query users event
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];
        var query = new FindSellerEventByUserQuery { UserId = seller.UserId, SellerId = seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        // assert
        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventExpired_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // query users event
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];
        var query = new FindSellerEventByUserQuery { UserId = seller.UserId, SellerId = seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        // assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EditExpired_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // query users event
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddHours(2));
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];
        var query = new FindSellerEventByUserQuery { UserId = seller.UserId, SellerId = seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        // assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.EditExpired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task OtherUser_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // query users event
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];
        var query = new FindSellerEventByUserQuery { UserId = Guid.NewGuid(), SellerId = seller.Id };
        var result = await sut.Handle(query, _cancellationToken);

        // assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Internal.InvalidRequest).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CreateSellerArticleByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create article
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];
        var command = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", 1);
        var result = await sut.Handle(command, _cancellationToken);

        // assert
        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task MaxExceeded_CreateSellerArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create max articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];
        seller.MaxArticleCount = 3;
        await sellerRepo.Update(seller, _cancellationToken);

        foreach (var i in Enumerable.Range(0, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", 1);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // check max articles
        var command = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", 1);
        var result = await sut.Handle(command, _cancellationToken);
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.MaxExceeded).ShouldBeTrue();
    }

    [TestMethod]
    public async Task CreateSellerArticleByUserCommand_ValidateArticles_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // check articles
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
        articles.Select(a => a.LabelNumber).Distinct().Count().ShouldBe(3);
        articles.Sum(a => a.Price).ShouldBe(1 + 2 + 3);
    }

    [TestMethod]
    public async Task DeleteSellerArticleByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // delete article
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
        var command = new DeleteSellerArticleByUserCommand(seller.UserId, articles[0].Id);

        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task EditExpired_DeleteSellerArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // delete article
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(1));

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
        var command = new DeleteSellerArticleByUserCommand(seller.UserId, articles[0].Id);

        var result = await sut.Handle(command, _cancellationToken);
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.EditExpired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task UpdateSellerArticleByUserCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // update article
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
        var command = new UpdateSellerArticleByUserCommand(seller.UserId, articles[0].Id, "bar", "baz", 10);

        var result = await sut.Handle(command, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
        articles.Select(a => a.LabelNumber).Distinct().Count().ShouldBe(3);
        articles.Sum(a => a.Price).ShouldBeGreaterThan(10);
    }

    [TestMethod]
    public async Task EditExpired_UpdateSellerArticleByUserCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // update article
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(1));

        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
        var command = new UpdateSellerArticleByUserCommand(seller.UserId, articles[0].Id, "bar", "baz", 10);

        var result = await sut.Handle(command, _cancellationToken);
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.EditExpired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task FindSellerArticleByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // query article
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
        var query = new FindSellerArticleByUserQuery(seller.UserId, articles[0].Id);

        var result = await sut.Handle(query, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(eventId);
        result.Value.IsBooked.ShouldBeFalse();
    }

    [TestMethod]
    public async Task FindSellerWithEventAndArticlesByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // query articles
        var sellerArticleRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerArticleRepository>();
        var articles = await sellerArticleRepo.GetByBazaarSellerId(seller.Id, _cancellationToken);
        var query = new FindSellerWithEventAndArticlesByUserQuery(seller.UserId, seller.Id);

        var result = await sut.Handle(query, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Event.Id.ShouldBe(eventId);
        result.Value.Seller.Id.ShouldBe(seller.Id);
        result.Value.Articles.Select(a => a.SellerArticle.Id).SequenceEqual(articles.Select(a => a.Id)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GetEventsWithSellerAndArticleCountByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // query articles
        var query = new GetEventsWithSellerAndArticleCountByUserQuery(seller.UserId);
        var result = await sut.Handle(query, _cancellationToken);
        result.Length.ShouldBe(1);
        result[0].Event.Id.ShouldBe(eventId);
        result[0].Seller.Id.ShouldBe(seller.Id);
        result[0].ArticleCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task FindSellerWithRegistrationAndArticlesQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, _cancellationToken);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, _cancellationToken))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, _cancellationToken);

        // create some articles
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, _cancellationToken))[0];

        foreach (var i in Enumerable.Range(1, 3))
        {
            var c = new CreateSellerArticleByUserCommand(seller.UserId, seller.Id, "foo", "bar", i);
            var r = await sut.Handle(c, _cancellationToken);
            r.IsSuccess.ShouldBeTrue();
        }

        // query articles
        var query = new FindSellerWithRegistrationAndArticlesQuery(seller.Id);
        var result = await sut.Handle(query, _cancellationToken);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Seller.Id.ShouldBe(seller.Id);
        result.Value.Registration.Id.ShouldBe(sellerRegId);
        result.Value.Articles.Length.ShouldBe(3);
    }
}
