using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EgitimPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClosurePatchP101 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Coaches_AspNetUsers_UserId",
                table: "Coaches",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coaches_AspNetUsers_UserId",
                table: "Coaches");
        }
    }
}
