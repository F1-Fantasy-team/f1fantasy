using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class AddQualifyingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Qualifyings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Season = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Round = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Position = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DriverId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConstructorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Q1 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Q2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Q3 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualifyings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Qualifyings_ConstructorId",
                table: "Qualifyings",
                column: "ConstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualifyings_DriverId",
                table: "Qualifyings",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualifyings_Season_Round",
                table: "Qualifyings",
                columns: new[] { "Season", "Round" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Qualifyings");
        }
    }
}
