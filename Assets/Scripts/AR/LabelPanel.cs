using UnityEngine;
using TMPro;

/// <summary>
/// Per-building label panel behaviour responsible for data binding and distance-based fade.
/// Visual layout and expansion UI are configured on the prefab in later phases.
/// </summary>
public class LabelPanel : MonoBehaviour
{
    private const float DisableThresholdAlpha = 0.01f;
    private const float DefaultPanelWidth = 320f;

    /// <summary>
    /// Data backing this label panel instance.
    /// </summary>
    public BuildingRecord BuildingData { get; private set; }

    /// <summary>
    /// Indicates whether this label is currently in an expanded state.
    /// </summary>
    public bool IsExpanded { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private float fadeInNearMeters = 20f;
    [SerializeField] private float fadeInFarMeters = 50f;
    [SerializeField] private float fadeOutNearMeters = 200f;
    [SerializeField] private float fadeOutFarMeters = 300f;

    [Header("References")]
    [SerializeField] private RectTransform rootRectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI departmentText;
    [SerializeField] private TextMeshProUGUI hoursText;
    [SerializeField] private TextMeshProUGUI funFactText;
    [SerializeField] private GameObject funFactSection;

    [Header("Expand/Collapse Sizing")]
    [SerializeField] private float collapsedHeight = 120f;
    [SerializeField] private float expandedHeight = 200f;

    private int lastRoundedDistanceValue = -1;
    private bool lastDistanceDisplayedInKilometers;
    private float cachedPanelWidth = DefaultPanelWidth;
    private bool hasCachedPanelWidth;

    /// <summary>
    /// Initializes this panel with the specified building data.
    /// Called once when the panel is instantiated.
    /// </summary>
    /// <param name="data">Building record backing this label.</param>
    public void Initialize(BuildingRecord data)
    {
        BuildingData = data;
        IsExpanded = false;
        lastRoundedDistanceValue = -1;
        lastDistanceDisplayedInKilometers = false;
        CacheRootRectIfNeeded();
        CachePanelWidthIfNeeded();

        if (BuildingData != null)
        {
            if (nameText != null)
            {
                nameText.text = BuildingData.name;
            }

            if (departmentText != null)
            {
                departmentText.text = BuildingData.department;
            }

            if (hoursText != null)
            {
                hoursText.text = BuildingData.hours;
            }

            if (funFactText != null)
            {
                funFactText.text = BuildingData.funFact;
            }
        }

        if (funFactSection != null)
        {
            funFactSection.SetActive(false);
        }

        ApplyPanelHeight(collapsedHeight);
    }

    /// <summary>
    /// Updates the panel's fade and distance text based on the given distance and relative angle.
    /// </summary>
    /// <param name="distanceMeters">Distance from the user to the building in meters.</param>
    /// <param name="deltaAngle">Signed bearing delta in degrees relative to the user's heading.</param>
    public void UpdatePanel(float distanceMeters, float deltaAngle)
    {
        if (canvasGroup == null)
        {
            return;
        }

        float effectiveMax = fadeOutFarMeters;
        if (BuildingData != null && BuildingData.visibilityRadiusMeters > 0f)
        {
            effectiveMax = Mathf.Min(effectiveMax, BuildingData.visibilityRadiusMeters);
        }

        float alpha = ComputeAlpha(distanceMeters, effectiveMax);
        canvasGroup.alpha = alpha;

        bool shouldBeActive = alpha > DisableThresholdAlpha;
        if (gameObject.activeSelf != shouldBeActive)
        {
            gameObject.SetActive(shouldBeActive);
        }

        UpdateDistanceText(distanceMeters);
    }

    /// <summary>
    /// Toggles the expanded state of this panel.
    /// </summary>
    public void ToggleExpand()
    {
        IsExpanded = !IsExpanded;

        if (funFactSection != null)
        {
            funFactSection.SetActive(IsExpanded);
        }

        ApplyPanelHeight(IsExpanded ? expandedHeight : collapsedHeight);
    }

    /// <summary>
    /// Collapses this panel to its non-expanded state.
    /// </summary>
    public void Collapse()
    {
        IsExpanded = false;

        if (funFactSection != null)
        {
            funFactSection.SetActive(false);
        }

        ApplyPanelHeight(collapsedHeight);
    }

    private void CacheRootRectIfNeeded()
    {
        if (rootRectTransform != null)
        {
            return;
        }

        rootRectTransform = transform as RectTransform;
    }

    private void CachePanelWidthIfNeeded()
    {
        if (hasCachedPanelWidth)
        {
            return;
        }

        if (rootRectTransform != null)
        {
            cachedPanelWidth = rootRectTransform.sizeDelta.x;
            if (cachedPanelWidth <= 0f)
            {
                cachedPanelWidth = DefaultPanelWidth;
            }

            hasCachedPanelWidth = true;
        }
    }

    private void ApplyPanelHeight(float height)
    {
        if (rootRectTransform == null)
        {
            return;
        }

        CachePanelWidthIfNeeded();
        rootRectTransform.sizeDelta = new Vector2(cachedPanelWidth, height);
    }

    private float ComputeAlpha(float distanceMeters, float effectiveMax)
    {
        if (distanceMeters < fadeInNearMeters)
        {
            return 0f;
        }

        if (distanceMeters <= fadeInFarMeters)
        {
            return Mathf.InverseLerp(fadeInNearMeters, fadeInFarMeters, distanceMeters);
        }

        if (distanceMeters < fadeOutNearMeters)
        {
            return 1f;
        }

        float outer = Mathf.Max(fadeOutNearMeters, effectiveMax);
        if (distanceMeters >= outer)
        {
            return 0f;
        }

        return 1f - Mathf.InverseLerp(fadeOutNearMeters, outer, distanceMeters);
    }

    private void UpdateDistanceText(float distanceMeters)
    {
        if (distanceText == null)
        {
            return;
        }

        if (distanceMeters < 0f)
        {
            return;
        }

        bool useKilometers = distanceMeters >= 1000f;
        int roundedValue = useKilometers
            ? Mathf.RoundToInt(distanceMeters / 100f) // preserve one decimal in km
            : Mathf.RoundToInt(distanceMeters);

        if (roundedValue == lastRoundedDistanceValue && useKilometers == lastDistanceDisplayedInKilometers)
        {
            return;
        }

        lastRoundedDistanceValue = roundedValue;
        lastDistanceDisplayedInKilometers = useKilometers;

        if (useKilometers)
        {
            float km = distanceMeters / 1000f;
            distanceText.SetText("{0:F1} km", km);
        }
        else
        {
            distanceText.SetText("{0:F0} m", distanceMeters);
        }
    }
}

