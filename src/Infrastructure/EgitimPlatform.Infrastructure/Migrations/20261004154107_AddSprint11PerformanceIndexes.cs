using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSprint11PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_StudentId_IsCompleted_PlannedDate",
                table: "StudyTasks",
                columns: new[] { "StudentId", "IsCompleted", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachTasks_StudentId_Status_DueDate",
                table: "CoachTasks",
                columns: new[] { "StudentId", "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudyTasks_StudentId_IsCompleted_PlannedDate",
                table: "StudyTasks");

            migrationBuilder.DropIndex(
                name: "IX_CoachTasks_StudentId_Status_DueDate",
                table: "CoachTasks");
        }
    }
}

