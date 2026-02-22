using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace F1Fantasy.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveSeasonsToDriversAndConstructors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "ActiveSeasons",
                table: "Drivers",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.AddColumn<List<string>>(
                name: "ActiveSeasons",
                table: "Constructors",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveSeasons",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "ActiveSeasons",
                table: "Constructors");
        }
    }
}
