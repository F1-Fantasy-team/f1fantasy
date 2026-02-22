using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusTableAndResultStatusId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatusId",
                table: "Results",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    StatusId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StatusText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Count = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.StatusId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Results_StatusId",
                table: "Results",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_StatusText",
                table: "Statuses",
                column: "StatusText");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Statuses");

            migrationBuilder.DropIndex(
                name: "IX_Results_StatusId",
                table: "Results");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Results");
        }
    }
}
