using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderAndDormPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "StudentRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DormPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    DormitoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DormPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DormPreferences_AllocationPeriods_AllocationPeriodId",
                        column: x => x.AllocationPeriodId,
                        principalTable: "AllocationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DormPreferences_Dormitories_DormitoryId",
                        column: x => x.DormitoryId,
                        principalTable: "Dormitories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DormPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DormPreferences_AllocationPeriodId",
                table: "DormPreferences",
                column: "AllocationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_DormPreferences_DormitoryId",
                table: "DormPreferences",
                column: "DormitoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DormPreferences_UserId_AllocationPeriodId_DormitoryId",
                table: "DormPreferences",
                columns: new[] { "UserId", "AllocationPeriodId", "DormitoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DormPreferences_UserId_AllocationPeriodId_Rank",
                table: "DormPreferences",
                columns: new[] { "UserId", "AllocationPeriodId", "Rank" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DormPreferences");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "StudentRecords");
        }
    }
}
