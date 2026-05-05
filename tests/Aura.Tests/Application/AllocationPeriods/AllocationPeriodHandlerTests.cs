using Aura.Application.AllocationPeriods.Commands.ActivateAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.CloseAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.CreateAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.DeleteAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.StartAllocating;
using Aura.Application.AllocationPeriods.Commands.UpdateAllocationPeriod;
using Aura.Application.AllocationPeriods.Queries.GetAllocationPeriodById;
using Aura.Application.AllocationPeriods.Queries.GetAllocationPeriods;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.AllocationPeriods;

public static class PeriodFactory
{
    public static readonly DateTime Start = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime End = new(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    public static AllocationPeriod Draft() => AllocationPeriod.Create("2026-2027", Start, End, Start.AddDays(14), 3);

    public static AllocationPeriod Open()
    {
        var p = Draft();
        p.Activate();
        return p;
    }

    public static AllocationPeriod Allocating()
    {
        var p = Open();
        p.StartAllocating();
        return p;
    }
}

public class CreateAllocationPeriodCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _repo = Substitute.For<IAllocationPeriodRepository>();

    [Fact]
    public async Task Handle_CreatesDraftPeriod()
    {
        var handler = new CreateAllocationPeriodCommandHandler(_repo);
        var result = await handler.Handle(
            new CreateAllocationPeriodCommand("2026-2027", PeriodFactory.Start, PeriodFactory.End, PeriodFactory.Start.AddDays(14), 3),
            CancellationToken.None);

        result.Name.Should().Be("2026-2027");
        result.Status.Should().Be("Draft");
        await _repo.Received(1).AddAsync(Arg.Any<AllocationPeriod>(), Arg.Any<CancellationToken>());
    }
}

public class UpdateAllocationPeriodCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _repo = Substitute.For<IAllocationPeriodRepository>();

    [Fact]
    public async Task Handle_UpdatesDraftPeriod()
    {
        var period = PeriodFactory.Draft();
        _repo.FindByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var handler = new UpdateAllocationPeriodCommandHandler(_repo);
        await handler.Handle(
            new UpdateAllocationPeriodCommand(period.Id, "Updated", PeriodFactory.Start, PeriodFactory.End),
            CancellationToken.None);

        period.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var handler = new UpdateAllocationPeriodCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new UpdateAllocationPeriodCommand(Guid.NewGuid(), "X", PeriodFactory.Start, PeriodFactory.End),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class DeleteAllocationPeriodCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _repo = Substitute.For<IAllocationPeriodRepository>();

    [Fact]
    public async Task Handle_DeletesDraftPeriod()
    {
        var period = PeriodFactory.Draft();
        _repo.FindByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var handler = new DeleteAllocationPeriodCommandHandler(_repo);
        await handler.Handle(new DeleteAllocationPeriodCommand(period.Id), CancellationToken.None);

        _repo.Received(1).Remove(period);
    }

    [Fact]
    public async Task Handle_WhenOpen_Throws()
    {
        var period = PeriodFactory.Open();
        _repo.FindByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var handler = new DeleteAllocationPeriodCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new DeleteAllocationPeriodCommand(period.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var handler = new DeleteAllocationPeriodCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new DeleteAllocationPeriodCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class ActivateAllocationPeriodCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _repo = Substitute.For<IAllocationPeriodRepository>();

    [Fact]
    public async Task Handle_ActivatesDraftWhenNoneActive()
    {
        var period = PeriodFactory.Draft();
        _repo.FindByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);
        _repo.AnyActiveAsync(Arg.Any<CancellationToken>()).Returns(false);

        var handler = new ActivateAllocationPeriodCommandHandler(_repo);
        await handler.Handle(new ActivateAllocationPeriodCommand(period.Id), CancellationToken.None);

        period.Status.ToString().Should().Be("Open");
    }

    [Fact]
    public async Task Handle_WhenAnotherActive_Throws()
    {
        var period = PeriodFactory.Draft();
        _repo.FindByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);
        _repo.AnyActiveAsync(Arg.Any<CancellationToken>()).Returns(true);

        var handler = new ActivateAllocationPeriodCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new ActivateAllocationPeriodCommand(period.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var handler = new ActivateAllocationPeriodCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new ActivateAllocationPeriodCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class StartAllocatingCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _repo = Substitute.For<IAllocationPeriodRepository>();

    [Fact]
    public async Task Handle_MovesOpenToAllocating()
    {
        var period = PeriodFactory.Open();
        _repo.FindByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var handler = new StartAllocatingCommandHandler(_repo);
        await handler.Handle(new StartAllocatingCommand(period.Id), CancellationToken.None);

        period.Status.ToString().Should().Be("Allocating");
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var handler = new StartAllocatingCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new StartAllocatingCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class CloseAllocationPeriodCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _repo = Substitute.For<IAllocationPeriodRepository>();

    [Fact]
    public async Task Handle_ClosesAllocatingPeriod()
    {
        var period = PeriodFactory.Allocating();
        _repo.FindByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var handler = new CloseAllocationPeriodCommandHandler(_repo);
        await handler.Handle(new CloseAllocationPeriodCommand(period.Id), CancellationToken.None);

        period.Status.ToString().Should().Be("Closed");
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var handler = new CloseAllocationPeriodCommandHandler(_repo);
        var act = async () => await handler.Handle(
            new CloseAllocationPeriodCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

public class GetAllocationPeriodsQueryHandlerTests
{
    private readonly IAllocationPeriodRepository _repo = Substitute.For<IAllocationPeriodRepository>();

    [Fact]
    public async Task Handle_ReturnsAllPeriods()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<AllocationPeriod>
        {
            PeriodFactory.Draft(),
            PeriodFactory.Open(),
        });

        var handler = new GetAllocationPeriodsQueryHandler(_repo);
        var result = await handler.Handle(new GetAllocationPeriodsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}

public class GetAllocationPeriodByIdQueryHandlerTests
{
    private readonly IAllocationPeriodRepository _repo = Substitute.For<IAllocationPeriodRepository>();

    [Fact]
    public async Task Handle_ReturnsPeriod()
    {
        var period = PeriodFactory.Draft();
        _repo.FindByIdAsync(period.Id, Arg.Any<CancellationToken>()).Returns(period);

        var handler = new GetAllocationPeriodByIdQueryHandler(_repo);
        var result = await handler.Handle(new GetAllocationPeriodByIdQuery(period.Id), CancellationToken.None);

        result.Id.Should().Be(period.Id);
    }

    [Fact]
    public async Task Handle_WhenNotFound_Throws()
    {
        _repo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var handler = new GetAllocationPeriodByIdQueryHandler(_repo);
        var act = async () => await handler.Handle(
            new GetAllocationPeriodByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
