using Aura.Application.Common.Interfaces;
using Aura.Infrastructure.Auth;
using Aura.Infrastructure.Email;
using Aura.Infrastructure.External.Spotify;
using Aura.Infrastructure.Persistence;
using Aura.Infrastructure.Persistence.Repositories;
using Aura.Infrastructure.Persistence.Seeding;
using Aura.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Aura.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AuraDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseVector();
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

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
        services.AddHttpClient<ISpotifyClient, SpotifyClient>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.Configure<FrontendSettings>(configuration.GetSection("Frontend"));
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<DataSeeder>();

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<AllocationSchedulerService>();
        services.AddHostedService<AllocationExpirationService>();

        return services;
    }
}
