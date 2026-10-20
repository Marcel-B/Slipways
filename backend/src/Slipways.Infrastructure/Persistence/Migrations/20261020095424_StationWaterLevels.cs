using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slipways.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StationWaterLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExternalId",
                table: "Stations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WaterLevel",
                table: "Stations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WaterLevelMeasuredAt",
                table: "Stations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaterLevelState",
                table: "Stations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stations_ExternalId",
                table: "Stations",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stations_ExternalId",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "WaterLevel",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "WaterLevelMeasuredAt",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "WaterLevelState",
                table: "Stations");
        }
    }
}
