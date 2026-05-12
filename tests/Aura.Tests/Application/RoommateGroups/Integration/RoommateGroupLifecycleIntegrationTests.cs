using Aura.Application.Common.Interfaces;
using Aura.Application.Compatibility;
using Aura.Application.RoommateGroups.Commands.AcceptGroupInvitation;
using Aura.Application.RoommateGroups.Commands.CreateRoommateGroup;
using Aura.Application.RoommateGroups.Commands.InviteToGroup;
using Aura.Application.RoommateGroups.Commands.LockGroup;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
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

namespace Aura.Tests.Application.RoommateGroups.Integration;

public class RoommateGroupLifecycleIntegrationTests : IAsyncLifetime
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
        services.AddScoped<IRoommateGroupRepository, RoommateGroupRepository>();
        services.AddScoped<IGroupInvitationRepository, GroupInvitationRepository>();
        services.AddScoped<IDormAllocationRepository, DormAllocationRepository>();
        services.AddScoped<IDormitoryRepository, DormitoryRepository>();
        services.AddScoped<IAllocationPeriodRepository, AllocationPeriodRepository>();
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
    public async Task FullLifecycle_CreateInviteAcceptLock_RaisesGroupLockedEvent()
    {
        Guid userA, userB, groupId, invitationId;

        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var campus = Campus.Create("TC");
            var faculty = Faculty.Create("Informatica", "INF");
            var dorm = Dormitory.Create("D1", campus.Id);
            var period = AllocationPeriod.Create("p", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(1), 7);
            period.Activate();
            period.StartAllocating();
            var a = User.Create("a@uaic.ro", "h");
            a.UpdateProfile("A", "One");
            a.AssignToFaculty(faculty.Id);
            a.SetGender(Gender.Male);
            var b = User.Create("b@uaic.ro", "h");
            b.UpdateProfile("B", "Two");
            b.AssignToFaculty(faculty.Id);
            b.SetGender(Gender.Male);
            var allocA = DormAllocation.Create(a.Id, dorm.Id, period.Id, 1);
            allocA.Accept();
            var allocB = DormAllocation.Create(b.Id, dorm.Id, period.Id, 1);
            allocB.Accept();
            ctx.Campuses.Add(campus);
            ctx.Faculties.Add(faculty);
            ctx.Dormitories.Add(dorm);
            ctx.AllocationPeriods.Add(period);
            ctx.Users.AddRange(a, b);
            ctx.DormAllocations.AddRange(allocA, allocB);
            await ctx.SaveChangesAsync();
            userA = a.Id;
            userB = b.Id;
        }

        // A creates a 2-bed group
        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userA);
            groupId = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new CreateRoommateGroupCommand(RoomSizePreference.TwoBed));
        }

        // A invites B
        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userA);
            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new InviteToGroupCommand(groupId, userB));
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            invitationId = (await ctx.GroupInvitations.SingleAsync()).Id;
        }

        // B accepts
        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userB);
            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new AcceptGroupInvitationCommand(invitationId));
        }

        // A locks
        await using (var scope = _sp.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentUserService>().GetCurrentUserId().Returns(userA);
            await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new LockGroupCommand(groupId));
        }

        // Assert: group is Locked with 2 members
        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var group = await ctx.RoommateGroups.Include(g => g.Members).SingleAsync();
            group.Status.Should().Be(GroupStatus.Locked);
            group.Members.Should().HaveCount(2);
            group.Members.Should().Contain(m => m.UserId == userA);
            group.Members.Should().Contain(m => m.UserId == userB);
        }
    }
}
