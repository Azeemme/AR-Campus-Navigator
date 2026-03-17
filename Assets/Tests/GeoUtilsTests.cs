using NUnit.Framework;

/// <summary>
/// EditMode unit tests for GeoUtils (Haversine distance and bearing).
/// Run via Window > General > Test Runner > EditMode.
/// </summary>
public static class GeoUtilsTests
{
    // Reference coordinates (Purdue campus)
    private const double PMULat = 40.4259;
    private const double PMULon = -86.9081;
    private const double LawsonLat = 40.4276;
    private const double LawsonLon = -86.9132;
    private const double RossAdeLat = 40.4396;
    private const double RossAdeLon = -86.9211;

    [Test]
    public static void TestHaversine_SamePoint_ReturnsZero()
    {
        float distance = GeoUtils.HaversineDistance(PMULat, PMULon, PMULat, PMULon);
        Assert.AreEqual(0f, distance, 0.001f);
    }

    [Test]
    public static void TestHaversine_PMUtoLawson()
    {
        float distance = GeoUtils.HaversineDistance(PMULat, PMULon, LawsonLat, LawsonLon);
        float expected = 480f;
        float tolerance = expected * 0.05f; // 5%
        Assert.AreEqual(expected, distance, tolerance, "PMU to Lawson should be ~480 m");
    }

    [Test]
    public static void TestHaversine_PMUtoRossAde()
    {
        float distance = GeoUtils.HaversineDistance(PMULat, PMULon, RossAdeLat, RossAdeLon);
        float expected = 1880f;
        float tolerance = expected * 0.05f; // 5%
        Assert.AreEqual(expected, distance, tolerance, "PMU to Ross-Ade should be ~1880 m");
    }

    [Test]
    public static void TestHaversine_Symmetry()
    {
        float ab = GeoUtils.HaversineDistance(PMULat, PMULon, LawsonLat, LawsonLon);
        float ba = GeoUtils.HaversineDistance(LawsonLat, LawsonLon, PMULat, PMULon);
        Assert.AreEqual(ab, ba, 0.001f, "Haversine(A,B) should equal Haversine(B,A)");
    }

    [Test]
    public static void TestBearing_DueNorth()
    {
        // Point 2 is north of point 1 (higher latitude, same longitude)
        float bearing = GeoUtils.BearingTo(40.0, -86.0, 41.0, -86.0);
        Assert.AreEqual(0f, bearing, 1f, "Bearing due north should be ~0°");
    }

    [Test]
    public static void TestBearing_DueEast()
    {
        // Point 2 is east of point 1 (same latitude, higher longitude)
        float bearing = GeoUtils.BearingTo(40.0, -86.0, 40.0, -85.0);
        Assert.AreEqual(90f, bearing, 1f, "Bearing due east should be ~90°");
    }

    [Test]
    public static void TestBearing_DueSouth()
    {
        // Point 2 is south of point 1 (lower latitude, same longitude)
        float bearing = GeoUtils.BearingTo(41.0, -86.0, 40.0, -86.0);
        Assert.AreEqual(180f, bearing, 1f, "Bearing due south should be ~180°");
    }

    [Test]
    public static void TestBearing_DueWest()
    {
        // Point 2 is west of point 1 (same latitude, lower longitude)
        float bearing = GeoUtils.BearingTo(40.0, -85.0, 40.0, -86.0);
        Assert.AreEqual(270f, bearing, 1f, "Bearing due west should be ~270°");
    }

    [Test]
    public static void TestBearing_PMUtoLawson()
    {
        float bearing = GeoUtils.BearingTo(PMULat, PMULon, LawsonLat, LawsonLon);
        float expected = 300f; // Northwest
        float tolerance = 15f;
        Assert.AreEqual(expected, bearing, tolerance, "PMU to Lawson bearing should be ~300° (northwest)");
    }

    [Test]
    public static void TestBearing_RangeAlwaysNormalized()
    {
        float bearingN = GeoUtils.BearingTo(40.0, -86.0, 41.0, -86.0);
        float bearingE = GeoUtils.BearingTo(40.0, -86.0, 40.0, -85.0);
        float bearingS = GeoUtils.BearingTo(41.0, -86.0, 40.0, -86.0);
        float bearingW = GeoUtils.BearingTo(40.0, -85.0, 40.0, -86.0);
        float bearingPMULawson = GeoUtils.BearingTo(PMULat, PMULon, LawsonLat, LawsonLon);

        Assert.GreaterOrEqual(bearingN, 0f, "Bearing should be >= 0");
        Assert.Less(bearingN, 360f, "Bearing should be < 360");
        Assert.GreaterOrEqual(bearingE, 0f);
        Assert.Less(bearingE, 360f);
        Assert.GreaterOrEqual(bearingS, 0f);
        Assert.Less(bearingS, 360f);
        Assert.GreaterOrEqual(bearingW, 0f);
        Assert.Less(bearingW, 360f);
        Assert.GreaterOrEqual(bearingPMULawson, 0f);
        Assert.Less(bearingPMULawson, 360f);
    }
}
