using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroStrictGridMovementFixer
    {
        [MenuItem("The Hero/Fix/Fix Strict Grid Movement")]
        public static void FixMovement()
        {
            if (!File.Exists("Assets/Scenes/Map.unity"))
            {
                Debug.LogError("[TheHeroGridFix] Map scene missing!");
                return;
            }

            EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

            // 1. Scan and Fix Tiles
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int tileCount = 0;
            foreach (var go in allObjects)
            {
                if (go.name.StartsWith("Tile_"))
                {
                    FixTile(go);
                    tileCount++;
                }
            }
            Debug.Log($"[TheHeroGridFix] {tileCount} tiles scanned and fixed.");

            // 2. Setup Hero
            var hero = GameObject.Find("Hero");
            if (hero == null)
            {
                hero = new GameObject("Hero");
                Debug.Log("[TheHeroGridFix] Hero missing. Created new Hero.");
            }

            // Disable old movers
            var movers = hero.GetComponents<MonoBehaviour>();
            foreach (var m in movers)
            {
                string typeName = m.GetType().Name;
                if (typeName == "THGuaranteedHeroMovement" || typeName == "THReliableHeroMovement" || typeName == "HeroMover")
                {
                    m.enabled = false;
                    Debug.Log($"[TheHeroGridFix] Disabled old mover: {typeName}");
                }
            }

            var gridMover = hero.GetComponent<THStrictGridHeroMovement>();
            if (gridMover == null) gridMover = hero.AddComponent<THStrictGridHeroMovement>();
            gridMover.enabled = true;

            var sr = hero.GetComponent<SpriteRenderer>();
            if (sr == null) sr = hero.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            if (sr.sprite == null) sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Units/hero.png");

            // Snap Hero to nearest walkable tile
            var tiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            var startTile = tiles.Where(t => t.walkable).OrderBy(t => Vector2.Distance(t.transform.position, hero.transform.position)).FirstOrDefault();
            if (startTile != null)
            {
                hero.transform.position = new Vector3(startTile.transform.position.x, startTile.transform.position.y, 0);
                gridMover.currentX = startTile.x;
                gridMover.currentY = startTile.y;
            }

            // 3. Center Camera
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                if (tiles.Length > 0)
                {
                    float minX = tiles.Min(t => t.transform.position.x);
                    float maxX = tiles.Max(t => t.transform.position.x);
                    float minY = tiles.Min(t => t.transform.position.y);
                    float maxY = tiles.Max(t => t.transform.position.y);
                    cam.transform.position = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, -10);
                    cam.orthographicSize = 7.5f;
                    cam.backgroundColor = new Color(0.1f, 0.15f, 0.1f);
                }
            }

            // 4. UI Raycast Blockers
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
            Debug.Log("<color=green>[TheHeroGridFix] Ready for testing</color>");
        }

        private static void FixTile(GameObject go)
        {
            var thTile = go.GetComponent<THTile>();
            if (thTile == null) thTile = go.AddComponent<THTile>();

            var coll = go.GetComponent<BoxCollider2D>();
            if (coll == null) coll = go.AddComponent<BoxCollider2D>();
            coll.isTrigger = false;

            // Guess coordinates
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
