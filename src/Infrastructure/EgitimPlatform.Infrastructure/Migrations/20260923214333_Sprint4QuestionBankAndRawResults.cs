using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4QuestionBankAndRawResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRawAvailable",
                table: "StudentExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceYear = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_AcademicLearningOutcomes_LearningOutcomeId",
                        column: x => x.LearningOutcomeId,
                        principalTable: "AcademicLearningOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_AcademicSubTopics_SubTopicId",
                        column: x => x.SubTopicId,
                        principalTable: "AcademicSubTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_AcademicSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "AcademicSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_AcademicTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "AcademicTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CorrectOption = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
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
                    table.PrimaryKey("PK_ExamQuestions", x => x.Id);
                    table.UniqueConstraint("AK_ExamQuestions_Id_InstitutionId", x => new { x.Id, x.InstitutionId });
                    table.ForeignKey(
                        name: "FK_ExamQuestions_ExamSections_ExamSectionId_InstitutionId",
                        columns: x => new { x.ExamSectionId, x.InstitutionId },
                        principalTable: "ExamSections",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_Exams_ExamId_InstitutionId",
                        columns: x => new { x.ExamId, x.InstitutionId },
                        principalTable: "Exams",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionLabel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentExamQuestionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentExamAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedOption = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    IsBlank = table.Column<bool>(type: "bit", nullable: false),
                    NetContribution = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_StudentExamQuestionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentExamQuestionResults_ExamQuestions_ExamQuestionId_InstitutionId",
                        columns: x => new { x.ExamQuestionId, x.InstitutionId },
                        principalTable: "ExamQuestions",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamQuestionResults_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamQuestionResults_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamQuestionResults_StudentExamAttempts_StudentExamAttemptId_InstitutionId",
                        columns: x => new { x.StudentExamAttemptId, x.InstitutionId },
                        principalTable: "StudentExamAttempts",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamId_InstitutionId",
                table: "ExamQuestions",
                columns: new[] { "ExamId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamSectionId_InstitutionId",
                table: "ExamQuestions",
                columns: new[] { "ExamSectionId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamSectionId_QuestionId",
                table: "ExamQuestions",
                columns: new[] { "ExamSectionId", "QuestionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamSectionId_QuestionNumber",
                table: "ExamQuestions",
                columns: new[] { "ExamSectionId", "QuestionNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_InstitutionId_ExamId",
                table: "ExamQuestions",
                columns: new[] { "InstitutionId", "ExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_InstitutionId_QuestionId",
                table: "ExamQuestions",
                columns: new[] { "InstitutionId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_QuestionId",
                table: "ExamQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId_OptionLabel",
                table: "QuestionOptions",
                columns: new[] { "QuestionId", "OptionLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_InstitutionId_Status",
                table: "Questions",
                columns: new[] { "InstitutionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_InstitutionId_SubjectId_TopicId",
                table: "Questions",
                columns: new[] { "InstitutionId", "SubjectId", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_LearningOutcomeId",
                table: "Questions",
                column: "LearningOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SourceType_SourceYear",
                table: "Questions",
                columns: new[] { "SourceType", "SourceYear" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SubjectId_Difficulty",
                table: "Questions",
                columns: new[] { "SubjectId", "Difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SubTopicId",
                table: "Questions",
                column: "SubTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TopicId",
                table: "Questions",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamQuestionResults_ExamQuestionId_InstitutionId",
                table: "StudentExamQuestionResults",
                columns: new[] { "ExamQuestionId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamQuestionResults_InstitutionId_StudentExamAttemptId",
                table: "StudentExamQuestionResults",
                columns: new[] { "InstitutionId", "StudentExamAttemptId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamQuestionResults_QuestionId",
                table: "StudentExamQuestionResults",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamQuestionResults_StudentExamAttemptId_ExamQuestionId",
                table: "StudentExamQuestionResults",
                columns: new[] { "StudentExamAttemptId", "ExamQuestionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamQuestionResults_StudentExamAttemptId_InstitutionId",
                table: "StudentExamQuestionResults",
                columns: new[] { "StudentExamAttemptId", "InstitutionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "StudentExamQuestionResults");

            migrationBuilder.DropTable(
                name: "ExamQuestions");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropColumn(
                name: "IsRawAvailable",
                table: "StudentExamAttempts");
        }
    }
}

