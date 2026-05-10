using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using TheHero.Generated;

public class TheHeroFixMapAndBaseUI : EditorWindow
{
    [MenuItem("The Hero/Fix/Fix Map Labels And Base Hire UI")]
    public static void FixAll()
    {
        FixMapScene();
        FixBaseScene();
        
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("[TheHeroFix] Ready for testing");
    }

    private static void FixMapScene()
    {
        if (!System.IO.File.Exists("Assets/Scenes/Map.unity")) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            // Remove empty placeholder panels (Images with no text and no children)
            foreach (Transform child in canvas.transform)
            {
                if (child.name.Contains("Panel") || child.name.Contains("Placeholder") || child.name.Contains("HUD"))
                {
                    if (child.childCount == 0)
                    {
                        var img = child.GetComponent<Image>();
                        var txt = child.GetComponent<Text>();
                        if (img != null && (txt == null || string.IsNullOrEmpty(txt.text)))
                        {
                            Undo.DestroyObjectImmediate(child.gameObject);
                        }
                    }
                }
            }
            Debug.Log("[TheHeroMapUI] Empty placeholder panels removed");
        }

        // Fix Boss Label duplication (ensure only one Boss object name label exists)
        // Code-wise duplication is handled by THMapObjectVisuals fix.
        // Scene-wise duplication:
        var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGos)
        {
            if (go.name == "Tooltip" && go.transform.parent != null && go.transform.parent.name == "Тёмный Лорд")
            {
                Undo.DestroyObjectImmediate(go);
            }
        }
        Debug.Log("[TheHeroMapUI] Boss label duplication fixed");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void FixBaseScene()
    {
        if (!System.IO.File.Exists("Assets/Scenes/Base.unity")) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Base.unity");

        // 1. Re-build Canvas structure
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;
        
        var runtime = Object.FindAnyObjectByType<THBaseRuntime>();
        if (runtime == null)
        {
            var go = new GameObject("BaseRuntime");
            runtime = go.AddComponent<THBaseRuntime>();
        }

        // Clean old stuff we are going to replace or update
        Transform topBar = canvasGo.transform.Find("TopBar");
        Transform mainPanel = canvasGo.transform.Find("MainPanel");

        // Ensure HirePanel and ArmyPanel are correct
        Transform hirePanel = mainPanel?.Find("BuildingsPanel") ?? mainPanel?.Find("HirePanel");
        if (hirePanel != null) hirePanel.name = "HirePanel";
        
        Transform armyPanel = mainPanel?.Find("ArmyPanel");

        // 2. Redesign BuildingTemplate (Hire Row)
        GameObject template = canvasGo.transform.Find("BuildingTemplate")?.gameObject;
        if (template != null)
        {
            RedesignHireRow(template);
            runtime.buildingCardTemplate = template;
        }

        // 3. Update references
        runtime.resourcesText = topBar?.Find("ResourcesText")?.GetComponent<Text>();
        runtime.backToMapButton = topBar?.Find("BackToMapButton")?.GetComponent<Button>();
        runtime.buildingsContainer = hirePanel?.Find("BuildingsContainer") ?? hirePanel?.Find("HireListContainer");
        if (runtime.buildingsContainer != null) runtime.buildingsContainer.name = "HireListContainer";
        
        runtime.armyContainer = armyPanel?.Find("ArmyContainer") ?? armyPanel?.Find("ArmyListContainer");
        if (runtime.armyContainer != null) runtime.armyContainer.name = "ArmyListContainer";
        
        runtime.armySummaryText = armyPanel?.Find("Summary")?.GetComponent<Text>();

        // Ensure raycast targets are correct
        foreach (var img in canvasGo.GetComponentsInChildren<Image>(true))
        {
            if (img.GetComponent<Button>() == null) img.raycastTarget = false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TheHeroBase] Base hire UI fixed");
    }

    private static void RedesignHireRow(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1400, 100);
        
        // Remove old Layout
        var oldLayout = go.GetComponent<LayoutGroup>();
        if (oldLayout != null) DestroyImmediate(oldLayout);

        // Add Horizontal Layout
        var hlg = go.GetComponent<HorizontalLayoutGroup>() ?? go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(15, 15, 10, 10);
        hlg.spacing = 30;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;

        // Left Side: Icon & Name
        GameObject icon = EnsureChild(go, "UnitIcon", typeof(Image));
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
        
        GameObject nameTxt = EnsureChild(go, "NameText", typeof(Text));
        nameTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);
        var tName = nameTxt.GetComponent<Text>();
        tName.alignment = TextAnchor.MiddleLeft;
        tName.fontSize = 24;
        tName.fontStyle = FontStyle.Bold;

        // Right Side Info (grouped or just loose)
        GameObject infoGroup = EnsureChild(go, "InfoGroup", typeof(RectTransform));
        var infoHlg = infoGroup.GetComponent<HorizontalLayoutGroup>() ?? infoGroup.AddComponent<HorizontalLayoutGroup>();
        infoHlg.spacing = 20;
        infoHlg.childControlWidth = true;
        infoHlg.childForceExpandWidth = false;
        infoGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 80);

        GameObject availTxt = EnsureChild(infoGroup, "CountAvailableText", typeof(Text));
        availTxt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        availTxt.GetComponent<Text>().fontSize = 22;
        availTxt.GetComponent<Text>().color = Color.yellow;

        GameObject costTxt = EnsureChild(infoGroup, "CostText", typeof(Text));
        costTxt.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        costTxt.GetComponent<Text>().fontSize = 20;

        GameObject totalTxt = EnsureChild(infoGroup, "TotalCostText", typeof(Text));
        totalTxt.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        totalTxt.GetComponent<Text>().fontSize = 20;
        totalTxt.GetComponent<Text>().fontStyle = FontStyle.Bold;
        totalTxt.GetComponent<Text>().color = new Color(1, 0.8f, 0);

        // Buttons
        GameObject btnGroup = EnsureChild(go, "Buttons", typeof(RectTransform));
        var btnHlg = btnGroup.GetComponent<HorizontalLayoutGroup>() ?? btnGroup.AddComponent<HorizontalLayoutGroup>();
        btnHlg.spacing = 15;
        btnGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(450, 60);

        EnsureButton(btnGroup, "RecruitOneButton", "Нанять 1");
        EnsureButton(btnGroup, "RecruitAllButton", "Нанять всех");
        EnsureButton(btnGroup, "UpgradeButton", "Улучшить");

        // Clean up old loose texts
        string[] toRemove = { "LevelText", "UnitText", "AvailableText", "RecruitCostText", "UpgradeCostText" };
        foreach (var name in toRemove)
        {
            var child = go.transform.Find(name);
            if (child != null && child.parent == go.transform) child.gameObject.SetActive(false);
        }
    }

    private static GameObject EnsureChild(GameObject parent, string name, System.Type type)
    {
        var child = parent.transform.Find(name);
        if (child != null) return child.gameObject;
        var go = new GameObject(name, typeof(RectTransform), type);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void EnsureButton(GameObject parent, string name, string label)
    {
        var btnGo = EnsureChild(parent, name, typeof(Image));
        if (btnGo.GetComponent<Button>() == null) btnGo.AddComponent<Button>();
        btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 40);
        btnGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        
        var txtGo = EnsureChild(btnGo, "Text", typeof(Text));
        var t = txtGo.GetComponent<Text>();
        t.text = label;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 16;
        t.color = Color.white;
        Stretch(txtGo);
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
