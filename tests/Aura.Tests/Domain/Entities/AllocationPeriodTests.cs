using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class AllocationPeriodTests
{
    private static readonly DateTime ValidStart = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ValidEnd = new(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string ValidName = "2026-2027";

    // ─── Create() ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsPeriodInDraftStatus()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);

        period.Id.Should().NotBe(Guid.Empty);
        period.Name.Should().Be(ValidName);
        period.StartDate.Should().Be(ValidStart);
        period.EndDate.Should().Be(ValidEnd);
        period.Status.Should().Be(AllocationPeriodStatus.Draft);
    }

    [Fact]
    public void Create_TrimsName()
    {
        var period = AllocationPeriod.Create("  2026-2027  ", ValidStart, ValidEnd);

        period.Name.Should().Be("2026-2027");
    }

    [Fact]
    public void Create_NormalizesDatesToUtc()
    {
        var localStart = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Local);
        var localEnd = new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Local);

        var period = AllocationPeriod.Create(ValidName, localStart, localEnd);

        period.StartDate.Kind.Should().Be(DateTimeKind.Utc);
        period.EndDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        var act = () => AllocationPeriod.Create(name!, ValidStart, ValidEnd);

        act.Should().Throw<DomainException>().WithMessage("Allocation period name is required.");
    }

    [Fact]
    public void Create_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var longName = new string('a', 201);

        var act = () => AllocationPeriod.Create(longName, ValidStart, ValidEnd);

        act.Should().Throw<DomainException>().WithMessage("Allocation period name must not exceed 200 characters.");
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ThrowsDomainException()
    {
        var act = () => AllocationPeriod.Create(ValidName, ValidEnd, ValidStart);

        act.Should().Throw<DomainException>().WithMessage("End date must be after start date.");
    }

    [Fact]
    public void Create_WithEndDateEqualToStartDate_ThrowsDomainException()
    {
        var act = () => AllocationPeriod.Create(ValidName, ValidStart, ValidStart);

        act.Should().Throw<DomainException>().WithMessage("End date must be after start date.");
    }

    // ─── Activate() — Draft → Open ───────────────────────────────────────

    [Fact]
    public void Activate_FromDraft_TransitionsToOpen()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);

        period.Activate();

        period.Status.Should().Be(AllocationPeriodStatus.Open);
    }

    [Fact]
    public void Activate_FromOpen_ThrowsDomainException()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);
        period.Activate();

        var act = () => period.Activate();

        act.Should().Throw<DomainException>().WithMessage("Can only activate a Draft allocation period.");
    }

    [Fact]
    public void Activate_FromAllocating_ThrowsDomainException()
    {
        var period = InAllocatingState();

        var act = () => period.Activate();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_FromClosed_ThrowsDomainException()
    {
        var period = InClosedState();

        var act = () => period.Activate();

        act.Should().Throw<DomainException>();
    }

    // ─── StartAllocating() — Open → Allocating ───────────────────────────

    [Fact]
    public void StartAllocating_FromOpen_TransitionsToAllocating()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);
        period.Activate();

        period.StartAllocating();

        period.Status.Should().Be(AllocationPeriodStatus.Allocating);
    }

    [Fact]
    public void StartAllocating_FromDraft_ThrowsDomainException()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);

        var act = () => period.StartAllocating();

        act.Should().Throw<DomainException>().WithMessage("Can only start allocating an Open allocation period.");
    }

    [Fact]
    public void StartAllocating_FromAllocating_ThrowsDomainException()
    {
        var period = InAllocatingState();

        var act = () => period.StartAllocating();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartAllocating_FromClosed_ThrowsDomainException()
    {
        var period = InClosedState();

        var act = () => period.StartAllocating();

        act.Should().Throw<DomainException>();
    }

    // ─── Close() — Allocating → Closed ───────────────────────────────────

    [Fact]
    public void Close_FromAllocating_TransitionsToClosed()
    {
        var period = InAllocatingState();

        period.Close();

        period.Status.Should().Be(AllocationPeriodStatus.Closed);
    }

    [Fact]
    public void Close_FromDraft_ThrowsDomainException()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);

        var act = () => period.Close();

        act.Should().Throw<DomainException>().WithMessage("Can only close an Allocating allocation period.");
    }

    [Fact]
    public void Close_FromOpen_ThrowsDomainException()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);
        period.Activate();

        var act = () => period.Close();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Close_FromClosed_ThrowsDomainException()
    {
        var period = InClosedState();

        var act = () => period.Close();

        act.Should().Throw<DomainException>();
    }

    // ─── Update() — Draft only ───────────────────────────────────────────

    [Fact]
    public void Update_WhenDraft_UpdatesFields()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);
        var newStart = ValidStart.AddDays(1);
        var newEnd = ValidEnd.AddDays(1);

        period.Update("2027-2028", newStart, newEnd);

        period.Name.Should().Be("2027-2028");
        period.StartDate.Should().Be(newStart);
        period.EndDate.Should().Be(newEnd);
    }

    [Fact]
    public void Update_WhenOpen_ThrowsDomainException()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);
        period.Activate();

        var act = () => period.Update("new name", ValidStart, ValidEnd);

        act.Should().Throw<DomainException>().WithMessage("Can only update allocation period while in Draft status.");
    }

    [Fact]
    public void Update_WhenAllocating_ThrowsDomainException()
    {
        var period = InAllocatingState();

        var act = () => period.Update("new name", ValidStart, ValidEnd);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WhenClosed_ThrowsDomainException()
    {
        var period = InClosedState();

        var act = () => period.Update("new name", ValidStart, ValidEnd);

        act.Should().Throw<DomainException>();
    }

    // ─── Full lifecycle ──────────────────────────────────────────────────

    [Fact]
    public void FullLifecycle_DraftOpenAllocatingClosed_TransitionsCleanly()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);
        period.Status.Should().Be(AllocationPeriodStatus.Draft);

        period.Activate();
        period.Status.Should().Be(AllocationPeriodStatus.Open);

        period.StartAllocating();
        period.Status.Should().Be(AllocationPeriodStatus.Allocating);

        period.Close();
        period.Status.Should().Be(AllocationPeriodStatus.Closed);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static AllocationPeriod InAllocatingState()
    {
        var period = AllocationPeriod.Create(ValidName, ValidStart, ValidEnd);
        period.Activate();
        period.StartAllocating();
        return period;
    }

    private static AllocationPeriod InClosedState()
    {
        var period = InAllocatingState();
        period.Close();
        return period;
    }
}
