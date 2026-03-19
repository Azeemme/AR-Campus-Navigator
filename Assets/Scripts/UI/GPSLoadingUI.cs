using UnityEngine;
using TMPro;

/// <summary>
/// Displays a simple overlay while the app is acquiring the first GPS fix.
/// </summary>
public class GPSLoadingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Timing")]
    [SerializeField] private float extendedMessageDelaySeconds = 15f;

    private UserLocationManager locationManager;
    private float visibleStartTime;
    private bool isVisible;
    private bool showingExtendedMessage;

    private void Start()
    {
        locationManager = UserLocationManager.Instance;

        if (rootPanel == null)
        {
            rootPanel = gameObject;
        }

        SetVisible(false, true);
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

        // In simulation mode, HasGPSFix is set immediately; skip loading UI.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (IsSimulatingGPS())
        {
            SetVisible(false, false);
            return;
        }
#endif

        bool shouldShow = !locationManager.HasGPSFix
                          && !locationManager.GPSPermissionDenied
                          && locationManager.IsGPSEnabled;

        if (shouldShow)
        {
            if (!isVisible)
            {
                SetVisible(true, true);
            }
            else if (!showingExtendedMessage)
            {
                float elapsed = Time.unscaledTime - visibleStartTime;
                if (elapsed >= extendedMessageDelaySeconds)
                {
                    showingExtendedMessage = true;
                    if (statusText != null)
                    {
                        statusText.text = "Taking longer than expected. Make sure you are outdoors with a clear sky.";
                    }
                }
            }
        }
        else if (isVisible)
        {
            SetVisible(false, false);
        }
    }

    private void SetVisible(bool visible, bool resetTimer)
    {
        isVisible = visible;

        if (rootPanel != null && rootPanel.activeSelf != visible)
        {
            rootPanel.SetActive(visible);
        }

        if (visible && resetTimer)
        {
            visibleStartTime = Time.unscaledTime;
            showingExtendedMessage = false;
            if (statusText != null)
            {
                statusText.text = "Acquiring GPS signal...";
            }
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool IsSimulatingGPS()
    {
        // Reflective access to avoid exposing simulateGPS publicly.
        var type = typeof(UserLocationManager);
        var field = type.GetField("simulateGPS",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null || locationManager == null)
        {
            return false;
        }

        object value = field.GetValue(locationManager);
        return value is bool b && b;
    }
#endif
}

