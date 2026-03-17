using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton manager responsible for providing the user's current GPS location and compass heading.
/// Supports both real device data and an editor/development simulation mode.
/// </summary>
public class UserLocationManager : MonoBehaviour
{
    private const float DesiredAccuracyMeters = 5f;
    private const float UpdateDistanceMeters = 1f;
    private const float LocationServiceTimeoutSeconds = 20f;
    private const float GpsSignalLostThresholdSeconds = 10f;

    /// <summary>
    /// Singleton instance of the UserLocationManager.
    /// </summary>
    public static UserLocationManager Instance { get; private set; }

    /// <summary>
    /// Current user latitude in degrees.
    /// </summary>
    public double UserLatitude { get; private set; }

    /// <summary>
    /// Current user longitude in degrees.
    /// </summary>
    public double UserLongitude { get; private set; }

    /// <summary>
    /// Smoothed compass heading in degrees, normalized to [0, 360).
    /// </summary>
    public float SmoothedHeading { get; private set; }

    /// <summary>
    /// Indicates whether a valid GPS fix has been obtained.
    /// </summary>
    public bool HasGPSFix { get; private set; }

    /// <summary>
    /// Indicates whether the GPS/location service is running and enabled.
    /// </summary>
    public bool IsGPSEnabled { get; private set; }

    /// <summary>
    /// Indicates whether the user has denied GPS/location permission.
    /// </summary>
    public bool GPSPermissionDenied { get; private set; }

    /// <summary>
    /// Indicates whether the GPS signal appears to be lost (no updates for a threshold period).
    /// </summary>
    public bool GPSSignalLost { get; private set; }

    /// <summary>
    /// Indicates whether the location service failed to start.
    /// </summary>
    public bool LocationServiceFailed { get; private set; }

    [Header("Simulation")]
    [SerializeField] private bool simulateGPS = false;
    [SerializeField] private double simulatedLatitude = 40.4259;
    [SerializeField] private double simulatedLongitude = -86.9081;
    [SerializeField] private float simulatedHeading = 0f;

    [Header("Compass Smoothing")]
    [SerializeField] private float headingSmoothingFactor = 0.1f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField] private bool useRealCompass = false;
#endif

    private bool hasLoggedCompassNaNWarning;
    private double lastLocationTimestamp;
    private float lastLocationRealtimeSinceStartup;

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
        if (simulateGPS)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UserLatitude = simulatedLatitude;
            UserLongitude = simulatedLongitude;
            SmoothedHeading = simulatedHeading;
            HasGPSFix = true;
            IsGPSEnabled = true;
            GPSPermissionDenied = false;
            LocationServiceFailed = false;
            GPSSignalLost = false;
            return;
#else
            simulateGPS = false;
#endif
        }

        Input.compass.enabled = true;
        StartCoroutine(InitLocationService());
    }

    /// <summary>
    /// Attempts to (re)start the location service after a failure or permission change.
    /// </summary>
    public void RetryStartLocationService()
    {
        if (IsGPSEnabled)
        {
            return;
        }

        StopAllCoroutines();
        LocationServiceFailed = false;
        GPSPermissionDenied = false;
        StartCoroutine(InitLocationService());
    }

    private IEnumerator InitLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            GPSPermissionDenied = true;
            yield break;
        }

        Input.location.Start(DesiredAccuracyMeters, UpdateDistanceMeters);

        float startTime = Time.realtimeSinceStartup;

        while (Input.location.status == LocationServiceStatus.Initializing &&
               Time.realtimeSinceStartup - startTime < LocationServiceTimeoutSeconds)
        {
            yield return null;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            IsGPSEnabled = true;
            LocationServiceFailed = false;
        }
        else
        {
            IsGPSEnabled = false;
            LocationServiceFailed = true;
            Debug.LogError("[UserLocationManager] Failed to start location service or timed out.");
        }
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (simulateGPS)
        {
            UserLatitude = simulatedLatitude;
            UserLongitude = simulatedLongitude;
            HasGPSFix = true;
            IsGPSEnabled = true;
            GPSSignalLost = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (useRealCompass && Input.compass.enabled)
            {
                UpdateHeadingFromCompass(Input.compass.trueHeading);
            }
            else
            {
                SmoothedHeading = NormalizeHeading(simulatedHeading);
            }
#else
            SmoothedHeading = NormalizeHeading(simulatedHeading);
#endif
            return;
        }
#endif

        if (!IsGPSEnabled)
        {
            return;
        }

        LocationInfo lastData = Input.location.lastData;
        UserLatitude = lastData.latitude;
        UserLongitude = lastData.longitude;

        if (!HasGPSFix)
        {
            HasGPSFix = true;
            GPSSignalLost = false;
        }

        float now = Time.realtimeSinceStartup;
        if (System.Math.Abs(lastData.timestamp - lastLocationTimestamp) > double.Epsilon)
        {
            lastLocationTimestamp = lastData.timestamp;
            lastLocationRealtimeSinceStartup = now;
            GPSSignalLost = false;
        }
        else if (HasGPSFix && now - lastLocationRealtimeSinceStartup > GpsSignalLostThresholdSeconds)
        {
            GPSSignalLost = true;
        }

        float rawHeading = Input.compass.trueHeading;
        UpdateHeadingFromCompass(rawHeading);
    }

    /// <summary>
    /// Adjusts the simulated heading by the specified delta in degrees, wrapping to [0, 360).
    /// </summary>
    /// <param name="deltaDegrees">Delta in degrees to add to the simulated heading.</param>
    public void AdjustSimulatedHeading(float deltaDegrees)
    {
        simulatedHeading = NormalizeHeading(simulatedHeading + deltaDegrees);
    }

    private void UpdateHeadingFromCompass(float rawHeading)
    {
        if (float.IsNaN(rawHeading))
        {
            if (!hasLoggedCompassNaNWarning)
            {
                Debug.LogWarning("[UserLocationManager] Compass returned NaN heading. Using last known SmoothedHeading.");
                hasLoggedCompassNaNWarning = true;
            }

            return;
        }

        hasLoggedCompassNaNWarning = false;

        float delta = Mathf.DeltaAngle(SmoothedHeading, rawHeading);
        float smoothed = SmoothedHeading + delta * headingSmoothingFactor;
        SmoothedHeading = NormalizeHeading(smoothed);
    }

    private static float NormalizeHeading(float heading)
    {
        float normalized = (heading % 360f + 360f) % 360f;
        return normalized;
    }
}

