using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slipways.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WaterShapes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OsmElements",
                table: "Waters",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Shape",
                table: "Waters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShapeUpdated",
                table: "Waters",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OsmElements",
                table: "Waters");

            migrationBuilder.DropColumn(
                name: "Shape",
                table: "Waters");

            migrationBuilder.DropColumn(
                name: "ShapeUpdated",
                table: "Waters");
        }
    }
}
