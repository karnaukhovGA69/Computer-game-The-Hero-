using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Linq;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroCriticalFixer
    {
        [MenuItem("The Hero/Fix/Critical Fix Map Movement And Base UI")]
        public static void FixAll()
        {
            Debug.Log("<b>[TheHeroCriticalFix] Starting Critical Fix...</b>");

            FixMapScene();
            FixBaseScene();
            
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[TheHeroCriticalFix] All fixes applied successfully!</color>");
        }

        private static void FixMapScene()
        {
            if (!File.Exists("Assets/Scenes/Map.unity")) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

            // 1. MapRoot Setup
            GameObject mapRoot = GameObject.Find("MapRoot");
            if (mapRoot == null)
            {
                mapRoot = new GameObject("MapRoot");
                // Move everything to MapRoot (except Camera and EventSystem and Canvas)
                var allRoots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var go in allRoots)
                {
                    if (go.name != "Main Camera" && go.name != "EventSystem" && go.name != "Canvas" && go.name != "TH_Bootstrap" && go.name != "MapRoot" && go.name != "MapController")
                    {
                        go.transform.SetParent(mapRoot.transform);
                    }
                }
            }

            // 2. Center Map
            var centerFix = mapRoot.GetComponent<THCenterMapAndCamera>();
            if (centerFix == null) centerFix = mapRoot.AddComponent<THCenterMapAndCamera>();
            centerFix.CenterNow();

            // 3. Hero Fix
            var hero = GameObject.Find("Hero");
            if (hero == null)
            {
                hero = new GameObject("Hero");
                hero.transform.SetParent(mapRoot.transform);
                Debug.Log("[TheHeroCriticalFix] Created Hero.");
            }
            
            var sr = hero.GetComponent<SpriteRenderer>();
            if (sr == null) sr = hero.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;

            var mover = hero.GetComponent<THGuaranteedHeroMovement>();
            if (mover == null) mover = hero.AddComponent<THGuaranteedHeroMovement>();

            // Disable old mover if exists
            var oldMover = hero.GetComponent<THHeroMover>();
            if (oldMover != null) oldMover.enabled = false;

            // 4. Tile Colliders and THTile
            var tiles = mapRoot.GetComponentsInChildren<Transform>()
                .Where(t => t.name.StartsWith("Tile_"))
                .ToList();

            foreach (var t in tiles)
            {
                if (t.GetComponent<Collider2D>() == null) t.gameObject.AddComponent<BoxCollider2D>();
                if (t.GetComponent<THTile>() == null)
                {
                    var tileComp = t.gameObject.AddComponent<THTile>();
                    string type = "grass";
                    if (t.name.ToLower().Contains("mountain")) type = "mountain";
                    else if (t.name.ToLower().Contains("water")) type = "water";
                    else if (t.name.ToLower().Contains("forest")) type = "forest";
                    else if (t.name.ToLower().Contains("road")) type = "road";
                    
                    // Guess x,y from name
                    string[] parts = t.name.Split('_');
                    int x = 0, y = 0;
                    if (parts.Length >= 3) { int.TryParse(parts[1], out x); int.TryParse(parts[2], out y); }
                    tileComp.Setup(x, y, type);
                }
            }

            // 5. UI Blockers
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var images = canvas.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.GetComponent<Button>() == null && img.color.a < 0.05f)
                    {
                        img.raycastTarget = false;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[TheHeroCriticalFix] Map scene fixed.");
        }

        private static void FixBaseScene()
        {
            if (!File.Exists("Assets/Scenes/Base.unity")) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Base.unity");

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Back button
            var backBtnGo = GameObject.Find("BackToMapButton");
            if (backBtnGo == null)
            {
                backBtnGo = new GameObject("BackToMapButton", typeof(RectTransform), typeof(Image), typeof(Button));
                backBtnGo.transform.SetParent(canvas.transform, false);
                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(backBtnGo.transform, false);
            }

            if (canvas.GetComponent<THBaseBackButtonFix>() == null)
                canvas.gameObject.AddComponent<THBaseBackButtonFix>();

            // 2. Compact Layout
            var buildingsPanel = canvas.transform.Find("BuildingsPanel") ?? canvas.transform.Find("RecruitPanel") ?? canvas.transform;
            var layoutFix = buildingsPanel.GetComponent<THBaseCompactUILayout>();
            if (layoutFix == null) layoutFix = buildingsPanel.gameObject.AddComponent<THBaseCompactUILayout>();
            layoutFix.ApplyCompactLayout();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[TheHeroCriticalFix] Base scene fixed.");
        }
    }
}
