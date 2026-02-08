using FluentMigrator.Runner;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Repositories;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public sealed class EventHandlerTests
{
    private readonly ServiceFixture _fixture = new();
    private IServiceProvider _serviceProvider = null!;
    private TimeProvider _mockTimeProvider = null!;
    private CancellationToken _cancellationToken;

    public EventHandlerTests(TestContext context)
    {
        _cancellationToken = context.CancellationTokenSource.Token;
    }

    [TestInitialize]
    public async Task Init()
    {
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockTimeProvider.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);

        _fixture.Services.AddSingleton(_mockTimeProvider);
        _fixture.Services.AddScoped<IEvents, Events>();
        _fixture.Services.AddScoped<IPlannings, Plannings>();
        _fixture.Services.AddScoped<ISellerRegistrations, SellerRegistrations>();

        _serviceProvider = _fixture.Build();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _fixture.DisposeAsync();
    }

    [TestMethod]
    public async Task CreateEventCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await sut.Send(new CreateEventCommand(TestData.CreateEvent(_mockTimeProvider.GetUtcNow())), _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task DeleteEventCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var repo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var id = (await repo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var result = await sut.Send(new DeleteEventCommand(id), _cancellationToken);

        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task UpdateEventCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var repo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var id = (await repo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        var model = await repo.Find(id, _cancellationToken);
        model.Value.Description = "foo";

        var result = await sut.Send(new UpdateEventCommand(model.Value), _cancellationToken);
        model = await repo.Find(id, _cancellationToken);

        result.IsError.ShouldBeFalse();
        model.Value.Description.ShouldBe("foo");
    }

    [TestMethod]
    public async Task FindEventQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var repo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var id = (await repo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var result = await sut.Send(new FindEventQuery(id), _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Commission.ShouldBe(20);
    }

    [TestMethod]
    public async Task FindEventForRegisterQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var repo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var id = (await repo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var result = await sut.Send(new FindEventForRegistrationQuery(id), _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.Event.Id.ShouldBe(id);
        result.Value.RegistrationCount.ShouldBe(0);
    }

    [TestMethod]
    public async Task Registrations_FindEventForRegisterQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var regRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrations>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var id = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        await regRepo.Create(new() { EventId = id, Email = "user@foo", Name = "foo", Phone = "12345" }, _cancellationToken);
        await regRepo.Create(new() { EventId = id, Email = "user@bar", Name = "bar", Phone = "12345" }, _cancellationToken);
        await regRepo.Create(new() { EventId = id, Email = "user@baz", Name = "baz", Phone = "12345" }, _cancellationToken);

        var result = await sut.Send(new FindEventForRegistrationQuery(id), _cancellationToken);

        result.IsError.ShouldBeFalse();
        result.Value.RegistrationCount.ShouldBe(3);
    }

    [TestMethod]
    public async Task GetEventsWithRegistrationCountQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var regRepo = scope.ServiceProvider.GetRequiredService<ISellerRegistrations>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var id1 = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        await regRepo.Create(new() { EventId = id1, Email = "user@foo", Name = "foo", Phone = "12345" }, _cancellationToken);
        await regRepo.Create(new() { EventId = id1, Email = "user@bar", Name = "bar", Phone = "12345" }, _cancellationToken);
        await regRepo.Create(new() { EventId = id1, Email = "user@baz", Name = "baz", Phone = "12345" }, _cancellationToken);
        var id2 = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;
        await regRepo.Create(new() { EventId = id2, Email = "user@foo", Name = "foo", Phone = "12345" }, _cancellationToken);

        var result = await sut.Send(new GetEventsWithRegistrationCountQuery(), _cancellationToken);

        result.Length.ShouldBe(2);
        result.First(r => r.Event.Id == id1).RegistrationCount.ShouldBe(3);
        result.First(r => r.Event.Id == id2).RegistrationCount.ShouldBe(1);
    }
}
