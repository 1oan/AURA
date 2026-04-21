using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class FacultyRoomAllocationTests
{
    private static readonly Guid ValidFacultyId = Guid.NewGuid();
    private static readonly Guid ValidRoomId = Guid.NewGuid();
    private static readonly Guid ValidPeriodId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsPopulatedAllocation()
    {
        var allocation = FacultyRoomAllocation.Create(ValidFacultyId, ValidRoomId, ValidPeriodId);

        allocation.Id.Should().NotBe(Guid.Empty);
        allocation.FacultyId.Should().Be(ValidFacultyId);
        allocation.RoomId.Should().Be(ValidRoomId);
        allocation.AllocationPeriodId.Should().Be(ValidPeriodId);
    }

    [Fact]
    public void Create_WithEmptyFacultyId_ThrowsDomainException()
    {
        var act = () => FacultyRoomAllocation.Create(Guid.Empty, ValidRoomId, ValidPeriodId);

        act.Should().Throw<DomainException>().WithMessage("Faculty ID is required.");
    }

    [Fact]
    public void Create_WithEmptyRoomId_ThrowsDomainException()
    {
        var act = () => FacultyRoomAllocation.Create(ValidFacultyId, Guid.Empty, ValidPeriodId);

        act.Should().Throw<DomainException>().WithMessage("Room ID is required.");
    }

    [Fact]
    public void Create_WithEmptyAllocationPeriodId_ThrowsDomainException()
    {
        var act = () => FacultyRoomAllocation.Create(ValidFacultyId, ValidRoomId, Guid.Empty);

        act.Should().Throw<DomainException>().WithMessage("Allocation period ID is required.");
    }
}
