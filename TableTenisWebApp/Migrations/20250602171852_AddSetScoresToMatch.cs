using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableTenisWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSetScoresToMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SetScores",
                table: "Matches",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SetScores",
                table: "Matches");
        }
    }
}
