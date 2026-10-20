using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Slipways.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seeded stations at 0/0 showed up off the coast of Africa and never got a water level, since
            // PEGELONLINE has no coordinates for them either.
            migrationBuilder.Sql("DELETE FROM Stations WHERE Latitude = 0 AND Longitude = 0;");

            // The original data has latitude and longitude of this slipway in Pirna the wrong way round.
            migrationBuilder.Sql(
                "UPDATE Slipways SET Latitude = Longitude, Longitude = Latitude " +
                "WHERE Id = '0F33747F-531E-4AFB-B020-6AD46DD95320' AND Latitude < 20;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
