using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Circuits",
                columns: table => new
                {
                    CircuitId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CircuitName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location_Lat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location_Long = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location_Locality = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location_Country = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circuits", x => x.CircuitId);
                });

            migrationBuilder.CreateTable(
                name: "Constructors",
                columns: table => new
                {
                    ConstructorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Constructors", x => x.ConstructorId);
                });

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    DriverId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PermanentNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GivenName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FamilyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.DriverId);
                });

            migrationBuilder.CreateTable(
                name: "Races",
                columns: table => new
                {
                    Season = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Round = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RaceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Date = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstPractice_Date = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FirstPractice_Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SecondPractice_Date = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SecondPractice_Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ThirdPractice_Date = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ThirdPractice_Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Qualifying_Date = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Qualifying_Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Sprint_Date = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Sprint_Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SprintQualifying_Date = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SprintQualifying_Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => new { x.Season, x.Round });
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Year = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Year);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Circuits");

            migrationBuilder.DropTable(
                name: "Constructors");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "Races");

            migrationBuilder.DropTable(
                name: "Seasons");
        }
    }
}
