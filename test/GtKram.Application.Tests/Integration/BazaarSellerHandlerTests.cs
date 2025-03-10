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
    private readonly ServiceFixture _fixture = new();
    private TimeProvider _mockTimeProvider = null!;
    private IServiceProvider _serviceProvider = null!;

    [TestInitialize]
    public void Init()
    {
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockTimeProvider.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);

        var mockMediator = Substitute.For<IMediator>();
        mockMediator.Send(Arg.Any<CreateUserCommand>(), default).Returns(Result.Ok(Guid.NewGuid()));
        _fixture.Services.AddScoped(_ => mockMediator);
        _fixture.Services.AddSingleton(_mockTimeProvider);

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
        mockEmailValidatorService.Validate(Arg.Any<string>(), default).Returns(true);
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
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), default);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SameUserTwoTimes_CreateSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), default);
        result.IsSuccess.ShouldBeTrue();
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), default);
        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task RegistrationLimitExceeded_CreateSellerRegistrationCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), default);
        result.IsSuccess.ShouldBeTrue();
        reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "bar", Phone = "12345", Email = "bar@bar" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), default);
        result.IsSuccess.ShouldBeTrue();
        reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "baz", Phone = "12345", Email = "baz@baz" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), default);
        result.IsSuccess.ShouldBeTrue();
        reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foobar", Phone = "12345", Email = "foo@bar" };
        result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), default);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == EventRegistration.LimitExceeded).ShouldBeTrue();
    }

    [TestMethod]
    public async Task EventExpired_CreateSellerRegistrationCommand_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        var reg = new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Phone = "12345", Email = "foo@foo" };
        var result = await sut.Handle(new CreateSellerRegistrationCommand(reg, true), default);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [TestMethod]
    public async Task AcceptSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, default))[0].Id;

        var command = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(command, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, default))[0].Id;

        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        var result = await sut.Handle(command, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteSellerRegistrationCommand_AfterAccept_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, default))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(acceptCommand, default);
        result.IsSuccess.ShouldBeTrue();

        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        result = await sut.Handle(command, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DenySellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, default))[0].Id;

        var command = new DenySellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        var result = await sut.Handle(command, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [TestMethod]
    public async Task FindSellerEventByUserQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, default))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, default);

        // query users event
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, default))[0];
        var query = new FindSellerEventByUserQuery { UserId = seller.UserId, SellerId = seller.Id };
        var result = await sut.Handle(query, default);

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
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, default))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, default);

        // query users event
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, default))[0];
        var query = new FindSellerEventByUserQuery { UserId = seller.UserId, SellerId = seller.Id };
        var result = await sut.Handle(query, default);

        // assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired);
    }

    [TestMethod]
    public async Task EditExpired_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, default))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, default);

        // query users event
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddHours(2));
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, default))[0];
        var query = new FindSellerEventByUserQuery { UserId = seller.UserId, SellerId = seller.Id };
        var result = await sut.Handle(query, default);

        // assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == SellerArticle.EditExpired);
    }

    [TestMethod]
    public async Task OtherUser_FindSellerEventByUserQuery_IsFailed()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();

        // create event and accept seller
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRepository>();
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetByBazaarEventId(eventId, default))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        await sut.Handle(acceptCommand, default);

        // query users event
        var seller = (await sellerRepo.GetByBazaarEventId(eventId, default))[0];
        var query = new FindSellerEventByUserQuery { UserId = Guid.NewGuid(), SellerId = seller.Id };
        var result = await sut.Handle(query, default);

        // assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Domain.Errors.Internal.InvalidRequest);
    }
}
