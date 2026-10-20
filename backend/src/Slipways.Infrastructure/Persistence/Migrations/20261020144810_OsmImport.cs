using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slipways.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OsmImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "WaterId",
                table: "Slipways",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Access",
                table: "Slipways",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fee",
                table: "Slipways",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningHours",
                table: "Slipways",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Operator",
                table: "Slipways",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OsmId",
                table: "Slipways",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OsmType",
                table: "Slipways",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Slipways",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<string>(
                name: "Surface",
                table: "Slipways",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Slipways",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WaterId",
                table: "Marinas",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Access",
                table: "Marinas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fee",
                table: "Marinas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpeningHours",
                table: "Marinas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Operator",
                table: "Marinas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OsmId",
                table: "Marinas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OsmType",
                table: "Marinas",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Marinas",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.CreateIndex(
                name: "IX_Slipways_OsmType_OsmId",
                table: "Slipways",
                columns: new[] { "OsmType", "OsmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marinas_OsmType_OsmId",
                table: "Marinas",
                columns: new[] { "OsmType", "OsmId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Slipways_OsmType_OsmId",
                table: "Slipways");

            migrationBuilder.DropIndex(
                name: "IX_Marinas_OsmType_OsmId",
                table: "Marinas");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "OpeningHours",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "Operator",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "OsmId",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "OsmType",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "Surface",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "Marinas");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "Marinas");

            migrationBuilder.DropColumn(
                name: "OpeningHours",
                table: "Marinas");

            migrationBuilder.DropColumn(
                name: "Operator",
                table: "Marinas");

            migrationBuilder.DropColumn(
                name: "OsmId",
                table: "Marinas");

            migrationBuilder.DropColumn(
                name: "OsmType",
                table: "Marinas");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Marinas");

            migrationBuilder.AlterColumn<Guid>(
                name: "WaterId",
                table: "Slipways",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WaterId",
                table: "Marinas",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
