using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace com.b_velop.Slipways.DataProvider.Migrations
{
    public partial class AddPorts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PortFk",
                table: "Slipways",
                nullable: true,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Slipways_PortFk",
                table: "Slipways",
                column: "PortFk");

            migrationBuilder.AddForeignKey(
                name: "FK_Slipways_Ports_PortFk",
                table: "Slipways",
                column: "PortFk",
                principalTable: "Ports",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Slipways_Ports_PortFk",
                table: "Slipways");

            migrationBuilder.DropIndex(
                name: "IX_Slipways_PortFk",
                table: "Slipways");

            migrationBuilder.DropColumn(
                name: "PortFk",
                table: "Slipways");
        }
    }
}
