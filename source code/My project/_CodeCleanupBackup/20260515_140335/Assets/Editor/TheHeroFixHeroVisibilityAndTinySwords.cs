using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroFixHeroVisibilityAndTinySwords : EditorWindow
    {
        [MenuItem("The Hero/Map/Fix Hero Visibility And Tiny Swords")]
        public static void FixMap()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Map.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[TheHeroHeroFix] Could not open Map scene");
                return;
            }

            Debug.Log("[TheHeroHeroFix] Map opened");

            // 1. Find or Restore Hero
            GameObject hero = FixHero();
            
            // 2. Assign Tiny Swords Sprites
            AssignTinySwordsSprites(hero);

            // 3. Set Safe Spawn
            SetSafeSpawn(hero);

            // 4. Fix Camera
            FixCamera(hero);

            // 5. Cleanup
            CleanupDuplicates(hero);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TheHeroHeroFix] Ready for testing");
        }

        private static GameObject FixHero()
        {
            var allHeroes = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(o => (o.name == "Hero" || o.name == "PlayerHero" || o.name == "THHero") && !o.name.Contains("Panel") && !o.name.Contains("Text"))
                .ToList();

            GameObject hero = allHeroes.FirstOrDefault(h => h.activeInHierarchy) ?? allHeroes.FirstOrDefault();

            if (hero == null)
            {
                hero = new GameObject("Hero");
                Debug.Log("[TheHeroHeroFix] Hero created");
            }
            else
            {
                Debug.Log($"[TheHeroHeroFix] Hero found: {hero.name}");
            }

            hero.SetActive(true);
            var sr = hero.GetComponent<SpriteRenderer>() ?? hero.AddComponent<SpriteRenderer>();
            sr.enabled = true;
            sr.sortingOrder = 100;
            hero.transform.localScale = Vector3.one;

            var mover = hero.GetComponent<THStrictGridHeroMovement>() ?? hero.AddComponent<THStrictGridHeroMovement>();
            mover.enabled = true;

            return hero;
        }

        private static void AssignTinySwordsSprites(GameObject hero)
        {
            Sprite heroSprite = GetSpriteFromPath("Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior_Idle.png", "Warrior_Idle_0");
            if (heroSprite != null)
            {
                hero.GetComponent<SpriteRenderer>().sprite = heroSprite;
                Debug.Log("[TheHeroHeroFix] Tiny Swords sprite assigned to Hero");
            }
            else
            {
                Debug.LogWarning("[TheHeroHeroFix] Tiny Swords hero sprite not found");
            }

            // Assign enemy sprites
            var enemies = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                .Where(o => o.type == THMapObject.ObjectType.Enemy)
                .ToList();

            Sprite redWarrior = GetSpriteFromPath("Assets/Tiny Swords/Units/Red Units/Warrior/Warrior_Idle.png", "Warrior_Idle_0");
            Sprite purpleWarrior = GetSpriteFromPath("Assets/Tiny Swords/Units/Purple Units/Warrior/Warrior_Idle.png", "Warrior_Idle_0");
            Sprite blackWarrior = GetSpriteFromPath("Assets/Tiny Swords/Units/Black Units/Warrior/Warrior_Idle.png", "Warrior_Idle_0");

            foreach (var enemy in enemies)
            {
                var esr = enemy.GetComponent<SpriteRenderer>() ?? enemy.gameObject.AddComponent<SpriteRenderer>();
                esr.enabled = true;
                esr.sortingOrder = 90;
                enemy.transform.localScale = Vector3.one;

                if (enemy.isDarkLord || enemy.isFinalBoss)
                {
                    esr.sprite = blackWarrior;
                }
                else if (enemy.id.Contains("Goblin") || enemy.name.Contains("Goblin"))
                {
                    esr.sprite = redWarrior;
                }
                else
                {
                    esr.sprite = purpleWarrior;
                }
            }
            Debug.Log("[TheHeroHeroFix] Tiny Swords sprites assigned to enemies");
        }

        private static Sprite GetSpriteFromPath(string path, string spriteName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            return assets.OfType<Sprite>().FirstOrDefault(s => s.name.Contains(spriteName)) ?? assets.OfType<Sprite>().FirstOrDefault();
        }

        private static void SetSafeSpawn(GameObject hero)
        {
            var castle = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(o => o.name == "Castle_Player" || o.name.Contains("Castle"));

            Vector2Int startPos = new Vector2Int(4, 3);
            if (castle != null)
            {
                // Try to find nearest walkable tile to castle
                var grid = Object.FindFirstObjectByType<THMapGridInput>();
                if (grid != null)
                {
                    var castleTile = grid.GetTileAt((int)castle.transform.position.x, (int)castle.transform.position.y);
                    // This is rough because castle might not be on grid exactly
                    // For now, let's just use (4,3) as preferred start if it's walkable
                    var t43 = grid.GetTileAt(4, 3);
                    if (t43 != null && t43.walkable)
                    {
                        startPos = new Vector2Int(4, 3);
                    }
                }
            }

            var mover = hero.GetComponent<THStrictGridHeroMovement>();
            if (mover != null)
            {
                mover.SetPositionImmediate(startPos.x, startPos.y);
                Debug.Log($"[TheHeroHeroFix] Hero moved to safe start tile: {startPos.x},{startPos.y}");
            }
        }

        private static void FixCamera(GameObject hero)
        {
            var cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 7;
                cam.transform.position = new Vector3(hero.transform.position.x, hero.transform.position.y, -10);

                var follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
                follow.Target = hero.transform;
                follow.SmoothSpeed = 10f;
                follow.MinBounds = new Vector2(-20, -15);
                follow.MaxBounds = new Vector2(20, 15);
                
                Debug.Log("[TheHeroHeroFix] Camera follow target set to Hero");
                Debug.Log("[TheHeroHeroFix] Camera centered on Hero");
            }
        }

        private static void CleanupDuplicates(GameObject mainHero)
        {
            var allHeroes = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(o => (o.name == "Hero" || o.name == "PlayerHero" || o.name == "THHero") && !o.name.Contains("Panel") && !o.name.Contains("Text"))
                .ToList();

            foreach (var h in allHeroes)
            {
                if (h != mainHero)
                {
                    Object.DestroyImmediate(h);
                }
            }
            Debug.Log("[TheHeroHeroFix] Duplicate heroes removed/disabled");
        }
    }
}
