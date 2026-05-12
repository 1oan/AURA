using Aura.Application.Common.Interfaces;
using Aura.Application.Compatibility;
using Aura.Application.Profile.Commands.SetInterests;
using Aura.Application.Profile.Commands.SubmitLifestyle;
using Aura.Application.Profile.Commands.SubmitTipi;
using Aura.Application.Profile.Queries.GetMyProfile;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Infrastructure.Persistence;
using Aura.Infrastructure.Persistence.Repositories;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Aura.Tests.Application.Profile.Integration;

public class ProfileLifecycleIntegrationTests : IAsyncLifetime
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
        // IDataProtectionProvider is needed by Spotify handlers in the same MediatR assembly;
        // this test doesn't invoke those handlers, so a stub is sufficient.
        services.AddSingleton<IDataProtectionProvider>(_ => Substitute.For<IDataProtectionProvider>());
        services.AddDbContext<AuraDbContext>(o =>
            o.UseNpgsql(_container.GetConnectionString(), npgsql => npgsql.UseVector()));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AuraDbContext>());
        services.AddScoped<IInterestRepository, InterestRepository>();
        services.AddScoped<IStudentProfileRepository, StudentProfileRepository>();
        services.AddScoped<IStudentEmbeddingRepository, StudentEmbeddingRepository>();
        services.AddScoped<ISpotifySnapshotRepository, SpotifySnapshotRepository>();
        services.AddScoped<ICurrentUserService>(_ => Substitute.For<ICurrentUserService>());
        services.AddScoped<ICompatibilityScorer, NullCompatibilityScorer>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SubmitLifestyleCommand>());

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
    public async Task Migration_SeedsInterestCatalog()
    {
        await using var scope = _sp.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

        var count = await ctx.Interests.CountAsync(i => i.IsActive);
        count.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public async Task FullLifecycle_SubmitLifestyleTipiInterests_Reaches75Percent()
    {
        var userId = Guid.NewGuid();

        await using (var scope = _sp.CreateAsyncScope())
        {
            // Seed a User row so FK constraints are satisfied
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var user = User.Create($"profile-{userId:N}@uaic.ro", "h");
            user.SetPrivateProperty("Id", userId);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
        }

        // Submit lifestyle
        await using (var scope = _sp.CreateAsyncScope())
        {
            var current = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
            current.GetCurrentUserId().Returns(userId);
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new SubmitLifestyleCommand(
                SleepSchedule.Late, WakeUpTime.Late, 4,
                NoiseTolerance.Some, StudyLocation.Mixed,
                GuestFrequency.Weekly, SmokingHabit.OutdoorsOnly));
        }

        // Submit TIPI
        await using (var scope = _sp.CreateAsyncScope())
        {
            var current = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
            current.GetCurrentUserId().Returns(userId);
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new SubmitTipiCommand(new[] { 7, 2, 6, 2, 5, 2, 5, 3, 6, 3 }));
        }

        // Set interests
        await using (var scope = _sp.CreateAsyncScope())
        {
            var current = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
            current.GetCurrentUserId().Returns(userId);
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new SetInterestsCommand(new[] { "football", "gaming" }));
        }

        // Query and verify
        await using (var scope = _sp.CreateAsyncScope())
        {
            var current = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
            current.GetCurrentUserId().Returns(userId);
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var profile = await sender.Send(new GetMyProfileQuery());

            profile.CompletenessPercent.Should().Be(75);
            profile.Lifestyle.Should().NotBeNull();
            profile.Tipi.Should().NotBeNull();
            profile.Interests.Slugs.Should().Equal("football", "gaming");
            profile.Spotify.Connected.Should().BeFalse();
        }

        // Verify embedding row exists
        await using (var scope = _sp.CreateAsyncScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            var embedding = await ctx.StudentEmbeddings.SingleAsync(e => e.UserId == userId);
            embedding.Embedding.Should().BeNull();
            embedding.LastEmbeddedAt.Should().BeNull();
        }
    }
}
