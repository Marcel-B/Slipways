namespace Slipways.Infrastructure.Osm;

internal static class GeoMath
{
    public const double EarthRadius = 6_371_000;

    /// <summary>Metres along a meridian per degree of latitude.</summary>
    public const double MetresPerDegree = Math.PI * EarthRadius / 180;

    /// <summary>Great-circle distance in metres.</summary>
    public static double Haversine(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var dLat = ToRadians(latitude2 - latitude1);
        var dLon = ToRadians(longitude2 - longitude1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Cos(ToRadians(latitude1)) * Math.Cos(ToRadians(latitude2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * EarthRadius * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
