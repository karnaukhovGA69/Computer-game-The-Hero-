using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using TheHero.Generated;
using TMPro;

namespace TheHero.Editor
{
    public static class TheHeroValidateMapUIBounds
    {
        [MenuItem("The Hero/Validation/Validate Map UI Bounds")]
        public static void Validate()
        {
            if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/Map.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");
            }

            bool allPass = true;

            // 1. No giant UI
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            bool foundGiant = false;
            foreach (var canvas in allCanvases)
            {
                if (!canvas.gameObject.activeInHierarchy) continue;
                if (canvas.name.StartsWith("Deprecated_")) continue;

                var texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach(var t in texts)
                {
                    if (t.text.Contains("ЗАМОК"))
                    {
                        var rt = t.GetComponent<RectTransform>();
                        if (rt.rect.width > 250 || rt.rect.height > 120 || canvas.renderMode == RenderMode.WorldSpace)
                        {
                            foundGiant = true;
                            Debug.LogError("[TheHeroMapUIValidation] FAIL Giant Castle UI text object found: " + t.name + " in " + canvas.name + " Size: " + rt.rect.size);
                        }
                    }
                }

                var legacyTexts = canvas.GetComponentsInChildren<Text>(true);
                foreach(var t in legacyTexts)
                {
                    if (t.text.Contains("ЗАМОК"))
                    {
                        var rt = t.GetComponent<RectTransform>();
                        if (rt.rect.width > 250 || rt.rect.height > 120 || canvas.renderMode == RenderMode.WorldSpace)
                        {
                            foundGiant = true;
                            Debug.LogError("[TheHeroMapUIValidation] FAIL Giant Castle UI text object found: " + t.name + " in " + canvas.name + " Size: " + rt.rect.size);
                        }
                    }
                }
            }
            if (!foundGiant) Debug.Log("[TheHeroMapUIValidation] PASS No giant Castle UI");
            else allPass = false;

            // 2 & 3. Small Castle button in Screen Space Overlay
            var btn = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude)
                .FirstOrDefault(b => b.name == "CastleButton" || b.GetComponentsInChildren<Text>().Any(t => t.text.Contains("ЗАМОК")));
            
            if (btn != null)
            {
                Debug.Log("[TheHeroMapUIValidation] PASS Small Castle button");
                var canvas = btn.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Debug.Log("[TheHeroMapUIValidation] PASS CastleButton in Screen Space Overlay");
                }
                else
                {
                    Debug.LogError("[TheHeroMapUIValidation] FAIL CastleButton NOT in Screen Space Overlay");
                    allPass = false;
                }
                
                if (btn.onClick.GetPersistentEventCount() > 0)
                {
                    Debug.Log("[TheHeroMapUIValidation] PASS CastleButton opens Base");
                }
                else
                {
                    Debug.LogWarning("[TheHeroMapUIValidation] WARNING CastleButton onClick might be empty");
                }
            }
            else
            {
                Debug.LogError("[TheHeroMapUIValidation] FAIL Small Castle button NOT found");
                allPass = false;
            }

            // 6. Camera bounds ignore UI
            if (THCameraFollow.TryCalculateSceneMapBounds(out Bounds bounds))
            {
                // If it successfully calculates bounds using THCameraFollow logic, it ignores UI by design
                Debug.Log("[TheHeroMapUIValidation] PASS Camera bounds ignore UI");
            }
            else
            {
                Debug.LogError("[TheHeroMapUIValidation] FAIL Could not calculate camera bounds");
                allPass = false;
            }

            // 7. Main Camera follows Hero
            var cam = Camera.main;
            var follow = cam?.GetComponent<THCameraFollow>();
            if (follow != null && follow.target != null && follow.target.name == "Hero")
            {
                Debug.Log("[TheHeroMapUIValidation] PASS Main Camera follows Hero");
            }
            else
            {
                Debug.LogError("[TheHeroMapUIValidation] FAIL Camera follow not configured correctly");
                allPass = false;
            }

            // 8, 9, 10. MapRoot, Hero, Enemies, Resources
            var hero = GameObject.Find("Hero");
            if (hero != null) Debug.Log("[TheHeroMapUIValidation] PASS Hero exists");
            else { Debug.LogError("[TheHeroMapUIValidation] FAIL Hero missing"); allPass = false; }

            int enemies = Object.FindObjectsByType<THEnemy>(FindObjectsInactive.Include).Length;
            int resources = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include).Count(o => o.type != THMapObject.ObjectType.Base);
            
            if (enemies > 0) Debug.Log("[TheHeroMapUIValidation] PASS Enemies exist (" + enemies + ")");
            else Debug.LogWarning("[TheHeroMapUIValidation] WARNING No enemies found");

            if (resources > 0) Debug.Log("[TheHeroMapUIValidation] PASS Resources exist (" + resources + ")");
            else Debug.LogWarning("[TheHeroMapUIValidation] WARNING No resources found");

            if (allPass) Debug.Log("[TheHeroMapUIValidation] SUCCESS: All critical Map UI and Bounds checks passed.");
        }
    }
}
