using FluentMigrator.Runner;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Repositories;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace GtKram.Application.Tests.Integration;

[TestClass]
public sealed class PlanningHandlerTests
{
    private readonly ServiceFixture _fixture = new();
    private IServiceProvider _serviceProvider = null!;
    private TimeProvider _mockTimeProvider = null!;
    private CancellationToken _cancellationToken;

    public PlanningHandlerTests(TestContext context)
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
    public async Task CreatePlanningCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var eventRepo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var planning = new Planning
        {
            EventId = eventId,
            Name = "planning",
            Date = _mockTimeProvider.GetUtcNow(),
            From = new TimeOnly(9, 0),
            To = new TimeOnly(12, 0)
        };

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(new CreatePlanningCommand(planning), _cancellationToken);
        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task GetPlanningsQuery_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var plannings = await CreatePlannings(scope);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(new GetPlanningsQuery(plannings[0].EventId), _cancellationToken);
        result.Length.ShouldBe(2);
    }

    [TestMethod]
    public async Task UpdatePlanningCommand_IsSuccess()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var plannings = await CreatePlannings(scope);
        plannings[0].From = new TimeOnly(8, 0);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(new UpdatePlanningCommand(plannings[0]), _cancellationToken);
        result.IsError.ShouldBeFalse();
    }

    [TestMethod]
    public async Task EventExpired_UpdatePlanningCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var plannings = await CreatePlannings(scope);

        _mockTimeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow.AddDays(3));
        
        plannings[0].From = new TimeOnly(8, 0);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(new UpdatePlanningCommand(plannings[0]), _cancellationToken);
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Domain.Errors.Event.Expired);
    }

    [TestMethod]
    public async Task ValidationFrom_UpdatePlanningCommand_IsFailed()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var plannings = await CreatePlannings(scope);

        plannings[0].From = new TimeOnly(18, 0);

        var sut = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await sut.Send(new UpdatePlanningCommand(plannings[0]), _cancellationToken);
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(Domain.Errors.Planning.ValidationFromBeforeToFailed);
    }

    private async Task<Planning[]> CreatePlannings(AsyncServiceScope scope)
    {
        var eventRepo = scope.ServiceProvider.GetRequiredService<IEvents>();
        var eventId = (await eventRepo.Create(TestData.CreateEvent(_mockTimeProvider.GetUtcNow()), _cancellationToken)).Value;

        var planningsRepo = scope.ServiceProvider.GetRequiredService<IPlannings>();

        var planning = new Planning
        {
            EventId = eventId,
            Name = "planning 1",
            Date = _mockTimeProvider.GetUtcNow(),
            From = new TimeOnly(9, 0),
            To = new TimeOnly(12, 0)
        };

        await planningsRepo.Create(planning, _cancellationToken);

        planning = new Planning
        {
            EventId = eventId,
            Name = "planning 2",
            Date = _mockTimeProvider.GetUtcNow(),
            From = new TimeOnly(12, 0),
            To = new TimeOnly(15, 0)
        };

        await planningsRepo.Create(planning, _cancellationToken);

        return await planningsRepo.GetAll(_cancellationToken);
    }

}
