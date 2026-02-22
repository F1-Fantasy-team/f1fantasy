using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructorStandingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConstructorStandings",
                columns: table => new
                {
                    Season = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ConstructorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Round = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Position = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PositionText = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Points = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Wins = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructorStandings", x => new { x.Season, x.ConstructorId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConstructorStandings_ConstructorId",
                table: "ConstructorStandings",
                column: "ConstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructorStandings_Season",
                table: "ConstructorStandings",
                column: "Season");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstructorStandings");
        }
    }
}
