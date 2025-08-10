using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableTenisWebApp.MigrationsIdentity
{
    /// <inheritdoc />
    public partial class AddIsStartedToTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStarted",
                table: "Tournaments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStarted",
                table: "Tournaments");
        }
    }
}
