using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class AddResultsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Season = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Round = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Position = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PositionText = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Points = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DriverId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConstructorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Grid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Laps = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Time_Millis = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Time_Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FastestLap_Rank = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FastestLap_Lap = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FastestLap_Time_Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FastestLap_AverageSpeed_Units = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FastestLap_AverageSpeed_Speed = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Results_ConstructorId",
                table: "Results",
                column: "ConstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_DriverId",
                table: "Results",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_Season_Round",
                table: "Results",
                columns: new[] { "Season", "Round" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");
        }
    }
}
