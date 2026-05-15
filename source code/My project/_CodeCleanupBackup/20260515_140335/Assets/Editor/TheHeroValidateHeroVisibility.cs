using UnityEngine;
using UnityEditor;
using System.Linq;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroValidateHeroVisibility : EditorWindow
    {
        [MenuItem("The Hero/Validation/Validate Hero Visibility And Movement")]
        public static void Validate()
        {
            Debug.Log("[TheHeroHeroValidation] Starting validation...");
            bool pass = true;

            // 1. Hero exists and unique
            var heroes = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(o => (o.name == "Hero" || o.name == "PlayerHero" || o.name == "THHero") && !o.name.Contains("Panel") && !o.name.Contains("Text"))
                .ToList();

            if (heroes.Count == 0)
            {
                Debug.LogError("[TheHeroHeroValidation] FAIL: Hero not found");
                pass = false;
            }
            else if (heroes.Count > 1 && heroes.Count(h => h.activeSelf) > 1)
            {
                Debug.LogError("[TheHeroHeroValidation] FAIL: Multiple active heroes found");
                pass = false;
            }
            else
            {
                Debug.Log("[TheHeroHeroValidation] PASS: Hero exists and is unique");
            }

            var hero = heroes.FirstOrDefault(h => h.activeSelf);
            if (hero != null)
            {
                // 2. SpriteRenderer and Sprite
                var sr = hero.GetComponent<SpriteRenderer>();
                if (sr == null || !sr.enabled)
                {
                    Debug.LogError("[TheHeroHeroValidation] FAIL: Hero SpriteRenderer missing or disabled");
                    pass = false;
                }
                else if (sr.sprite == null)
                {
                    Debug.LogError("[TheHeroHeroValidation] FAIL: Hero sprite is null");
                    pass = false;
                }
                else if (!sr.sprite.name.Contains("Warrior"))
                {
                    Debug.LogWarning("[TheHeroHeroValidation] WARNING: Hero sprite might not be Tiny Swords Warrior");
                }
                else
                {
                    Debug.Log("[TheHeroHeroValidation] PASS: Hero visible");
                }

                // 3. Sorting Order
                if (sr != null && sr.sortingOrder < 100)
                {
                    Debug.LogWarning("[TheHeroHeroValidation] WARNING: Hero sortingOrder might be too low (" + sr.sortingOrder + ")");
                }

                // 4. Movement Script
                var mover = hero.GetComponent<THStrictGridHeroMovement>();
                if (mover == null)
                {
                    Debug.LogError("[TheHeroHeroValidation] FAIL: THStrictGridHeroMovement missing on Hero");
                    pass = false;
                }
                else
                {
                    Debug.Log("[TheHeroHeroValidation] PASS: Hero movement script exists");
                }
            }

            // 5. Camera
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[TheHeroHeroValidation] FAIL: Main Camera not found");
                pass = false;
            }
            else
            {
                var follow = cam.GetComponent<THCameraFollow>();
                if (follow == null || follow.Target == null)
                {
                    Debug.LogError("[TheHeroHeroValidation] FAIL: Camera follow missing or target null");
                    pass = false;
                }
                else
                {
                    Debug.Log("[TheHeroHeroValidation] PASS: Camera follows Hero");
                }

                if (cam.orthographicSize < 5 || cam.orthographicSize > 9)
                {
                    Debug.LogWarning("[TheHeroHeroValidation] WARNING: Camera orthographic size is unusual (" + cam.orthographicSize + ")");
                }
            }

            // 6. Enemies
            var enemies = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                .Where(o => o.type == THMapObject.ObjectType.Enemy)
                .ToList();
            
            int tinySwordsEnemies = enemies.Count(e => {
                var esr = e.GetComponent<SpriteRenderer>();
                return esr != null && esr.sprite != null && esr.sprite.name.Contains("Warrior");
            });

            if (tinySwordsEnemies < 3 && enemies.Count >= 3)
            {
                Debug.LogWarning("[TheHeroHeroValidation] WARNING: Less than 3 enemies use Tiny Swords Warrior sprites");
            }
            else
            {
                Debug.Log("[TheHeroHeroValidation] PASS: Tiny Swords sprites used for enemies");
            }

            if (pass) Debug.Log("[TheHeroHeroValidation] ALL CRITICAL CHECKS PASSED");
            else Debug.LogError("[TheHeroHeroValidation] VALIDATION FAILED");
        }
    }
}
