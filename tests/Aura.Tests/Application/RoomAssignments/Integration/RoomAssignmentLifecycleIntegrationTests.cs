using Aura.Application.Common.Interfaces;
using Aura.Application.Compatibility;
using Aura.Application.RoomAssignments.Commands.PlaceMeNow;
using Aura.Application.RoommateGroups.Commands.AcceptGroupInvitation;
using Aura.Application.RoommateGroups.Commands.CreateRoommateGroup;
using Aura.Application.RoommateGroups.Commands.InviteToGroup;
using Aura.Application.RoommateGroups.Commands.LockGroup;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Infrastructure.Allocation;
using Aura.Infrastructure.Persistence;
using Aura.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Aura.Tests.Application.RoomAssignments.Integration;

public class RoomAssignmentLifecycleIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("aura_test").WithUsername("aura").WithPassword("aura").Build();

    private ServiceProvider _sp = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IDataProtectionProvider>(_ => Substitute.For<IDataProtectionProvider>());
        services.AddDbContext<AuraDbContext>(o =>
            o.UseNpgsql(_container.GetConnectionString(), n => n.UseVector()));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AuraDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICampusRepository, CampusRepository>();
        services.AddScoped<IDormitoryRepository, DormitoryRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IFacultyRepository, FacultyRepository>();
        services.AddScoped<IAllocationPeriodRepository, AllocationPeriodRepository>();
        services.AddScoped<IFacultyRoomAllocationRepository, FacultyRoomAllocationRepository>();
        services.AddScoped<IStudentRecordRepository, StudentRecordRepository>();
        services.AddScoped<IEmailConfirmationCodeRepository, EmailConfirmationCodeRepository>();
        services.AddScoped<IDormPreferenceRepository, DormPreferenceRepository>();
        services.AddScoped<IDormAllocationRepository, DormAllocationRepository>();
        services.AddScoped<IUpgradeRequestRepository, UpgradeRequestRepository>();
        services.AddScoped<IInterestRepository, InterestRepository>();
        services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
        services.AddScoped<IStudentEmbeddingRepository, StudentEmbeddingRepository>();
        services.AddScoped<ISpotifySnapshotRepository, SpotifySnapshotRepository>();
        services.AddScoped<IRoommateGroupRepository, RoommateGroupRepository>();
        services.AddScoped<IGroupInvitationRepository, GroupInvitationRepository>();
        services.AddScoped<IRoomAssignmentRepository, RoomAssignmentRepository>();
        services.AddScoped<IRoomAssignmentService, RoomAssignmentService>();
        services.AddScoped<ICurrentUserService>(_ => Substitute.For<ICurrentUserService>());
        services.AddScoped<IEmailService>(_ => Substitute.For<IEmailService>());
        services.AddScoped<ICompatibilityScorer, NullCompatibilityScorer>();
        services.AddSingleton(TimeProvider.System);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateRoommateGroupCommand>());
        _sp = services.BuildServiceProvider();
        await using var scope = _sp.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _sp.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task SoloPlacement_HappyPath_AssignsRoomToStudent()
    {
        Guid userId, roomId;

        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var campus = Campus.Create("TC");
            var faculty = Faculty.Create("Informatica", "INF");
            var dorm = Dormitory.Create("D1", campus.Id);
            var room = Room.Create("101", dorm.Id, 1, 2, Gender.Male);
            var period = AllocationPeriod.Create("p", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(1), 7);
            period.Activate();
            period.StartAllocating();
            var user = User.Create("a@uaic.ro", "h");
            user.UpdateProfile("A", "One");
            user.AssignToFaculty(faculty.Id);
            user.SetGender(Gender.Male);
            var alloc = DormAllocation.Create(user.Id, dorm.Id, period.Id, 1);
            alloc.Accept();
            ctx.Campuses.Add(campus);
            ctx.Faculties.Add(faculty);
            ctx.Dormitories.Add(dorm);
            ctx.Rooms.Add(room);
            ctx.AllocationPeriods.Add(period);
            ctx.Users.Add(user);
            ctx.DormAllocations.Add(alloc);
            await ctx.SaveChangesAsync();
            userId = user.Id;
            roomId = room.Id;
        }

        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userId);
            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new PlaceMeNowCommand());
        }

        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var assignment = await ctx.RoomAssignments.SingleAsync();
            assignment.UserId.Should().Be(userId);
            assignment.RoomId.Should().Be(roomId);
        }
    }

    [Fact]
    public async Task GroupPlacement_HappyPath_BothMembersGetSameRoom()
    {
        Guid userA, userB, groupId, invitationId, roomId;

        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var campus = Campus.Create("TC2");
            var faculty = Faculty.Create("Matematica", "MAT");
            var dorm = Dormitory.Create("D2", campus.Id);
            var room = Room.Create("101", dorm.Id, 1, 2, Gender.Male);
            var period = AllocationPeriod.Create("p2", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(1), 7);
            period.Activate();
            period.StartAllocating();
            var a = User.Create("c@uaic.ro", "h");
            a.UpdateProfile("C", "Three");
            a.AssignToFaculty(faculty.Id);
            a.SetGender(Gender.Male);
            var b = User.Create("d@uaic.ro", "h");
            b.UpdateProfile("D", "Four");
            b.AssignToFaculty(faculty.Id);
            b.SetGender(Gender.Male);
            var allocA = DormAllocation.Create(a.Id, dorm.Id, period.Id, 1);
            allocA.Accept();
            var allocB = DormAllocation.Create(b.Id, dorm.Id, period.Id, 1);
            allocB.Accept();
            ctx.Campuses.Add(campus);
            ctx.Faculties.Add(faculty);
            ctx.Dormitories.Add(dorm);
            ctx.Rooms.Add(room);
            ctx.AllocationPeriods.Add(period);
            ctx.Users.AddRange(a, b);
            ctx.DormAllocations.AddRange(allocA, allocB);
            await ctx.SaveChangesAsync();
            userA = a.Id;
            userB = b.Id;
            roomId = room.Id;
        }

        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userA);
            groupId = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new CreateRoommateGroupCommand(RoomSizePreference.TwoBed));
        }

        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userA);
            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new InviteToGroupCommand(groupId, userB));
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            invitationId = (await ctx.GroupInvitations.SingleAsync()).Id;
        }

        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userB);
            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new AcceptGroupInvitationCommand(invitationId));
        }

        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userA);
            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new LockGroupCommand(groupId));
        }

        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var assignments = await ctx.RoomAssignments.ToListAsync();
            assignments.Should().HaveCount(2);
            assignments.Should().AllSatisfy(a => a.RoomId.Should().Be(roomId));
        }
    }
}
