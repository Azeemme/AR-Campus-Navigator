/// <summary>
/// Serializable data record representing a single building entry loaded from JSON.
/// </summary>
[System.Serializable]
public class BuildingRecord
{
    /// <summary>Unique building identifier (uppercase abbreviation).</summary>
    public string buildingId;

    /// <summary>Full display name of the building.</summary>
    public string name;

    /// <summary>Short display name of the building.</summary>
    public string shortName;

    /// <summary>Latitude in degrees (double precision).</summary>
    public double latitude;

    /// <summary>Longitude in degrees (double precision).</summary>
    public double longitude;

    /// <summary>Department or category associated with the building.</summary>
    public string department;

    /// <summary>Human-readable hours string.</summary>
    public string hours;

    /// <summary>Short fun fact string shown when the label is expanded.</summary>
    public string funFact;

    /// <summary>Maximum visibility radius in meters for showing this building's label.</summary>
    public float visibilityRadiusMeters;
}

/// <summary>
/// Wrapper class for JsonUtility deserialization of the buildings database JSON payload.
/// </summary>
[System.Serializable]
public class BuildingDatabase
{
    /// <summary>Array of building records loaded from JSON.</summary>
    public BuildingRecord[] buildings;
}

