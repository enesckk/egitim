using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2Classrooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GradeLevel = table.Column<int>(type: "int", nullable: true),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                    table.UniqueConstraint("AK_Classrooms_Id_InstitutionId", x => new { x.Id, x.InstitutionId });
                    table.ForeignKey(
                        name: "FK_Classrooms_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassroomTeachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ClassroomTeachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomTeachers_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassroomTeachers_Classrooms_ClassroomId_InstitutionId",
                        columns: x => new { x.ClassroomId, x.InstitutionId },
                        principalTable: "Classrooms",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassroomTeachers_Teachers_TeacherId_InstitutionId",
                        columns: x => new { x.TeacherId, x.InstitutionId },
                        principalTable: "Teachers",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassroomStudents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_ClassroomStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomStudents_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassroomStudents_Classrooms_ClassroomId_InstitutionId",
                        columns: x => new { x.ClassroomId, x.InstitutionId },
                        principalTable: "Classrooms",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassroomStudents_Students_StudentId_InstitutionId",
                        columns: x => new { x.StudentId, x.InstitutionId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "InstitutionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_InstitutionId",
                table: "Classrooms",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_IsDeleted",
                table: "Classrooms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_Name_InstitutionId",
                table: "Classrooms",
                columns: new[] { "Name", "InstitutionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomTeachers_ClassroomId_InstitutionId",
                table: "ClassroomTeachers",
                columns: new[] { "ClassroomId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomTeachers_ClassroomId_TeacherId_InstitutionId",
                table: "ClassroomTeachers",
                columns: new[] { "ClassroomId", "TeacherId", "InstitutionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomTeachers_InstitutionId",
                table: "ClassroomTeachers",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomTeachers_IsDeleted",
                table: "ClassroomTeachers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomTeachers_TeacherId_InstitutionId",
                table: "ClassroomTeachers",
                columns: new[] { "TeacherId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomStudents_ClassroomId_InstitutionId",
                table: "ClassroomStudents",
                columns: new[] { "ClassroomId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomStudents_ClassroomId_StudentId_InstitutionId",
                table: "ClassroomStudents",
                columns: new[] { "ClassroomId", "StudentId", "InstitutionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomStudents_InstitutionId",
                table: "ClassroomStudents",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomStudents_IsDeleted",
                table: "ClassroomStudents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomStudents_StudentId_InstitutionId",
                table: "ClassroomStudents",
                columns: new[] { "StudentId", "InstitutionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassroomStudents");

            migrationBuilder.DropTable(
                name: "ClassroomTeachers");

            migrationBuilder.DropTable(
                name: "Classrooms");
        }
    }
}
