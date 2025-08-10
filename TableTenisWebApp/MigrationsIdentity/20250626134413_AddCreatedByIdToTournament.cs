using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableTenisWebApp.MigrationsIdentity
{
    /// <inheritdoc />
    public partial class AddCreatedByIdToTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Tournaments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_CreatedById",
                table: "Tournaments",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_AspNetUsers_CreatedById",
                table: "Tournaments",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_AspNetUsers_CreatedById",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_CreatedById",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Tournaments");
        }
    }
}
