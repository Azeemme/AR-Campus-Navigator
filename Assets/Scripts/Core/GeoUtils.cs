using System;

/// <summary>
/// Static utility class for GPS-based geographic calculations: Haversine distance and bearing.
/// Uses double precision throughout; only casts to float at return for Unity compatibility.
/// </summary>
public static class GeoUtils
{
    /// <summary>Earth radius in meters (WGS84 approximation).</summary>
    public const double EarthRadiusMeters = 6371000.0;

    /// <summary>Conversion factor from degrees to radians.</summary>
    public const double DegToRad = Math.PI / 180.0;

    /// <summary>
    /// Returns the great-circle distance in meters between two GPS coordinates using the Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of first point (degrees).</param>
    /// <param name="lon1">Longitude of first point (degrees).</param>
    /// <param name="lat2">Latitude of second point (degrees).</param>
    /// <param name="lon2">Longitude of second point (degrees).</param>
    /// <returns>Distance in meters as float.</returns>
    public static float HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * DegToRad;
        double dLon = (lon2 - lon1) * DegToRad;
        double lat1Rad = lat1 * DegToRad;
        double lat2Rad = lat2 * DegToRad;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (float)(EarthRadiusMeters * c);
    }

    /// <summary>
    /// Returns the bearing in degrees from point 1 to point 2 (0 = North, 90 = East, 180 = South, 270 = West).
    /// Result is normalized to [0, 360).
    /// </summary>
    /// <param name="lat1">Latitude of origin point (degrees).</param>
    /// <param name="lon1">Longitude of origin point (degrees).</param>
    /// <param name="lat2">Latitude of target point (degrees).</param>
    /// <param name="lon2">Longitude of target point (degrees).</param>
    /// <returns>Bearing in degrees [0, 360) as float.</returns>
    public static float BearingTo(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = (lon2 - lon1) * DegToRad;
        double lat1Rad = lat1 * DegToRad;
        double lat2Rad = lat2 * DegToRad;
        double y = Math.Sin(dLon) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                   Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);
        double bearingRad = Math.Atan2(y, x);
        double bearingDeg = bearingRad * 180.0 / Math.PI;
        double normalized = (bearingDeg + 360.0) % 360.0;
        return (float)normalized;
    }
}
