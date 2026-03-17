using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central controller responsible for creating, positioning, and managing all building label panels
/// in the AR view. Labels are camera-relative overlays updated every frame based on GPS and compass.
/// </summary>
public class ARLabelController : MonoBehaviour
{
    private const float OffScreenNormalizedXThreshold = 1.5f;
    private const float OverlapPixelThreshold = 50f;

    [Header("References")]
    [SerializeField] private GameObject labelPanelPrefab;

    [Header("Placement Settings")]
    [SerializeField] private float labelDistanceFromCamera = 5f;
    [SerializeField] private float verticalOffset = -0.5f;
    [SerializeField] private float overlapStaggerY = 0.3f;

    [Header("Dependencies")]
    [SerializeField] private UserLocationManager locationManager;
    [SerializeField] private BuildingDataManager buildingDataManager;

    /// <summary>
    /// Active label panels keyed by buildingId.
    /// </summary>
    private readonly Dictionary<string, LabelPanel> activePanels = new Dictionary<string, LabelPanel>();

    /// <summary>
    /// Cached reference to the AR camera.
    /// </summary>
    private Camera arCamera;

    private readonly List<LabelPanel> tempVisiblePanels = new List<LabelPanel>();
    private readonly List<Vector3> tempVisibleScreenPositions = new List<Vector3>();

    private void Start()
    {
        arCamera = Camera.main;

        if (locationManager == null)
        {
            locationManager = UserLocationManager.Instance;
        }

        if (buildingDataManager == null)
        {
            buildingDataManager = BuildingDataManager.Instance;
        }

        StartCoroutine(WaitAndInitialize());
    }

    /// <summary>
    /// Waits for GPS fix and building data to be loaded before initializing label panels.
    /// </summary>
    private IEnumerator WaitAndInitialize()
    {
        while ((locationManager == null || !locationManager.HasGPSFix) ||
               (buildingDataManager == null || !buildingDataManager.IsLoaded))
        {
            yield return null;
        }

        InitializePanels();
    }

    /// <summary>
    /// Instantiates label panels for each building and stores them in the activePanels dictionary.
    /// </summary>
    private void InitializePanels()
    {
        if (labelPanelPrefab == null)
        {
            Debug.LogError("[ARLabelController] Label panel prefab is not assigned.");
            return;
        }

        if (buildingDataManager == null || buildingDataManager.Buildings == null)
        {
            Debug.LogError("[ARLabelController] BuildingDataManager or Buildings list is null.");
            return;
        }

        for (int i = 0; i < buildingDataManager.Buildings.Count; i++)
        {
            BuildingRecord building = buildingDataManager.Buildings[i];
            if (building == null || string.IsNullOrEmpty(building.buildingId))
            {
                continue;
            }

            if (activePanels.ContainsKey(building.buildingId))
            {
                continue;
            }

            GameObject panelObject = Instantiate(labelPanelPrefab, transform);
            LabelPanel panel = panelObject.GetComponent<LabelPanel>();
            if (panel == null)
            {
                Debug.LogError("[ARLabelController] LabelPanel component missing on prefab instance.");
                Destroy(panelObject);
                continue;
            }

            panel.Initialize(building);
            panelObject.SetActive(false);
            activePanels.Add(building.buildingId, panel);
        }
    }

