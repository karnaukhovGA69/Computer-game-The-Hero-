using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroClickMovementDebugger
    {
        [MenuItem("The Hero/Fix/Debug And Fix Hero Click Movement")]
        public static void FixAll()
        {
            Debug.Log("<b>[TheHeroClickDebug] Starting Debug & Fix...</b>");

            if (File.Exists("Assets/Scenes/Map.unity"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");
            }
            else
            {
                Debug.LogError("[TheHeroClickDebug] Map scene missing!");
                return;
            }

            // 1. Scan and Fix Tiles
            var allTiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            if (allTiles.Length == 0)
            {
                // Try finding by name prefix
                var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
                foreach(var go in allGos)
                {
                    if (go.name.StartsWith("Tile_"))
                    {
                        SetupTile(go);
                    }
                }
                allTiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            }
            else
            {
                foreach(var tile in allTiles) SetupTile(tile.gameObject);
            }
            Debug.Log($"[TheHeroClickDebug] Tiles prepared: {allTiles.Length}");

            // 2. Setup MapGridInput
            var mapRoot = GameObject.Find("MapRoot") ?? GameObject.Find("MapController") ?? GameObject.Find("Tiles")?.transform.parent?.gameObject;
            if (mapRoot == null) mapRoot = new GameObject("MapRoot");
            
            if (mapRoot.GetComponent<THMapGridInput>() == null) mapRoot.AddComponent<THMapGridInput>();
            if (mapRoot.GetComponent<THClickDebugPanel>() == null) mapRoot.AddComponent<THClickDebugPanel>();
            Debug.Log("[TheHeroClickDebug] MapGridInput and DebugPanel installed");

            // 3. Setup Hero
            var hero = GameObject.Find("Hero");
            if (hero != null)
            {
                // Disable direct movement scripts
                var behaviours = hero.GetComponents<MonoBehaviour>();
                foreach (var b in behaviours)
                {
                    string n = b.GetType().Name;
                    if (n != "THStrictGridHeroMovement" && (n.Contains("Mover") || n.Contains("Movement")))
                    {
                        b.enabled = false;
                        Debug.Log($"[TheHeroClickDebug] Disabled old mover: {n}");
                    }
                }

                var strictMover = hero.GetComponent<THStrictGridHeroMovement>();
                if (strictMover == null) strictMover = hero.AddComponent<THStrictGridHeroMovement>();
                strictMover.enabled = true;
                strictMover.keyboardDebugMovement = true;
                
                var sr = hero.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 50;
                    sr.enabled = true;
                }
                hero.transform.position = new Vector3(hero.transform.position.x, hero.transform.position.y, 0);
                Debug.Log("[TheHeroClickDebug] StrictGridMovement installed and Hero visibility fixed");
            }

            // 4. UI Raycast Fix
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var images = canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images)
                {
                    // If it doesn't have an interactive component and is transparent/panel, disable raycast
                    bool isInteractive = img.GetComponent<UnityEngine.UI.Selectable>() != null;
                    if (!isInteractive && (img.color.a < 0.1f || img.name.Contains("Background") || img.name.Contains("Panel")))
                    {
                        img.raycastTarget = false;
                    }
                }
                Debug.Log("[TheHeroClickDebug] UI blockers fixed");
            }

            // 5. Fix Bootstrap
            var bootstrap = GameObject.Find("TH_Bootstrap") ?? GameObject.Find("Bootstrap");
            if (bootstrap != null)
            {
                if (bootstrap.GetComponent<THBootstrap>() == null)
                {
                    bootstrap.AddComponent<THBootstrap>();
                    Debug.Log("[TheHeroClickDebug] Restored THBootstrap script");
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            
            AssetDatabase.SaveAssets();
            
            // Open MainMenu
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            
            Debug.Log("<color=green>[TheHeroClickDebug] Ready for testing!</color>");
        }

        private static void SetupTile(GameObject go)
        {
            var thTile = go.GetComponent<THTile>();
            if (thTile == null) thTile = go.AddComponent<THTile>();

            if (go.GetComponent<BoxCollider2D>() == null)
            {
                go.AddComponent<BoxCollider2D>();
            }

            string[] parts = go.name.Split('_');
            int x = 0, y = 0;
            if (parts.Length >= 3)
            {
                int.TryParse(parts[1], out x);
                int.TryParse(parts[2], out y);
            }
            else
            {
                x = Mathf.RoundToInt(go.transform.position.x);
                y = Mathf.RoundToInt(go.transform.position.y);
            }

            string type = "grass";
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                string sName = sr.sprite.name.ToLower();
                if (sName.Contains("water")) type = "water";
                else if (sName.Contains("mountain")) type = "mountain";
                else if (sName.Contains("forest")) type = "forest";
                else if (sName.Contains("road")) type = "road";
            }
            
            thTile.Setup(x, y, type);
            go.layer = LayerMask.NameToLayer("Default");
        }
    }
}
