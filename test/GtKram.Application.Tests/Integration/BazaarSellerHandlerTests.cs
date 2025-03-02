using GtKram.Application.Options;
using GtKram.Application.Services;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Handlers;
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
using System.Threading.Tasks;

namespace GtKram.Application.Tests.Integration;

public sealed class BazaarSellerHandlerTests : DatabaseFixture
{
    private TimeProvider _mockTimeProvider = null!;

    protected override void Setup(IServiceCollection services)
    {
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockTimeProvider.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);

        var mockMediator = Substitute.For<IMediator>();
        mockMediator.Send(Arg.Any<CreateUserCommand>(), default).Returns(Result.Ok(Guid.NewGuid()));
        services.AddScoped(_ => mockMediator);
        services.AddSingleton(_mockTimeProvider);

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new AppSettings() { HeaderTitle = "Header", Organizer = "Organizer", PublicUrl = "http://localhost", Title = "Title" }));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ConfirmEmailDataProtectionTokenProviderOptions()));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions()));

        var mockUserManager = Substitute.For<MockUserManager>();
        mockUserManager.CreateAsync(Arg.Any<Infrastructure.Persistence.Entities.IdentityUserGuid>()).Returns(IdentityResult.Success);
        mockUserManager.AddToRolesAsync(Arg.Any<Infrastructure.Persistence.Entities.IdentityUserGuid>(), Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
        services.AddScoped<UserManager<Infrastructure.Persistence.Entities.IdentityUserGuid>>(_ => mockUserManager);
        services.AddScoped(_ => Substitute.For<IdentityErrorDescriber>());
        services.AddScoped<IUserRepository, UserRepository>();

        var mockEmailValidatorService = Substitute.For<IEmailValidatorService>();
        mockEmailValidatorService.Validate(Arg.Any<string>(), default).Returns(true);
        services.AddScoped(_ => mockEmailValidatorService);
        services.AddScoped<EmailQueueRepository>();

        services.AddScoped<IBazaarEventRepository, BazaarEventRepository>();
        services.AddScoped<IBazaarSellerRegistrationRepository, BazaarSellerRegistrationRepository>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IBazaarSellerRepository, BazaarSellerRepository>();
        services.AddScoped<IBazaarSellerArticleRepository, BazaarSellerArticleRepository>();
        services.AddScoped<IBazaarBillingRepository, BazaarBillingRepository>();
        services.AddScoped<IBazaarBillingArticleRepository, BazaarBillingArticleRepository>();

        services.AddScoped<BazaarSellerHandler>();
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public async Task AcceptSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetAll(default))[0].Id;

        var command = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(command, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteSellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetAll(default))[0].Id;

        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        var result = await sut.Handle(command, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteSellerRegistrationCommand_AfterAccept_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetAll(default))[0].Id;
        var acceptCommand = new AcceptSellerRegistrationCommand { SellerRegistrationId = sellerRegId, ConfirmUserCallbackUrl = "http://localhost" };
        var result = await sut.Handle(acceptCommand, default);
        result.IsSuccess.ShouldBeTrue();

        var command = new DeleteSellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        result = await sut.Handle(command, default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DenySellerRegistrationCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarSellerHandler>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var sellerRegRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        await sellerRegRepo.Create(new BazaarSellerRegistration { BazaarEventId = eventId, Name = "foo", Email = "foo@foo", Phone = "12345" }, default);
        var sellerRegId = (await sellerRegRepo.GetAll(default))[0].Id;

        var command = new DenySellerRegistrationCommand { SellerRegistrationId = sellerRegId };
        var result = await sut.Handle(command, default);

        result.IsSuccess.ShouldBeTrue();
    }
}
