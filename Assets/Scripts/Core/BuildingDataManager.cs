using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager responsible for loading and providing access to building metadata.
/// Loads `buildings.json` from Resources at startup and exposes a list of BuildingRecord entries.
/// </summary>
public class BuildingDataManager : MonoBehaviour
{
    private const string BuildingsResourceName = "buildings";

    private const double MinLatitude = 40.40;
    private const double MaxLatitude = 40.45;
    private const double MinLongitude = -86.95;
    private const double MaxLongitude = -86.88;

    /// <summary>
    /// Singleton instance of the BuildingDataManager.
    /// </summary>
    public static BuildingDataManager Instance { get; private set; }

    /// <summary>
    /// Loaded building records. Empty list if loading fails or JSON is missing.
    /// </summary>
    public List<BuildingRecord> Buildings { get; private set; }

    /// <summary>
    /// Indicates whether an attempt to load building data has completed.
    /// </summary>
    public bool IsLoaded { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadBuildings();
    }

    /// <summary>
    /// Loads buildings from `Resources/buildings.json` and populates the Buildings list.
    /// This method is resilient to missing files and JSON parse errors.
    /// </summary>
    public void LoadBuildings()
    {
        if (Buildings == null)
        {
            Buildings = new List<BuildingRecord>();
        }
        else
        {
            Buildings.Clear();
        }

        TextAsset jsonAsset = Resources.Load<TextAsset>(BuildingsResourceName);
        if (jsonAsset == null)
        {
            Debug.LogError("[BuildingDataManager] buildings.json not found in Resources.");
            IsLoaded = true;
            return;
        }

        try
        {
            BuildingDatabase database = JsonUtility.FromJson<BuildingDatabase>(jsonAsset.text);
            if (database == null || database.buildings == null)
            {
                Debug.LogError("[BuildingDataManager] Failed to parse buildings JSON: database or buildings array was null.");
                IsLoaded = true;
                return;
            }

            for (int i = 0; i < database.buildings.Length; i++)
            {
                BuildingRecord record = database.buildings[i];
                if (record == null)
                {
                    Debug.LogWarning($"[BuildingDataManager] Null building record at index {i}.");
                    continue;
                }

                ValidateRecord(record);
                Buildings.Add(record);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BuildingDataManager] Failed to parse buildings JSON: {ex.Message}");
        }
        finally
        {
            IsLoaded = true;
            Debug.Log($"Loaded {Buildings.Count} buildings");
        }
    }

    /// <summary>
    /// Returns buildings within maxRadius meters of the specified user position.
    /// </summary>
    /// <param name="userLat">User latitude in degrees.</param>
    /// <param name="userLon">User longitude in degrees.</param>
    /// <param name="maxRadius">Maximum radius in meters.</param>
    /// <returns>List of buildings within the specified radius.</returns>
    public List<BuildingRecord> GetBuildingsInRange(double userLat, double userLon, float maxRadius)
    {
        List<BuildingRecord> inRange = new List<BuildingRecord>();
        if (Buildings == null || Buildings.Count == 0)
        {
            return inRange;
        }

        for (int i = 0; i < Buildings.Count; i++)
        {
            BuildingRecord record = Buildings[i];
            if (record == null)
            {
                continue;
            }

            float distance = GeoUtils.HaversineDistance(userLat, userLon, record.latitude, record.longitude);
            if (distance <= maxRadius)
            {
                inRange.Add(record);
            }
        }

        return inRange;
    }

    private static void ValidateRecord(BuildingRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.buildingId))
        {
            Debug.LogWarning("[BuildingDataManager] Building record has empty buildingId.");
        }

        if (record.latitude < MinLatitude || record.latitude > MaxLatitude)
        {
            Debug.LogWarning($"[BuildingDataManager] {record.buildingId} latitude out of bounds: {record.latitude}");
        }

        if (record.longitude < MinLongitude || record.longitude > MaxLongitude)
        {
            Debug.LogWarning($"[BuildingDataManager] {record.buildingId} longitude out of bounds: {record.longitude}");
        }

        if (record.visibilityRadiusMeters <= 0f)
        {
            Debug.LogWarning($"[BuildingDataManager] {record.buildingId} has invalid visibilityRadiusMeters: {record.visibilityRadiusMeters}");
        }
    }
}