    private void Update()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null)
            {
                return;
            }
        }

        if (locationManager == null || !locationManager.HasGPSFix)
        {
            return;
        }

        if (buildingDataManager == null || !buildingDataManager.IsLoaded)
        {
            return;
        }

        double userLat = locationManager.UserLatitude;
        double userLon = locationManager.UserLongitude;
        float heading = locationManager.SmoothedHeading;

        tempVisiblePanels.Clear();
        tempVisibleScreenPositions.Clear();

        foreach (KeyValuePair<string, LabelPanel> kvp in activePanels)
        {
            LabelPanel panel = kvp.Value;
            if (panel == null || panel.BuildingData == null)
            {
                continue;
            }

            BuildingRecord building = panel.BuildingData;

            float distance = GeoUtils.HaversineDistance(userLat, userLon, building.latitude, building.longitude);
            float buildingBearing = GeoUtils.BearingTo(userLat, userLon, building.latitude, building.longitude);
            float currentHeading = heading;

            float bearingDelta = buildingBearing - currentHeading;
            // Normalize to -180..180
            while (bearingDelta > 180f)
            {
                bearingDelta -= 360f;
            }

            while (bearingDelta < -180f)
            {
                bearingDelta += 360f;
            }

            // Cull if outside FOV
            if (Mathf.Abs(bearingDelta) > 90f)
            {
                if (panel.gameObject.activeSelf)
                {
                    panel.gameObject.SetActive(false);
                }

                continue;
            }

            panel.UpdatePanel(distance, bearingDelta);

            if (!panel.gameObject.activeSelf)
            {
                continue;
            }

            Vector3 cameraPosition = arCamera.transform.position;

            // Bearing delta (already normalized to -180..180) → world-space direction on XZ plane
            float deltaRad = bearingDelta * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(deltaRad), 0f, -Mathf.Cos(deltaRad));

            Vector3 pos = cameraPosition + direction * labelDistanceFromCamera;
            pos += Vector3.up * verticalOffset;

            Transform panelTransform = panel.transform;
            panelTransform.position = pos;
            panelTransform.rotation = Quaternion.LookRotation(panelTransform.position - cameraPosition);

            tempVisiblePanels.Add(panel);
            tempVisibleScreenPositions.Add(arCamera.WorldToScreenPoint(panelTransform.position));
        }

        ApplyOverlapStagger();
        HandleTap();
    }

    private void ApplyOverlapStagger()
    {
        int count = tempVisiblePanels.Count;
        if (count <= 1)
        {
            return;
        }

        List<int> indices = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            indices.Add(i);
        }

        indices.Sort((a, b) =>
        {
            float ax = tempVisibleScreenPositions[a].x;
            float bx = tempVisibleScreenPositions[b].x;
            return ax.CompareTo(bx);
        });

        for (int i = 0; i < count - 1; i++)
        {
            int indexA = indices[i];
            int indexB = indices[i + 1];

            Vector3 screenA = tempVisibleScreenPositions[indexA];
            Vector3 screenB = tempVisibleScreenPositions[indexB];

            float dx = Mathf.Abs(screenA.x - screenB.x);
            float dy = Mathf.Abs(screenA.y - screenB.y);

            if (dx <= OverlapPixelThreshold && dy <= OverlapPixelThreshold)
            {
                LabelPanel panelB = tempVisiblePanels[indexB];
                if (panelB != null)
                {
                    panelB.transform.position += arCamera.transform.up * overlapStaggerY;
                }
            }
        }
    }

    private void HandleTap()
    {
        bool tapDetected = false;
        Vector2 tapPosition = Vector2.zero;

#if UNITY_EDITOR
        if (Input.GetMouseButtonUp(0))
        {
            tapDetected = true;
            tapPosition = Input.mousePosition;
        }
#else
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                tapDetected = true;
                tapPosition = touch.position;
            }
        }
#endif

        if (!tapDetected)
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(tapPosition);
        RaycastHit hit;
        LabelPanel tappedPanel = null;

        if (Physics.Raycast(ray, out hit))
        {
            tappedPanel = hit.collider.GetComponentInParent<LabelPanel>();
        }

        if (tappedPanel == null)
        {
            bool anyExpanded = false;
            foreach (LabelPanel panel in activePanels.Values)
            {
                if (panel != null && panel.IsExpanded)
                {
                    anyExpanded = true;
                    panel.Collapse();
                }
            }

            if (!anyExpanded)
            {
                return;
            }

            return;
        }

        foreach (LabelPanel panel in activePanels.Values)
        {
            if (panel == null)
            {
                continue;
            }

            if (panel == tappedPanel)
            {
                panel.ToggleExpand();
            }
            else
            {
                panel.Collapse();
            }
        }
    }
}

