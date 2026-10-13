using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3ExamResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExamDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WrongAnswerPenalty = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ScoreSource = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.UniqueConstraint("AK_Exams_Id_InstitutionId", x => new { x.Id, x.InstitutionId });
                    table.ForeignKey(
                        name: "FK_Exams_AcademicExamTypes_ExamTypeId",
                        column: x => x.ExamTypeId,
                        principalTable: "AcademicExamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exams_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    WrongAnswerPenalty = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_ExamSections", x => x.Id);
                    table.UniqueConstraint("AK_ExamSections_Id_InstitutionId", x => new { x.Id, x.InstitutionId });
                    table.ForeignKey(
                        name: "FK_ExamSections_AcademicSubjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "AcademicSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSections_Exams_ExamId_InstitutionId",
                        columns: x => new { x.ExamId, x.InstitutionId },
                        principalTable: "Exams",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSections_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    TakenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalCorrect = table.Column<int>(type: "int", nullable: false),
                    TotalWrong = table.Column<int>(type: "int", nullable: false),
                    TotalBlank = table.Column<int>(type: "int", nullable: false),
                    TotalNet = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    ReportedScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    ScoreSource = table.Column<int>(type: "int", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_StudentExamAttempts", x => x.Id);
                    table.UniqueConstraint("AK_StudentExamAttempts_Id_InstitutionId", x => new { x.Id, x.InstitutionId });
                    table.ForeignKey(
                        name: "FK_StudentExamAttempts_Exams_ExamId_InstitutionId",
                        columns: x => new { x.ExamId, x.InstitutionId },
                        principalTable: "Exams",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamAttempts_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamAttempts_Students_StudentId_InstitutionId",
                        columns: x => new { x.StudentId, x.InstitutionId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentExamSubjectResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentExamAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Correct = table.Column<int>(type: "int", nullable: false),
                    Wrong = table.Column<int>(type: "int", nullable: false),
                    Blank = table.Column<int>(type: "int", nullable: false),
                    Net = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_StudentExamSubjectResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentExamSubjectResults_ExamSections_ExamSectionId_InstitutionId",
                        columns: x => new { x.ExamSectionId, x.InstitutionId },
                        principalTable: "ExamSections",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamSubjectResults_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamSubjectResults_StudentExamAttempts_StudentExamAttemptId_InstitutionId",
                        columns: x => new { x.StudentExamAttemptId, x.InstitutionId },
                        principalTable: "StudentExamAttempts",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentExamTopicResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentExamAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Correct = table.Column<int>(type: "int", nullable: false),
                    Wrong = table.Column<int>(type: "int", nullable: false),
                    Blank = table.Column<int>(type: "int", nullable: false),
                    Net = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_StudentExamTopicResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentExamTopicResults_AcademicTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "AcademicTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamTopicResults_ExamSections_ExamSectionId_InstitutionId",
                        columns: x => new { x.ExamSectionId, x.InstitutionId },
                        principalTable: "ExamSections",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamTopicResults_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentExamTopicResults_StudentExamAttempts_StudentExamAttemptId_InstitutionId",
                        columns: x => new { x.StudentExamAttemptId, x.InstitutionId },
                        principalTable: "StudentExamAttempts",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamTypeId",
                table: "Exams",
                column: "ExamTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_InstitutionId_Code",
                table: "Exams",
                columns: new[] { "InstitutionId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_InstitutionId_ExamDate_Id",
                table: "Exams",
                columns: new[] { "InstitutionId", "ExamDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_InstitutionId_ExamTypeId_ExamDate",
                table: "Exams",
                columns: new[] { "InstitutionId", "ExamTypeId", "ExamDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSections_ExamId_DisplayOrder",
                table: "ExamSections",
                columns: new[] { "ExamId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSections_ExamId_InstitutionId",
                table: "ExamSections",
                columns: new[] { "ExamId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSections_ExamId_SubjectId",
                table: "ExamSections",
                columns: new[] { "ExamId", "SubjectId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSections_InstitutionId",
                table: "ExamSections",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSections_SubjectId",
                table: "ExamSections",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempts_ExamId_InstitutionId",
                table: "StudentExamAttempts",
                columns: new[] { "ExamId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempts_ExamId_StudentId_AttemptNumber",
                table: "StudentExamAttempts",
                columns: new[] { "ExamId", "StudentId", "AttemptNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempts_InstitutionId_ExamId_Status",
                table: "StudentExamAttempts",
                columns: new[] { "InstitutionId", "ExamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempts_InstitutionId_StudentId_TakenAt_Id",
                table: "StudentExamAttempts",
                columns: new[] { "InstitutionId", "StudentId", "TakenAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamAttempts_StudentId_InstitutionId",
                table: "StudentExamAttempts",
                columns: new[] { "StudentId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamSubjectResults_ExamSectionId_InstitutionId",
                table: "StudentExamSubjectResults",
                columns: new[] { "ExamSectionId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamSubjectResults_InstitutionId",
                table: "StudentExamSubjectResults",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamSubjectResults_StudentExamAttemptId_ExamSectionId",
                table: "StudentExamSubjectResults",
                columns: new[] { "StudentExamAttemptId", "ExamSectionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamSubjectResults_StudentExamAttemptId_InstitutionId",
                table: "StudentExamSubjectResults",
                columns: new[] { "StudentExamAttemptId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamTopicResults_ExamSectionId_InstitutionId",
                table: "StudentExamTopicResults",
                columns: new[] { "ExamSectionId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamTopicResults_InstitutionId",
                table: "StudentExamTopicResults",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamTopicResults_StudentExamAttemptId_ExamSectionId_TopicId",
                table: "StudentExamTopicResults",
                columns: new[] { "StudentExamAttemptId", "ExamSectionId", "TopicId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamTopicResults_StudentExamAttemptId_InstitutionId",
                table: "StudentExamTopicResults",
                columns: new[] { "StudentExamAttemptId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentExamTopicResults_TopicId_StudentExamAttemptId",
                table: "StudentExamTopicResults",
                columns: new[] { "TopicId", "StudentExamAttemptId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentExamSubjectResults");

            migrationBuilder.DropTable(
                name: "StudentExamTopicResults");

            migrationBuilder.DropTable(
                name: "ExamSections");

            migrationBuilder.DropTable(
                name: "StudentExamAttempts");

            migrationBuilder.DropTable(
                name: "Exams");
        }
    }
}
