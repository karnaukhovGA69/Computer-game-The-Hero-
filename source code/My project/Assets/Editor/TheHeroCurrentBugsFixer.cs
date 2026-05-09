using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroCurrentBugsFixer
    {
        [MenuItem("The Hero/Fix/Fix Current Map And Startup Bugs")]
        public static void FixAllBugs()
        {
            Debug.Log("<b>[TheHeroFix] Starting combined bug fix...</b>");

            // 1. Fix Startup Scene Order
            TheHeroSceneOrderFixer.FixSceneOrder();

            // 2. Open Map scene
            if (EditorSceneManager.GetActiveScene().name != "Map")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");
            }

            // 3. Fix Grey Stripe (MiniMapPanel in world space?)
            FixGreyStripe();

            // 4. Fix Hero Visibility and Position
            FixHero();

            // 5. Fix Camera Map Centering
            FixCamera();

            // 6. Fix Map UI Layout
            FixUILayout();

            // 7. Save Map scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // 8. Open MainMenu scene
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            // 9. Save all assets
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[TheHeroFix] All bugs fixed successfully!</color>");
        }

        private static void FixGreyStripe()
        {
            // The log showed MiniMapPanel at (3680, -560, 0).
            var mmPanel = GameObject.Find("MiniMapPanel");
            if (mmPanel != null && mmPanel.transform.parent == null)
            {
                var canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    mmPanel.transform.SetParent(canvas.transform, false);
                    Debug.Log("[TheHeroFix] Moved MiniMapPanel to Canvas.");
                }
            }
            
            // Check for any huge mountains or walls
            var tiles = Object.FindObjectsByType<THMapTile>(FindObjectsInactive.Include);
            foreach (var tile in tiles)
            {
                if (tile.transform.position.x > 20 || tile.transform.position.x < -10)
                {
                    // Likely misplaced or border
                    Debug.Log($"[TheHeroFix] Found misplaced tile at {tile.transform.position}. Disabling.");
                    tile.gameObject.SetActive(false);
                }
            }
            Debug.Log("[TheHeroFix] Grey stripe checked/fixed");
        }

        private static void FixHero()
        {
            var hero = GameObject.Find("Hero");
            if (hero == null)
            {
                hero = new GameObject("Hero");
                hero.AddComponent<THHeroMover>();
            }

            var sr = hero.GetComponent<SpriteRenderer>();
            if (sr == null) sr = hero.AddComponent<SpriteRenderer>();
            
            // Bright purple for visibility
            sr.color = new Color(0.7f, 0f, 1f); 
            sr.sortingOrder = 25;
            
            if (sr.sprite == null)
            {
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Units/hero.png");
            }
            
            hero.transform.localScale = Vector3.one * 0.9f;
            
            // Place at valid start
            hero.transform.position = new Vector3(1, 1, 0);
            
            Debug.Log("[TheHeroFix] Hero visible and placed at (1, 1)");
        }

        private static void FixCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var fix = cam.GetComponent<THCameraMapFix>();
            if (fix == null) fix = cam.gameObject.AddComponent<THCameraMapFix>();
            fix.FixCamera();
            Debug.Log("[TheHeroFix] Camera centered");
        }

        private static void FixUILayout()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var fix = canvas.GetComponent<THMapUILayoutFix>();
            if (fix == null) fix = canvas.gameObject.AddComponent<THMapUILayoutFix>();
            fix.ApplyFix();
            Debug.Log("[TheHeroFix] Map UI layout fixed");
        }
    }
}
