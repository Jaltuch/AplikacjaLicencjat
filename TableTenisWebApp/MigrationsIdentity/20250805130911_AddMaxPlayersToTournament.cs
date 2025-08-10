using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableTenisWebApp.MigrationsIdentity
{
    /// <inheritdoc />
    public partial class AddMaxPlayersToTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPlayers",
                table: "Tournaments",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPlayers",
                table: "Tournaments");
        }
    }
}
