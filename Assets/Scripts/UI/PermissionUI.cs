using UnityEngine;

/// <summary>
/// Controls the full-screen permission error UI shown when location permission is denied.
/// </summary>
public class PermissionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject rootPanel;

    private UserLocationManager locationManager;
    private bool wasPermissionDenied;

    private void Start()
    {
        locationManager = UserLocationManager.Instance;

        if (rootPanel == null)
        {
            rootPanel = gameObject;
        }

        bool shouldShow = locationManager != null && locationManager.GPSPermissionDenied;
        SetPanelVisible(shouldShow);
        wasPermissionDenied = shouldShow;
    }

    private void Update()
    {
        if (locationManager == null)
        {
            locationManager = UserLocationManager.Instance;
            if (locationManager == null)
            {
                return;
            }
        }

        bool permissionDenied = locationManager.GPSPermissionDenied;
        SetPanelVisible(permissionDenied);

        if (wasPermissionDenied && !permissionDenied)
        {
            locationManager.RetryStartLocationService();
        }

        wasPermissionDenied = permissionDenied;
    }

    /// <summary>
    /// Opens the Android app settings page so the user can grant location permission.
    /// In the Unity Editor, logs a stub message instead.
    /// </summary>
    public void OpenSettings()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            var intent = new AndroidJavaObject(
                "android.content.Intent",
                "android.settings.APPLICATION_DETAILS_SETTINGS");
            var uri = new AndroidJavaClass("android.net.Uri")
                .CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier);
            intent.Call<AndroidJavaObject>("setData", uri);
            currentActivity.Call("startActivity", intent);
        }
#else
        Debug.Log("[PermissionUI] Would open Android app settings (Editor stub).");
#endif
    }

    /// <summary>
    /// Retries starting the GPS/location service after the user taps the Retry button.
    /// </summary>
    public void Retry()
    {
        if (locationManager == null)
        {
            locationManager = UserLocationManager.Instance;
        }

        if (locationManager != null)
        {
            locationManager.RetryStartLocationService();
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (rootPanel == null)
        {
            return;
        }

        if (rootPanel.activeSelf != visible)
        {
            rootPanel.SetActive(visible);
        }
    }
}

