using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class UpdateZeroPointerToDriverList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Driver1Id",
                table: "ZeroPointerPredictions");

            migrationBuilder.DropColumn(
                name: "Driver2Id",
                table: "ZeroPointerPredictions");

            migrationBuilder.AddColumn<string>(
                name: "DriverIds",
                table: "ZeroPointerPredictions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverIds",
                table: "ZeroPointerPredictions");

            migrationBuilder.AddColumn<string>(
                name: "Driver1Id",
                table: "ZeroPointerPredictions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Driver2Id",
                table: "ZeroPointerPredictions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
