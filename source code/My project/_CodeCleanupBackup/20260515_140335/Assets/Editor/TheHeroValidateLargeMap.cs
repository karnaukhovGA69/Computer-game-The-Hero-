using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Linq;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroValidateLargeMap : EditorWindow
    {
        [MenuItem("The Hero/Validation/Validate Large Map")]
        public static void Validate()
        {
            Debug.Log("[TheHeroLargeMapValidation] Starting validation...");
            bool pass = true;

            // 1. Map size
            var thTiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            if (thTiles.Length < 2400) // 60x40 = 2400
            {
                Debug.LogError($"[TheHeroLargeMapValidation] FAIL: Map size too small. Found {thTiles.Length} tiles.");
                pass = false;
            }
            else
            {
                Debug.Log($"[TheHeroLargeMapValidation] PASS: Map size is {thTiles.Length} tiles.");
            }

            // 2. Tilemap Ground
            var groundTM = GameObject.Find("Ground_Tilemap")?.GetComponent<Tilemap>();
            if (groundTM == null)
            {
                Debug.LogError("[TheHeroLargeMapValidation] FAIL: Ground Tilemap not found.");
                pass = false;
            }
            else
            {
                Debug.Log("[TheHeroLargeMapValidation] PASS: Ground Tilemap exists.");
            }

            // 3. Hero
            var hero = GameObject.Find("Hero");
            if (hero == null || !hero.activeInHierarchy)
            {
                Debug.LogError("[TheHeroLargeMapValidation] FAIL: Hero not found or inactive.");
                pass = false;
            }
            else
            {
                var sr = hero.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null)
                {
                    Debug.LogError("[TheHeroLargeMapValidation] FAIL: Hero visible sprite missing.");
                    pass = false;
                }
                else
                {
                    Debug.Log("[TheHeroLargeMapValidation] PASS: Hero exists and visible.");
                }
            }

            // 4. Camera
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[TheHeroLargeMapValidation] FAIL: Main Camera not found.");
                pass = false;
            }
            else
            {
                var follow = cam.GetComponent<THCameraFollow>();
                if (follow == null || follow.Target == null)
                {
                    Debug.LogError("[TheHeroLargeMapValidation] FAIL: Camera follow or target missing.");
                    pass = false;
                }
                else
                {
                    Debug.Log("[TheHeroLargeMapValidation] PASS: Camera follow active.");
                }
            }

            // 5. Objects
            var castle = GameObject.Find("Castle_Player");
            if (castle == null)
            {
                Debug.LogError("[TheHeroLargeMapValidation] FAIL: Castle_Player not found.");
                pass = false;
            }
            else
            {
                Debug.Log("[TheHeroLargeMapValidation] PASS: Castle exists.");
            }

            var mapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            int resources = mapObjects.Count(o => o.type != THMapObject.ObjectType.Enemy && o.type != THMapObject.ObjectType.Base);
            int enemies = mapObjects.Count(o => o.type == THMapObject.ObjectType.Enemy);
            
            if (resources < 25)
            {
                Debug.LogError($"[TheHeroLargeMapValidation] FAIL: Not enough resources. Found {resources}.");
                pass = false;
            }
            else
            {
                Debug.Log($"[TheHeroLargeMapValidation] PASS: Resources count: {resources}.");
            }

            if (enemies < 25)
            {
                Debug.LogError($"[TheHeroLargeMapValidation] FAIL: Not enough enemies. Found {enemies}.");
                pass = false;
            }
            else
            {
                Debug.Log($"[TheHeroLargeMapValidation] PASS: Enemies count: {enemies}.");
            }

            var darkLord = mapObjects.FirstOrDefault(o => o.isFinalBoss);
            if (darkLord == null)
            {
                Debug.LogError("[TheHeroLargeMapValidation] FAIL: Dark Lord final boss not found.");
                pass = false;
            }
            else
            {
                Debug.Log("[TheHeroLargeMapValidation] PASS: Dark Lord exists.");
            }

            if (pass) Debug.Log("[TheHeroLargeMapValidation] ALL CRITICAL CHECKS PASSED");
            else Debug.LogError("[TheHeroLargeMapValidation] VALIDATION FAILED");
        }
    }
}
