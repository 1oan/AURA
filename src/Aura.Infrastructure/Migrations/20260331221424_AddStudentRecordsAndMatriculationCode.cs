using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentRecordsAndMatriculationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatriculationCode",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatriculationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentRecords_AllocationPeriods_AllocationPeriodId",
                        column: x => x.AllocationPeriodId,
                        principalTable: "AllocationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRecords_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_MatriculationCode",
                table: "Users",
                column: "MatriculationCode",
                unique: true,
                filter: "\"MatriculationCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRecords_AllocationPeriodId",
                table: "StudentRecords",
                column: "AllocationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRecords_FacultyId",
                table: "StudentRecords",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRecords_MatriculationCode_AllocationPeriodId",
                table: "StudentRecords",
                columns: new[] { "MatriculationCode", "AllocationPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentRecords_UserId_AllocationPeriodId",
                table: "StudentRecords",
                columns: new[] { "UserId", "AllocationPeriodId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentRecords");

            migrationBuilder.DropIndex(
                name: "IX_Users_MatriculationCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MatriculationCode",
                table: "Users");
        }
    }
}
