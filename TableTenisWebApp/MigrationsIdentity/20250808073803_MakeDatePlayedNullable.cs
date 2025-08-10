using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TableTenisWebApp.MigrationsIdentity
{
    /// <inheritdoc />
    public partial class MakeDatePlayedNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStarted",
                table: "Tournaments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStarted",
                table: "Tournaments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
