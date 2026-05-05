using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Commands.RunAllocationRound;
using Aura.Application.DormAllocations.Events;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Infrastructure.Persistence;
using Aura.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Aura.Tests.Application.DormAllocations;

public class AllocationIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("pgvector/pgvector:pg17")
        .WithDatabase("aura_test")
        .WithUsername("aura")
        .WithPassword("aura")
        .Build();

    private ServiceProvider _sp = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<AuraDbContext>(o =>
            o.UseNpgsql(_container.GetConnectionString(), npgsql => npgsql.UseVector()));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AuraDbContext>());
        services.AddScoped<IAllocationPeriodRepository, AllocationPeriodRepository>();
        services.AddScoped<IDormAllocationRepository, DormAllocationRepository>();
        services.AddScoped<IUpgradeRequestRepository, UpgradeRequestRepository>();
        services.AddScoped<IDormPreferenceRepository, DormPreferenceRepository>();
        services.AddScoped<IStudentRecordRepository, StudentRecordRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFacultyRepository, FacultyRepository>();
        services.AddScoped<ICampusRepository, CampusRepository>();
        services.AddScoped<IDormitoryRepository, DormitoryRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IFacultyRoomAllocationRepository, FacultyRoomAllocationRepository>();
        services.AddScoped<ICurrentUserService>(_ => Substitute.For<ICurrentUserService>());

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<RunAllocationRoundCommand>();
            // CapacityFreedNotificationHandler is in the same Application assembly — picked up automatically
        });

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
    public async Task FullRound_WithDecline_TriggersAutoUpgrade()
    {
        Guid periodId, dormAId, dormBId, user1Id, user2Id;

        // SEED
        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

            var campus = Campus.Create("Codrescu");
            var faculty = Faculty.Create("Informatica", "INF");
            var dormA = Dormitory.Create("CA", campus.Id);
            var dormB = Dormitory.Create("CB", campus.Id);

            var roomA = Room.Create("101", dormA.Id, 1, 1, Gender.Male);
            var roomB = Room.Create("101", dormB.Id, 1, 1, Gender.Male);

            var period = AllocationPeriod.Create(
                "2026-2027",
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                responseWindowDays: 3);
            period.Activate();
            period.StartAllocating();

            var fra1 = FacultyRoomAllocation.Create(faculty.Id, roomA.Id, period.Id);
            var fra2 = FacultyRoomAllocation.Create(faculty.Id, roomB.Id, period.Id);

            // User-1 has higher points (9) so will win dorm A; user-2 gets dorm B
            var user1 = User.Create("u1@uaic.ro", "h");
            user1.UpdateProfile("First1", "Last1");
            user1.AssignToFaculty(faculty.Id);
            user1.SetGender(Gender.Male);

            var user2 = User.Create("u2@uaic.ro", "h");
            user2.UpdateProfile("First2", "Last2");
            user2.AssignToFaculty(faculty.Id);
            user2.SetGender(Gender.Male);

            var record1 = StudentRecord.Create("M001", "First1", "Last1", 9, Gender.Male, faculty.Id, period.Id);
            record1.LinkToUser(user1.Id);
            var record2 = StudentRecord.Create("M002", "First2", "Last2", 7, Gender.Male, faculty.Id, period.Id);
            record2.LinkToUser(user2.Id);

            // Both rank dorm A first, dorm B second
            var pref1A = DormPreference.Create(user1.Id, period.Id, dormA.Id, 1);
            var pref1B = DormPreference.Create(user1.Id, period.Id, dormB.Id, 2);
            var pref2A = DormPreference.Create(user2.Id, period.Id, dormA.Id, 1);
            var pref2B = DormPreference.Create(user2.Id, period.Id, dormB.Id, 2);

            ctx.Campuses.Add(campus);
            ctx.Faculties.Add(faculty);
            ctx.Dormitories.Add(dormA);
            ctx.Dormitories.Add(dormB);
            ctx.Rooms.Add(roomA);
            ctx.Rooms.Add(roomB);
            ctx.AllocationPeriods.Add(period);
            ctx.FacultyRoomAllocations.Add(fra1);
            ctx.FacultyRoomAllocations.Add(fra2);
            ctx.Users.Add(user1);
            ctx.Users.Add(user2);
            ctx.StudentRecords.Add(record1);
            ctx.StudentRecords.Add(record2);
            ctx.DormPreferences.AddRange([pref1A, pref1B, pref2A, pref2B]);

            await ctx.SaveChangesAsync();

            periodId = period.Id;
            dormAId = dormA.Id;
            dormBId = dormB.Id;
            user1Id = user1.Id;
            user2Id = user2.Id;
        }

        // ACT — run allocation round 1
        await using (var scope = _sp.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new RunAllocationRoundCommand(periodId, 1));
        }

        // ASSERT — both students placed, higher-points student got dorm A
        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var allocations = await ctx.DormAllocations
                .Where(a => a.AllocationPeriodId == periodId)
                .ToListAsync();

            allocations.Should().HaveCount(2);
            allocations.Should().AllSatisfy(a => a.Status.Should().Be(AllocationStatus.Pending));
            allocations.Single(a => a.UserId == user1Id).DormitoryId.Should().Be(dormAId);
            allocations.Single(a => a.UserId == user2Id).DormitoryId.Should().Be(dormBId);
        }

        // ACT — user-2 submits an upgrade request for dorm A; user-1 declines their allocation
        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

            var upgrade = UpgradeRequest.Create(user2Id, periodId, [dormAId]);
            ctx.UpgradeRequests.Add(upgrade);

            var u1Alloc = await ctx.DormAllocations.FirstAsync(a => a.UserId == user1Id);
            u1Alloc.Decline();
            await ctx.SaveChangesAsync();

            // Publishing the event triggers CapacityFreedNotificationHandler which auto-upgrades user-2
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            await publisher.Publish(new AllocationDeclinedEvent(u1Alloc.Id, user1Id, dormAId, periodId));
        }

        // ASSERT — user-2 was upgraded: old dorm-B allocation is Replaced, new dorm-A allocation is Accepted
        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

            var u2Allocs = await ctx.DormAllocations
                .Where(a => a.UserId == user2Id)
                .OrderBy(a => a.AllocatedAt)
                .ToListAsync();

            u2Allocs.Should().HaveCount(2);
            u2Allocs[0].DormitoryId.Should().Be(dormBId);
            u2Allocs[0].Status.Should().Be(AllocationStatus.Replaced);
            u2Allocs[1].DormitoryId.Should().Be(dormAId);
            u2Allocs[1].Status.Should().Be(AllocationStatus.Accepted);

            var upgrade = await ctx.UpgradeRequests.FirstAsync(r => r.UserId == user2Id);
            upgrade.IsActive.Should().BeFalse();
        }
    }
}
