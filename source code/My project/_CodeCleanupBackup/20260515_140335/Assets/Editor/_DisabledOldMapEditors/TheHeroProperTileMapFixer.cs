using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroProperTileMapFixer
    {
        [MenuItem("The Hero/Fix/Build Proper Tile Map Movement")]
        public static void FixAll()
        {
            Debug.Log("<b>[TheHeroFix] Building Proper Tile Map Movement...</b>");

            if (File.Exists("Assets/Scenes/Map.unity"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");
            }
            else
            {
                Debug.LogError("[TheHeroFix] Map scene not found!");
                return;
            }

            // 1. Setup MapRoot and Tiles
            var mapRoot = GameObject.Find("MapRoot");
            if (mapRoot == null) mapRoot = new GameObject("MapRoot");

            var tilesContainer = GameObject.Find("Tiles");
            if (tilesContainer == null)
            {
                tilesContainer = new GameObject("Tiles");
                tilesContainer.transform.SetParent(mapRoot.transform);
            }

            // 2. Scan and Setup THTile and Colliders
            var allTiles = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int tileCount = 0;
            foreach (var go in allTiles)
            {
                if (go.name.StartsWith("Tile_"))
                {
                    SetupTile(go);
                    tileCount++;
                }
            }
            Debug.Log($"[TheHeroFix] {tileCount} tiles processed.");

            // 3. Setup MapBounds
            var bounds = mapRoot.GetComponent<THMapBounds>();
            if (bounds == null) bounds = mapRoot.AddComponent<THMapBounds>();
            bounds.CalculateBounds();

            // 4. Setup Camera
            var cam = Camera.main;
            if (cam != null)
            {
                if (cam.GetComponent<THCameraClamp>() == null) cam.gameObject.AddComponent<THCameraClamp>();
                
                // Center camera on map
                cam.transform.position = new Vector3((bounds.minX + bounds.maxX) / 2f, (bounds.minY + bounds.maxY) / 2f, -10);
                cam.orthographic = true;
                cam.orthographicSize = 7.5f;
            }

            // 5. Setup Hero
            var hero = GameObject.Find("Hero");
            if (hero != null)
            {
                // Disable other movement scripts
                var movers = hero.GetComponents<MonoBehaviour>();
                foreach (var m in movers)
                {
                    string typeName = m.GetType().Name;
                    if (typeName != "THStrictGridHeroMovement" && (typeName.Contains("Mover") || typeName.Contains("Movement")))
                    {
                        m.enabled = false;
                        Debug.Log($"[TheHeroFix] Disabled old mover: {typeName}");
                    }
                }

                var strictMover = hero.GetComponent<THStrictGridHeroMovement>();
                if (strictMover == null) strictMover = hero.AddComponent<THStrictGridHeroMovement>();
                strictMover.enabled = true;
                strictMover.InitializeGrid();

                var sr = hero.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 50;
                }
                
                hero.transform.position = new Vector3(Mathf.RoundToInt(hero.transform.position.x), Mathf.RoundToInt(hero.transform.position.y), 0);
            }

            // 6. UI Raycast check (already handled by previous fixers but good to ensure)
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var images = canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in images)
                {
                    if (img.GetComponent<UnityEngine.UI.Button>() == null && img.color.a < 0.1f)
                    {
                        img.raycastTarget = false;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            
            AssetDatabase.SaveAssets();
            
            // Open MainMenu
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            
            Debug.Log("<color=green>[TheHeroFix] Proper Tile Map Movement built successfully!</color>");
        }

        private static void SetupTile(GameObject go)
        {
            var thTile = go.GetComponent<THTile>();
            if (thTile == null) thTile = go.AddComponent<THTile>();

            if (go.GetComponent<BoxCollider2D>() == null)
            {
                go.AddComponent<BoxCollider2D>();
            }

            // Extract x,y from name Tile_x_y
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
        }
    }
}
