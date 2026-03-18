using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class BuildLabelPanelPrefab
{
    public static void Execute()
    {
        string prefabPath = "Assets/Prefabs/Canvas.prefab";
        string newPrefabPath = "Assets/Prefabs/LabelPanel.prefab";

        // Rename prefab if still called Canvas
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath) == null)
        {
            AssetDatabase.RenameAsset(prefabPath, "LabelPanel");
            AssetDatabase.SaveAssets();
            prefabPath = newPrefabPath;
        }
        else if (AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath) != null)
        {
            prefabPath = newPrefabPath;
        }

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError("LabelPanel prefab not found at: " + prefabPath);
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            // --- Clear existing children ---
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(root.transform.GetChild(i).gameObject);

            // --- Root Canvas setup ---
            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null) canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rootRT = root.GetComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(320, 120);
            root.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);

            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.AddComponent<CanvasGroup>();

            // --- PanelRoot ---
            GameObject panelRoot = CreatePanel(root.transform, "PanelRoot", HexColor("1A1A2E"));
            SetStretch(panelRoot.GetComponent<RectTransform>());

            // --- HeaderBar ---
            GameObject headerBar = CreatePanel(panelRoot.transform, "HeaderBar", HexColor("CEB888"));
            RectTransform headerRT = headerBar.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.anchoredPosition = Vector2.zero;
            headerRT.sizeDelta = new Vector2(0, 40);

            // BuildingNameText
            GameObject nameGO = CreateTMP(headerBar.transform, "BuildingNameText", Color.white, 22, TextAlignmentOptions.Left);
            RectTransform nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0);
            nameRT.anchorMax = new Vector2(0.65f, 1);
            nameRT.offsetMin = new Vector2(8, 0);
            nameRT.offsetMax = new Vector2(0, 0);
            var nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.text = "Building Name";

            // DistanceText
            GameObject distGO = CreateTMP(headerBar.transform, "DistanceText", Color.white, 16, TextAlignmentOptions.Right);
            RectTransform distRT = distGO.GetComponent<RectTransform>();
            distRT.anchorMin = new Vector2(0.65f, 0);
            distRT.anchorMax = new Vector2(1, 1);
            distRT.offsetMin = new Vector2(0, 0);
            distRT.offsetMax = new Vector2(-8, 0);
            distGO.GetComponent<TextMeshProUGUI>().text = "0m";

            // --- BodyPanel ---
            GameObject bodyPanel = CreatePanel(panelRoot.transform, "BodyPanel", HexColor("1A1A2E"));
            RectTransform bodyRT = bodyPanel.GetComponent<RectTransform>();
            bodyRT.anchorMin = new Vector2(0, 0);
            bodyRT.anchorMax = new Vector2(1, 1);
            bodyRT.offsetMin = new Vector2(0, 0);
            bodyRT.offsetMax = new Vector2(0, -40);

            // DepartmentText
            GameObject deptGO = CreateTMP(bodyPanel.transform, "DepartmentText", HexColor("CCCCCC"), 14, TextAlignmentOptions.Left);
            RectTransform deptRT = deptGO.GetComponent<RectTransform>();
            deptRT.anchorMin = new Vector2(0, 1);
            deptRT.anchorMax = new Vector2(1, 1);
            deptRT.pivot = new Vector2(0.5f, 1f);
            deptRT.anchoredPosition = new Vector2(0, -4);
            deptRT.sizeDelta = new Vector2(-16, 20);
            deptGO.GetComponent<TextMeshProUGUI>().text = "Dept: ...";

            // HoursText
            GameObject hoursGO = CreateTMP(bodyPanel.transform, "HoursText", HexColor("CCCCCC"), 14, TextAlignmentOptions.Left);
            RectTransform hoursRT = hoursGO.GetComponent<RectTransform>();
            hoursRT.anchorMin = new Vector2(0, 1);
            hoursRT.anchorMax = new Vector2(1, 1);
            hoursRT.pivot = new Vector2(0.5f, 1f);
            hoursRT.anchoredPosition = new Vector2(0, -26);
            hoursRT.sizeDelta = new Vector2(-16, 20);
            hoursGO.GetComponent<TextMeshProUGUI>().text = "Hours: ...";

            // Separator
            GameObject sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(bodyPanel.transform, false);
            sep.GetComponent<Image>().color = HexColor("CEB888");
            RectTransform sepRT = sep.GetComponent<RectTransform>();
            sepRT.anchorMin = new Vector2(0, 1);
            sepRT.anchorMax = new Vector2(1, 1);
            sepRT.pivot = new Vector2(0.5f, 1f);
            sepRT.anchoredPosition = new Vector2(0, -48);
            sepRT.sizeDelta = new Vector2(-16, 2);

            // FunFactSection
            GameObject funFactSection = new GameObject("FunFactSection", typeof(RectTransform));
            funFactSection.transform.SetParent(bodyPanel.transform, false);
            RectTransform ffsRT = funFactSection.GetComponent<RectTransform>();
            ffsRT.anchorMin = new Vector2(0, 0);
            ffsRT.anchorMax = new Vector2(1, 1);
            ffsRT.offsetMin = new Vector2(8, 4);
            ffsRT.offsetMax = new Vector2(-8, -52);
            funFactSection.SetActive(false);

            // FunFactText
            GameObject funFactGO = CreateTMP(funFactSection.transform, "FunFactText", Color.white, 13, TextAlignmentOptions.Left);
            SetStretch(funFactGO.GetComponent<RectTransform>());
            var funTMP = funFactGO.GetComponent<TextMeshProUGUI>();
            funTMP.enableWordWrapping = true;
            funTMP.text = "Fun fact goes here.";

            // --- Wire LabelPanel.cs references ---
            var labelPanel = root.GetComponent<MonoBehaviour>();
            // Find LabelPanel component by type name since we can't reference it directly in editor script
            foreach (var mb in root.GetComponents<MonoBehaviour>())
            {
                if (mb.GetType().Name == "LabelPanel")
                {
                    var t = mb.GetType();
                    SetField(mb, t, "rootRectTransform", rootRT);
                    SetField(mb, t, "canvasGroup", cg);
                    SetField(mb, t, "nameText", nameGO.GetComponent<TextMeshProUGUI>());
                    SetField(mb, t, "distanceText", distGO.GetComponent<TextMeshProUGUI>());
                    SetField(mb, t, "departmentText", deptGO.GetComponent<TextMeshProUGUI>());
                    SetField(mb, t, "hoursText", hoursGO.GetComponent<TextMeshProUGUI>());
                    SetField(mb, t, "funFactText", funFactGO.GetComponent<TextMeshProUGUI>());
                    SetField(mb, t, "funFactSection", funFactSection);
                    SetField(mb, t, "collapsedHeight", 120f);
                    SetField(mb, t, "expandedHeight", 200f);
                    EditorUtility.SetDirty(mb);
                    Debug.Log("[BuildLabelPanel] All LabelPanel.cs references wired.");
                    break;
                }
            }

            Debug.Log("[BuildLabelPanel] LabelPanel prefab rebuilt successfully.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuildLabelPanel] Done. Prefab saved.");
    }

    // --- Helpers ---

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateTMP(Transform parent, string name, Color color, float size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.color = color;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        return go;
    }

    static void SetStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    static void SetField(MonoBehaviour mb, System.Type t, string fieldName, object value)
    {
        var field = t.GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(mb, value);
        else
            Debug.LogWarning($"[BuildLabelPanel] Field '{fieldName}' not found on LabelPanel.");
    }
}
