using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroMovementAndBaseExitFixer
    {
        [MenuItem("The Hero/Fix/Fix Movement And Base Exit")]
        public static void FixAll()
        {
            FixMapScene();
            FixBaseScene();
            
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();
            
            Debug.Log("[TheHeroFix] All fixes applied.");
        }

        private static void FixMapScene()
        {
            if (!File.Exists("Assets/Scenes/Map.unity")) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

            // 1. Hero Setup
            var hero = GameObject.Find("Hero");
            if (hero == null)
            {
                hero = new GameObject("Hero");
                Debug.Log("[TheHeroFix] Created Hero object.");
            }

            var sr = hero.GetComponent<SpriteRenderer>();
            if (sr == null) sr = hero.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;

            if (hero.GetComponent<Collider2D>() == null)
            {
                hero.AddComponent<BoxCollider2D>().isTrigger = true;
            }

            // 2. Reliable Movement
            var mover = hero.GetComponent<THReliableHeroMovement>();
            if (mover == null) mover = hero.AddComponent<THReliableHeroMovement>();
            
            var oldMover = hero.GetComponent<THHeroMover>();
            if (oldMover != null) oldMover.enabled = false;

            // 3. Tile Setup
            var tiles = GameObject.Find("Tiles");
            if (tiles != null)
            {
                foreach (Transform t in tiles.transform)
                {
                    SetupTile(t);
                }
            }
            else
            {
                // Try to find tiles in root or elsewhere
                var allTiles = Object.FindObjectsByType<THMapTile>(FindObjectsInactive.Include);
                foreach (var tile in allTiles)
                {
                    SetupTile(tile.transform);
                }
            }

            // 4. UI Fix
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var images = canvas.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    // Disable raycast target for non-button images that might block
                    if (img.GetComponent<Button>() == null && img.name.Contains("Panel") && img.color.a < 0.1f)
                    {
                        img.raycastTarget = false;
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[TheHeroFix] Map scene fixed.");
        }

        private static void SetupTile(Transform t)
        {
            var thTile = t.GetComponent<THTile>();
            if (thTile == null) thTile = t.gameObject.AddComponent<THTile>();

            if (t.GetComponent<Collider2D>() == null)
            {
                t.gameObject.AddComponent<BoxCollider2D>();
            }

            // Try to parse x, y from name Tile_x_y
            string[] parts = t.name.Split('_');
            int x = 0, y = 0;
            if (parts.Length >= 3)
            {
                int.TryParse(parts[1], out x);
                int.TryParse(parts[2], out y);
            }
            else
            {
                x = Mathf.RoundToInt(t.position.x);
                y = Mathf.RoundToInt(t.position.y);
            }

            string type = "grass";
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                string spriteName = sr.sprite.name.ToLower();
                if (spriteName.Contains("water")) type = "water";
                else if (spriteName.Contains("mountain")) type = "mountain";
                else if (spriteName.Contains("forest")) type = "forest";
                else if (spriteName.Contains("road")) type = "road";
            }
            else if (t.name.ToLower().Contains("mountain")) type = "mountain";
            else if (t.name.ToLower().Contains("water")) type = "water";

            thTile.Setup(x, y, type);
        }

        private static void FixBaseScene()
        {
            if (!File.Exists("Assets/Scenes/Base.unity")) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Base.unity");

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var backBtn = canvas.transform.Find("BackToMapButton");
            if (backBtn == null)
            {
                var go = new GameObject("BackToMapButton", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(canvas.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
                rt.anchoredPosition = new Vector2(30, 30);
                rt.sizeDelta = new Vector2(180, 60);
                go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);
                var t = textGo.GetComponent<Text>();
                t.text = "На карту";
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 24;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                textGo.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
                
                backBtn = go.transform;
                Debug.Log("[TheHeroFix] Created BackToMapButton in Base.");
            }

            if (canvas.GetComponent<THBaseExitFix>() == null)
            {
                canvas.gameObject.AddComponent<THBaseExitFix>();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[TheHeroFix] Base scene fixed.");
        }
    }
}
