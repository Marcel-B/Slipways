using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace com.b_velop.Slipways.DataProvider.Migrations
{
    public partial class SlipwaysPortFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PortFk",
                table: "Slipways",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PortFk",
                table: "Slipways",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);
        }
    }
}
