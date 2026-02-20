using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class AddLapTimingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LapTimings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Season = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Round = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LapNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DriverId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Time = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LapTimings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LapTimings_DriverId",
                table: "LapTimings",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_LapTimings_Season_Round",
                table: "LapTimings",
                columns: new[] { "Season", "Round" });

            migrationBuilder.CreateIndex(
                name: "IX_LapTimings_Season_Round_DriverId",
                table: "LapTimings",
                columns: new[] { "Season", "Round", "DriverId" });

            migrationBuilder.CreateIndex(
                name: "IX_LapTimings_Season_Round_LapNumber",
                table: "LapTimings",
                columns: new[] { "Season", "Round", "LapNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LapTimings");
        }
    }
}
