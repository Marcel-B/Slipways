using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slipways.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JobStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobStates",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastSuccess = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastFailure = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStates", x => x.Name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobStates");
        }
    }
}
