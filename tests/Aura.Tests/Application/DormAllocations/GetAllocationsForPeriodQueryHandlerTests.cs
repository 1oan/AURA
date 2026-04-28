using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Queries.GetAllocationsForPeriod;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class GetAllocationsForPeriodQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();

    private (Campus campus, Dormitory dorm) MakeCampusAndDorm()
    {
        var campus = Campus.Create("Titu Maiorescu");
        var dorm = Dormitory.Create("D1", campus.Id);
        dorm.SetPrivateProperty("Campus", campus);
        return (campus, dorm);
    }

    [Fact]
    public async Task Handle_AsFacultyAdmin_ReturnsAllocationsForFaculty()
    {
        var adminId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var admin = User.Create("admin@uaic.ro", "hash");
        admin.SetPrivateProperty("Id", adminId);
        admin.SetRole(UserRole.FacultyAdmin);
        admin.AssignToFaculty(facultyId);

        var (_, dorm) = MakeCampusAndDorm();

        var a1 = DormAllocation.Create(Guid.NewGuid(), dorm.Id, periodId, 1);
        a1.SetPrivateProperty("Dormitory", dorm);
        var a2 = DormAllocation.Create(Guid.NewGuid(), dorm.Id, periodId, 1);
        a2.SetPrivateProperty("Dormitory", dorm);

        _currentUser.GetCurrentUserId().Returns(adminId);
        _users.FindByIdAsync(adminId, Arg.Any<CancellationToken>()).Returns(admin);
        _allocations.GetByPeriodAndFacultyAsync(periodId, facultyId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { a1, a2 });

        var handler = new GetAllocationsForPeriodQueryHandler(_currentUser, _users, _allocations);
        var result = await handler.Handle(new GetAllocationsForPeriodQuery(periodId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.All(r => r.DormitoryName == "D1").Should().BeTrue();
        result.All(r => r.CampusName == "Titu Maiorescu").Should().BeTrue();
        await _allocations.DidNotReceive().GetByPeriodAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AsSuperAdmin_ReturnsAllAllocationsForPeriod()
    {
        var adminId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var admin = User.Create("super@uaic.ro", "hash");
        admin.SetPrivateProperty("Id", adminId);
        admin.SetRole(UserRole.SuperAdmin);
        // No faculty assigned — SuperAdmin sees everything

        var (_, dorm) = MakeCampusAndDorm();
        var a1 = DormAllocation.Create(Guid.NewGuid(), dorm.Id, periodId, 1);
        a1.SetPrivateProperty("Dormitory", dorm);

        _currentUser.GetCurrentUserId().Returns(adminId);
        _users.FindByIdAsync(adminId, Arg.Any<CancellationToken>()).Returns(admin);
        _allocations.GetByPeriodAsync(periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { a1 });

        var handler = new GetAllocationsForPeriodQueryHandler(_currentUser, _users, _allocations);
        var result = await handler.Handle(new GetAllocationsForPeriodQuery(periodId), CancellationToken.None);

        result.Should().HaveCount(1);
        await _allocations.DidNotReceive().GetByPeriodAndFacultyAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_Throws()
    {
        var userId = Guid.NewGuid();
        _currentUser.GetCurrentUserId().Returns(userId);
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new GetAllocationsForPeriodQueryHandler(_currentUser, _users, _allocations);
        var act = async () => await handler.Handle(new GetAllocationsForPeriodQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_FacultyAdminWithoutFaculty_Throws()
    {
        var adminId = Guid.NewGuid();
        var admin = User.Create("admin@uaic.ro", "hash");
        admin.SetPrivateProperty("Id", adminId);
        admin.SetRole(UserRole.FacultyAdmin);
        // FacultyId remains null

        _currentUser.GetCurrentUserId().Returns(adminId);
        _users.FindByIdAsync(adminId, Arg.Any<CancellationToken>()).Returns(admin);

        var handler = new GetAllocationsForPeriodQueryHandler(_currentUser, _users, _allocations);
        var act = async () => await handler.Handle(new GetAllocationsForPeriodQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*faculty*");
    }

    [Fact]
    public async Task Handle_AsStudent_Throws()
    {
        var studentId = Guid.NewGuid();
        var student = User.Create("student@uaic.ro", "hash");
        student.SetPrivateProperty("Id", studentId);
        // Default role is Student per User.Create

        _currentUser.GetCurrentUserId().Returns(studentId);
        _users.FindByIdAsync(studentId, Arg.Any<CancellationToken>()).Returns(student);

        var handler = new GetAllocationsForPeriodQueryHandler(_currentUser, _users, _allocations);
        var act = async () => await handler.Handle(new GetAllocationsForPeriodQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*administrators*");
    }
}
