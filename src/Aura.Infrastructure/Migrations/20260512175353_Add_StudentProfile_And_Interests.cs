using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Aura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_StudentProfile_And_Interests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Interests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpotifySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TopArtists = table.Column<string>(type: "jsonb", nullable: false),
                    TopTracks = table.Column<string>(type: "jsonb", nullable: false),
                    TopGenres = table.Column<string>(type: "jsonb", nullable: false),
                    AvgEnergy = table.Column<decimal>(type: "numeric", nullable: true),
                    AvgValence = table.Column<decimal>(type: "numeric", nullable: true),
                    AvgDanceability = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotifySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(384)", nullable: true),
                    LastEmbeddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEmbeddings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SleepSchedule = table.Column<int>(type: "integer", nullable: true),
                    WakeUpTime = table.Column<int>(type: "integer", nullable: true),
                    CleanlinessLevel = table.Column<int>(type: "integer", nullable: true),
                    NoiseTolerance = table.Column<int>(type: "integer", nullable: true),
                    StudyLocation = table.Column<int>(type: "integer", nullable: true),
                    GuestFrequency = table.Column<int>(type: "integer", nullable: true),
                    SmokingHabit = table.Column<int>(type: "integer", nullable: true),
                    LifestyleCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TipiAnswers = table.Column<string>(type: "jsonb", nullable: false),
                    TipiExtraversion = table.Column<decimal>(type: "numeric", nullable: true),
                    TipiAgreeableness = table.Column<decimal>(type: "numeric", nullable: true),
                    TipiConscientiousness = table.Column<decimal>(type: "numeric", nullable: true),
                    TipiEmotionalStability = table.Column<decimal>(type: "numeric", nullable: true),
                    TipiOpenness = table.Column<decimal>(type: "numeric", nullable: true),
                    TipiCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InterestSlugs = table.Column<string>(type: "jsonb", nullable: false),
                    InterestsCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SpotifyAccessToken = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SpotifyRefreshToken = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SpotifyTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SpotifyConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SpotifyScopes = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfiles", x => x.Id);
                    table.CheckConstraint("CK_StudentProfile_Cleanliness_Range", "\"CleanlinessLevel\" IS NULL OR \"CleanlinessLevel\" BETWEEN 1 AND 5");
                });

            migrationBuilder.InsertData(
                table: "Interests",
                columns: new[] { "Id", "Category", "CreatedAt", "DisplayOrder", "IsActive", "Label", "Slug" },
                values: new object[,]
                {
                    { new Guid("a0000001-0001-0001-0001-000000000001"), "sports", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Football", "football" },
                    { new Guid("a0000001-0001-0001-0001-000000000002"), "sports", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Basketball", "basketball" },
                    { new Guid("a0000001-0001-0001-0001-000000000003"), "sports", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "Tennis", "tennis" },
                    { new Guid("a0000001-0001-0001-0001-000000000004"), "sports", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "Running", "running" },
                    { new Guid("a0000001-0001-0001-0001-000000000005"), "sports", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "Gym", "gym" },
                    { new Guid("a0000001-0001-0001-0001-000000000006"), "sports", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "Swimming", "swimming" },
                    { new Guid("a0000001-0001-0001-0001-000000000007"), "sports", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, "Hiking", "hiking" },
                    { new Guid("a0000001-0001-0001-0001-000000000008"), "sports", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, "Cycling", "cycling" },
                    { new Guid("a0000002-0002-0002-0002-000000000001"), "entertainment", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Movies", "movies" },
                    { new Guid("a0000002-0002-0002-0002-000000000002"), "entertainment", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "TV Series", "tv-series" },
                    { new Guid("a0000002-0002-0002-0002-000000000003"), "entertainment", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "Anime", "anime" },
                    { new Guid("a0000002-0002-0002-0002-000000000004"), "entertainment", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "Gaming", "gaming" },
                    { new Guid("a0000002-0002-0002-0002-000000000005"), "entertainment", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "Board Games", "board-games" },
                    { new Guid("a0000002-0002-0002-0002-000000000006"), "entertainment", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "Podcasts", "podcasts" },
                    { new Guid("a0000002-0002-0002-0002-000000000007"), "entertainment", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, "Live Music", "live-music" },
                    { new Guid("a0000002-0002-0002-0002-000000000008"), "entertainment", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, "Reading", "reading" },
                    { new Guid("a0000003-0003-0003-0003-000000000001"), "arts", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Drawing", "drawing" },
                    { new Guid("a0000003-0003-0003-0003-000000000002"), "arts", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Photography", "photography" },
                    { new Guid("a0000003-0003-0003-0003-000000000003"), "arts", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "Writing", "writing" },
                    { new Guid("a0000003-0003-0003-0003-000000000004"), "arts", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "Playing music", "music-playing" },
                    { new Guid("a0000003-0003-0003-0003-000000000005"), "arts", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "Theater", "theater" },
                    { new Guid("a0000003-0003-0003-0003-000000000006"), "arts", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, "Crafts", "crafts" },
                    { new Guid("a0000004-0004-0004-0004-000000000001"), "academic", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Programming", "programming" },
                    { new Guid("a0000004-0004-0004-0004-000000000002"), "academic", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Science", "science" },
                    { new Guid("a0000004-0004-0004-0004-000000000003"), "academic", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "History", "history" },
                    { new Guid("a0000004-0004-0004-0004-000000000004"), "academic", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "Languages", "languages" },
                    { new Guid("a0000004-0004-0004-0004-000000000005"), "academic", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "Mathematics", "mathematics" },
                    { new Guid("a0000005-0005-0005-0005-000000000001"), "lifestyle", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "Cooking", "cooking" },
                    { new Guid("a0000005-0005-0005-0005-000000000002"), "lifestyle", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Traveling", "traveling" },
                    { new Guid("a0000005-0005-0005-0005-000000000003"), "lifestyle", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, "Volunteering", "volunteering" },
                    { new Guid("a0000005-0005-0005-0005-000000000004"), "lifestyle", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, "Pets", "pets" },
                    { new Guid("a0000005-0005-0005-0005-000000000005"), "lifestyle", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, "Fashion", "fashion" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interests_Category_DisplayOrder_IsActive",
                table: "Interests",
                columns: new[] { "Category", "DisplayOrder", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Interests_Slug",
                table: "Interests",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpotifySnapshots_UserId_FetchedAt",
                table: "SpotifySnapshots",
                columns: new[] { "UserId", "FetchedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEmbeddings_UserId",
                table: "StudentEmbeddings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_UserId",
                table: "StudentProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Interests");

            migrationBuilder.DropTable(
                name: "SpotifySnapshots");

            migrationBuilder.DropTable(
                name: "StudentEmbeddings");

            migrationBuilder.DropTable(
                name: "StudentProfiles");
        }
    }
}
