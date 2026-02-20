using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSprintToResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSprint",
                table: "Results",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Results_Season_Round_IsSprint",
                table: "Results",
                columns: new[] { "Season", "Round", "IsSprint" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Results_Season_Round_IsSprint",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "IsSprint",
                table: "Results");
        }
    }
}
