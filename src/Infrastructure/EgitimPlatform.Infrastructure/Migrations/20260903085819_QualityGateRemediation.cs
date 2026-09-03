using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QualityGateRemediation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentCoachAssignments_Coaches_CoachId",
                table: "StudentCoachAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCoachAssignments_Institutions_InstitutionId",
                table: "StudentCoachAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCoachAssignments_Students_StudentId",
                table: "StudentCoachAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Coaches_UserId",
                table: "Coaches");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Students_Id_InstitutionId",
                table: "Students",
                columns: new[] { "Id", "InstitutionId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Coaches_Id_InstitutionId",
                table: "Coaches",
                columns: new[] { "Id", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentCoachAssignments_CoachId_InstitutionId",
                table: "StudentCoachAssignments",
                columns: new[] { "CoachId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentCoachAssignments_StudentId_InstitutionId",
                table: "StudentCoachAssignments",
                columns: new[] { "StudentId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Coaches_UserId_InstitutionId",
                table: "Coaches",
                columns: new[] { "UserId", "InstitutionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCoachAssignments_Coaches_CoachId_InstitutionId",
                table: "StudentCoachAssignments",
                columns: new[] { "CoachId", "InstitutionId" },
                principalTable: "Coaches",
                principalColumns: new[] { "Id", "InstitutionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCoachAssignments_Students_StudentId_InstitutionId",
                table: "StudentCoachAssignments",
                columns: new[] { "StudentId", "InstitutionId" },
                principalTable: "Students",
                principalColumns: new[] { "Id", "InstitutionId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentCoachAssignments_Coaches_CoachId_InstitutionId",
                table: "StudentCoachAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCoachAssignments_Students_StudentId_InstitutionId",
                table: "StudentCoachAssignments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Students_Id_InstitutionId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentCoachAssignments_CoachId_InstitutionId",
                table: "StudentCoachAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentCoachAssignments_StudentId_InstitutionId",
                table: "StudentCoachAssignments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Coaches_Id_InstitutionId",
                table: "Coaches");

            migrationBuilder.DropIndex(
                name: "IX_Coaches_UserId_InstitutionId",
                table: "Coaches");

            migrationBuilder.CreateIndex(
                name: "IX_Coaches_UserId",
                table: "Coaches",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCoachAssignments_Coaches_CoachId",
                table: "StudentCoachAssignments",
                column: "CoachId",
                principalTable: "Coaches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCoachAssignments_Institutions_InstitutionId",
                table: "StudentCoachAssignments",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCoachAssignments_Students_StudentId",
                table: "StudentCoachAssignments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
