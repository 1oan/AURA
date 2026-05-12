using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_RoomAssignments_And_AnchorRoomId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AnchorRoomId",
                table: "RoommateGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPreCloseWarningSentAt",
                table: "DormAllocations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoomAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoommateGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomAssignments_AllocationPeriods_AllocationPeriodId",
                        column: x => x.AllocationPeriodId,
                        principalTable: "AllocationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomAssignments_RoommateGroups_RoommateGroupId",
                        column: x => x.RoommateGroupId,
                        principalTable: "RoommateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RoomAssignments_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoomAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoommateGroups_AnchorRoomId",
                table: "RoommateGroups",
                column: "AnchorRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_AllocationPeriodId",
                table: "RoomAssignments",
                column: "AllocationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_RoomId_AllocationPeriodId",
                table: "RoomAssignments",
                columns: new[] { "RoomId", "AllocationPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_RoommateGroupId",
                table: "RoomAssignments",
                column: "RoommateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAssignments_UserId_AllocationPeriodId",
                table: "RoomAssignments",
                columns: new[] { "UserId", "AllocationPeriodId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RoommateGroups_Rooms_AnchorRoomId",
                table: "RoommateGroups",
                column: "AnchorRoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoommateGroups_Rooms_AnchorRoomId",
                table: "RoommateGroups");

            migrationBuilder.DropTable(
                name: "RoomAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RoommateGroups_AnchorRoomId",
                table: "RoommateGroups");

            migrationBuilder.DropColumn(
                name: "AnchorRoomId",
                table: "RoommateGroups");

            migrationBuilder.DropColumn(
                name: "LastPreCloseWarningSentAt",
                table: "DormAllocations");
        }
    }
}
