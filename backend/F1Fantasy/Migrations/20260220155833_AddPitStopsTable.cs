using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class AddPitStopsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PitStops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Season = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Round = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DriverId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Lap = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Stop = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Time = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Duration = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitStops", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PitStops_DriverId",
                table: "PitStops",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_PitStops_Season_Round",
                table: "PitStops",
                columns: new[] { "Season", "Round" });

            migrationBuilder.CreateIndex(
                name: "IX_PitStops_Season_Round_DriverId",
                table: "PitStops",
                columns: new[] { "Season", "Round", "DriverId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PitStops");
        }
    }
}
