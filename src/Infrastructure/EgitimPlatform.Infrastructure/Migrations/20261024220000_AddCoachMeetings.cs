using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachMeetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoachMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoachId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MeetingType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachMeetings_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachMeetings_Coaches_CoachId_InstitutionId",
                        columns: x => new { x.CoachId, x.InstitutionId },
                        principalTable: "Coaches",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachMeetings_Students_StudentId_InstitutionId",
                        columns: x => new { x.StudentId, x.InstitutionId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachMeetings_CoachId",
                table: "CoachMeetings",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachMeetings_StudentId",
                table: "CoachMeetings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachMeetings_IsDeleted",
                table: "CoachMeetings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CoachMeetings_InstitutionId_CoachId_StartTime_IsDeleted",
                table: "CoachMeetings",
                columns: new[] { "InstitutionId", "CoachId", "StartTime", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachMeetings_InstitutionId_StudentId_StartTime_IsDeleted",
                table: "CoachMeetings",
                columns: new[] { "InstitutionId", "StudentId", "StartTime", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachMeetings");
        }
    }
}
