using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slipways.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WebsiteInfos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlaceInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlaceType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PlaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OsmType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    OsmId = table.Column<long>(type: "INTEGER", nullable: true),
                    Field = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BaseValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Retrieved = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebsiteChecks",
                columns: table => new
                {
                    PlaceType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PlaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Checked = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteChecks", x => new { x.PlaceType, x.PlaceId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaceInfos_PlaceType_PlaceId",
                table: "PlaceInfos",
                columns: new[] { "PlaceType", "PlaceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaceInfos");

            migrationBuilder.DropTable(
                name: "WebsiteChecks");
        }
    }
}
