using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_DormAllocations_And_UpgradeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DormPreferences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<int>(
                name: "ResponseWindowDays",
                table: "AllocationPeriods",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "Round1Date",
                table: "AllocationPeriods",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "'2026-09-15 00:00:00+00'");

            migrationBuilder.CreateTable(
                name: "DormAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DormitoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AllocatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DormAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DormAllocations_AllocationPeriods_AllocationPeriodId",
                        column: x => x.AllocationPeriodId,
                        principalTable: "AllocationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DormAllocations_Dormitories_DormitoryId",
                        column: x => x.DormitoryId,
                        principalTable: "Dormitories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DormAllocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeRequests_AllocationPeriods_AllocationPeriodId",
                        column: x => x.AllocationPeriodId,
                        principalTable: "AllocationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UpgradeRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeRequestTargets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UpgradeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    DormitoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeRequestTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpgradeRequestTargets_Dormitories_DormitoryId",
                        column: x => x.DormitoryId,
                        principalTable: "Dormitories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UpgradeRequestTargets_UpgradeRequests_UpgradeRequestId",
                        column: x => x.UpgradeRequestId,
                        principalTable: "UpgradeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DormAllocation_ActiveOnePerUserPeriod",
                table: "DormAllocations",
                columns: new[] { "UserId", "AllocationPeriodId" },
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Accepted')");

            migrationBuilder.CreateIndex(
                name: "IX_DormAllocations_AllocationPeriodId",
                table: "DormAllocations",
                column: "AllocationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_DormAllocations_AllocationPeriodId_Round",
                table: "DormAllocations",
                columns: new[] { "AllocationPeriodId", "Round" });

            migrationBuilder.CreateIndex(
                name: "IX_DormAllocations_DormitoryId",
                table: "DormAllocations",
                column: "DormitoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequest_OneActivePerUserPeriod",
                table: "UpgradeRequests",
                columns: new[] { "UserId", "AllocationPeriodId" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequests_AllocationPeriodId",
                table: "UpgradeRequests",
                column: "AllocationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequestTargets_DormitoryId",
                table: "UpgradeRequestTargets",
                column: "DormitoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequestTargets_UpgradeRequestId_DormitoryId",
                table: "UpgradeRequestTargets",
                columns: new[] { "UpgradeRequestId", "DormitoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeRequestTargets_UpgradeRequestId_Rank",
                table: "UpgradeRequestTargets",
                columns: new[] { "UpgradeRequestId", "Rank" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DormAllocations");

            migrationBuilder.DropTable(
                name: "UpgradeRequestTargets");

            migrationBuilder.DropTable(
                name: "UpgradeRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DormPreferences");

            migrationBuilder.DropColumn(
                name: "ResponseWindowDays",
                table: "AllocationPeriods");

            migrationBuilder.DropColumn(
                name: "Round1Date",
                table: "AllocationPeriods");
        }
    }
}
