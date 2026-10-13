using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSprint7APersistentStudyPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    RecommendationPolicyVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StudyPlanPolicyVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PlanDays = table.Column<int>(type: "int", nullable: false),
                    DailyStudyMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalAvailableMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalPlannedMinutes = table.Column<int>(type: "int", nullable: false),
                    UnallocatedMinutes = table.Column<int>(type: "int", nullable: false),
                    UtilizationRate = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    ScheduledRecommendationCount = table.Column<int>(type: "int", nullable: false),
                    UnscheduledRecommendationCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StudyPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayIndex = table.Column<int>(type: "int", nullable: false),
                    PlannedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    NodeType = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TopicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubTopicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LearningOutcomeName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TitleSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ClassificationStateSnapshot = table.Column<int>(type: "int", nullable: false),
                    PriorityScoreSnapshot = table.Column<int>(type: "int", nullable: false),
                    RecommendationType = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_StudyTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyTasks_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_InstitutionId",
                table: "StudyPlans",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_StudentId",
                table: "StudyPlans",
                column: "StudentId",
                unique: true,
                filter: "[Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_StudentId_Status",
                table: "StudyPlans",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_InstitutionId",
                table: "StudyTasks",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_StudyPlanId",
                table: "StudyTasks",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyTasks_StudentId_PlannedDate",
                table: "StudyTasks",
                columns: new[] { "StudentId", "PlannedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyTasks");

            migrationBuilder.DropTable(
                name: "StudyPlans");
        }
    }
}
