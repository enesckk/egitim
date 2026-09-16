using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2StudentProfileGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnrollmentDate",
                table: "Students",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradeLevel",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolName",
                table: "Students",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNumber",
                table: "Students",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetExamTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetScore = table.Column<int>(type: "int", nullable: true),
                    TargetRank = table.Column<int>(type: "int", nullable: true),
                    TargetSchoolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_StudentGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGoals_Students_StudentId_InstitutionId",
                        columns: x => new { x.StudentId, x.InstitutionId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentGoalHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentGoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousValuesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGoalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGoalHistories_StudentGoals_StudentGoalId",
                        column: x => x.StudentGoalId,
                        principalTable: "StudentGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId_GradeLevel",
                table: "Students",
                columns: new[] { "InstitutionId", "GradeLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId_StudentNumber",
                table: "Students",
                columns: new[] { "InstitutionId", "StudentNumber" },
                unique: true,
                filter: "[StudentNumber] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGoalHistories_ChangedAt",
                table: "StudentGoalHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGoalHistories_StudentGoalId",
                table: "StudentGoalHistories",
                column: "StudentGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGoals_InstitutionId",
                table: "StudentGoals",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGoals_InstitutionId_IsDeleted",
                table: "StudentGoals",
                columns: new[] { "InstitutionId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentGoals_IsDeleted",
                table: "StudentGoals",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGoals_StudentId_InstitutionId",
                table: "StudentGoals",
                columns: new[] { "StudentId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentGoals_StudentId_IsActive",
                table: "StudentGoals",
                columns: new[] { "StudentId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentGoalHistories");

            migrationBuilder.DropTable(
                name: "StudentGoals");

            migrationBuilder.DropIndex(
                name: "IX_Students_InstitutionId_GradeLevel",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_InstitutionId_StudentNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "EnrollmentDate",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "GradeLevel",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SchoolName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentNumber",
                table: "Students");
        }
    }
}
