using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Handlers;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Errors;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;


namespace GtKram.Application.Tests.Integration;

public sealed class BazaarEventHandlerTests : DatabaseFixture
{
    private TimeProvider _mockTimeProvider = null!;

    protected override void Setup(IServiceCollection services)
    {
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockTimeProvider.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);

        services.AddSingleton(_mockTimeProvider);
        services.AddScoped<IBazaarEventRepository, BazaarEventRepository>();
        services.AddScoped<IBazaarSellerRegistrationRepository, BazaarSellerRegistrationRepository>();
        services.AddScoped<BazaarEventHandler>();
    }

    [Fact]
    public async Task CreateEventCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();

        var result = await sut.Handle(new CreateEventCommand(TestData.CreateEvent(_mockTimeProvider.GetUtcNow())), default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteEventCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var repo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id = (await repo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;

        var result = await sut.Handle(new DeleteEventCommand(id), default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateEventCommand_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var repo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id = (await repo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        var model = await repo.Find(id, default);
        model.Value.Description = "foo";

        var result = await sut.Handle(new UpdateEventCommand(model.Value), default);
        model = await repo.Find(id, default);

        result.IsSuccess.ShouldBeTrue();
        model.Value.Description.ShouldBe("foo");
    }

    [Fact]
    public async Task FindEventQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var repo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id = (await repo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;

        var result = await sut.Handle(new FindEventQuery(id), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Commission.ShouldBe(20);
    }

    [Fact]
    public async Task FindEventForRegisterQuery_IsSuccess()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var repo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id = (await repo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;

        var result = await sut.Handle(new FindEventForRegisterQuery(id), default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task FFindEventForRegisterQuery_IsFailed_If_Expired()
    {
        var @event = TestData.CreateEvent(_mockTimeProvider.GetUtcNow());
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));

        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var repo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id = (await repo.Create(@event, default)).Value;

        var result = await sut.Handle(new FindEventForRegisterQuery(id), default);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == Event.Expired).ShouldBeTrue();
    }

    [Fact]
    public async Task FindEventForRegisterQuery_IsFailed_If_RegisterStartsOn()
    {
        var @event = TestData.CreateEvent(_mockTimeProvider.GetUtcNow().AddHours(1));

        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var repo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id = (await repo.Create(@event, default)).Value;

        var result = await sut.Handle(new FindEventForRegisterQuery(id), default);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == EventRegistration.NotReady).ShouldBeTrue();
    }

    [Fact]
    public async Task FindEventForRegisterQuery_IsFailed_If_RegisterEndsOn()
    {
        var @event = TestData.CreateEvent(_mockTimeProvider.GetUtcNow());
        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddHours(2));

        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var repo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id = (await repo.Create(@event, default)).Value;

        var result = await sut.Handle(new FindEventForRegisterQuery(id), default);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == EventRegistration.NotReady).ShouldBeTrue();
    }

    [Fact]
    public async Task FindEventForRegisterQuery_IsFailed_If_MaxSellers()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var regRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        await regRepo.Create(new() { BazaarEventId = id, Email = "user@foo", Name = "foo", Phone = "12345" }, default);
        await regRepo.Create(new() { BazaarEventId = id, Email = "user@bar", Name = "bar", Phone = "12345" }, default);
        await regRepo.Create(new() { BazaarEventId = id, Email = "user@baz", Name = "baz", Phone = "12345" }, default);

        var result = await sut.Handle(new FindEventForRegisterQuery(id), default);

        result.IsFailed.ShouldBeTrue();
        result.Errors.Any(e => e == EventRegistration.LimitExceeded).ShouldBeTrue();
    }

    [Fact]
    public async Task GetEventsWithRegistrationCountQuery_Has_Result()
    {
        using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<BazaarEventHandler>();
        var regRepo = scope.ServiceProvider.GetRequiredService<IBazaarSellerRegistrationRepository>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IBazaarEventRepository>();
        var id1 = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        await regRepo.Create(new() { BazaarEventId = id1, Email = "user@foo", Name = "foo", Phone = "12345" }, default);
        await regRepo.Create(new() { BazaarEventId = id1, Email = "user@bar", Name = "bar", Phone = "12345" }, default);
        await regRepo.Create(new() { BazaarEventId = id1, Email = "user@baz", Name = "baz", Phone = "12345" }, default);
        var id2 = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), default)).Value;
        await regRepo.Create(new() { BazaarEventId = id2, Email = "user@foo", Name = "foo", Phone = "12345" }, default);

        var result = await sut.Handle(new GetEventsWithRegistrationCountQuery(), default);

        result.Length.ShouldBe(2);
        result.First(r => r.Event.Id == id1).RegistrationCount.ShouldBe(3);
        result.First(r => r.Event.Id == id2).RegistrationCount.ShouldBe(1);
    }
}
