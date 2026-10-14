using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionStylesAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionStyles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionStyles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionStyleAssignments",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionStyleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionStyleAssignments", x => new { x.QuestionId, x.QuestionStyleId });
                    table.ForeignKey(
                        name: "FK_QuestionStyleAssignments_QuestionStyles_QuestionStyleId",
                        column: x => x.QuestionStyleId,
                        principalTable: "QuestionStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionStyleAssignments_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionStyleAssignments_QuestionStyleId",
                table: "QuestionStyleAssignments",
                column: "QuestionStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionStyles_Code",
                table: "QuestionStyles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionStyles_IsActive_DisplayOrder_Name",
                table: "QuestionStyles",
                columns: new[] { "IsActive", "DisplayOrder", "Name" });

            migrationBuilder.InsertData(
                table: "QuestionStyles",
                columns: new[] { "Id", "Code", "Name", "Description", "IsActive", "DisplayOrder", "CreatedAt" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000001"), "NEW_GENERATION", "Yeni Nesil", "Yeni nesil beceri temelli sorular", true, 1, new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero) },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), "CONCEPTUAL", "Kavramsal", "Kavram ve tanım odaklı sorular", true, 2, new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero) },
                    { new Guid("a0000000-0000-0000-0000-000000000003"), "CALCULATION", "İşlem", "Matematiksel ve formül işlem ağırlıklı sorular", true, 3, new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero) },
                    { new Guid("a0000000-0000-0000-0000-000000000004"), "INTERPRETATION", "Yorum", "Metin, çıkarım ve yorumlama odaklı sorular", true, 4, new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero) },
                    { new Guid("a0000000-0000-0000-0000-000000000005"), "GRAPH_TABLE", "Grafik / Tablo", "Grafik ve tablo analizi içeren sorular", true, 5, new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero) },
                    { new Guid("a0000000-0000-0000-0000-000000000006"), "PROBLEM", "Problem", "Günlük hayat senaryolu problem soruları", true, 6, new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero) },
                    { new Guid("a0000000-0000-0000-0000-000000000007"), "VISUAL", "Görsel", "Şekil, diyagram ve görsel içeren sorular", true, 7, new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero) },
                    { new Guid("a0000000-0000-0000-0000-000000000008"), "MULTI_STEP", "Çok Adımlı", "Birden fazla işlem/kazanım gerektiren çok adımlı sorular", true, 8, new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionStyleAssignments");

            migrationBuilder.DropTable(
                name: "QuestionStyles");
        }
    }
}
