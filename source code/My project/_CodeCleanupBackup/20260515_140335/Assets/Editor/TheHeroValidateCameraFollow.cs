using System;
using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TheHero.Editor
{
    public static class TheHeroValidateCameraFollow
    {
        private const string MapScenePath = "Assets/Scenes/Map.unity";
        private static readonly string[] HeroNames = { "Hero", "Player", "PlayerHero", "THHero", "MapHero", "HeroMarker" };

        [MenuItem("The Hero/Validation/Validate Camera Follow")]
        public static void ValidateCameraFollow()
        {
            int fails = RunValidation(true);
            if (fails == 0)
                Debug.Log("[TheHeroCameraValidation] PASS Camera follow validation ready");
            else
                Debug.LogError($"[TheHeroCameraValidation] FAIL {fails} issue(s)");
        }

        public static int RunValidation(bool logSummary)
        {
            EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
            int fails = 0;

            List<GameObject> activeHeroes = FindHeroCandidates()
                .Where(h => h.activeInHierarchy)
                .ToList();

            if (activeHeroes.Count == 1)
                Pass("Hero exists");
            else
                Fail(ref fails, $"Expected exactly one active Hero, found {activeHeroes.Count}");

            GameObject hero = activeHeroes.FirstOrDefault();
            SpriteRenderer heroRenderer = hero != null ? hero.GetComponent<SpriteRenderer>() : null;

            if (heroRenderer != null)
                Pass("Hero has SpriteRenderer");
            else
                Fail(ref fails, "Hero SpriteRenderer missing");

            if (heroRenderer != null && heroRenderer.sprite != null)
                Pass("Hero sprite not null");
            else
                Fail(ref fails, "Hero sprite is null");

            if (hero != null && !IsZeroScale(hero.transform.localScale))
                Pass("Hero scale not zero");
            else
                Fail(ref fails, "Hero scale is zero");

            int terrainSorting = GetTerrainSortingOrder();
            if (heroRenderer != null && heroRenderer.sortingOrder > terrainSorting)
                Pass("Hero sorting above terrain");
            else
                Fail(ref fails, $"Hero sortingOrder must be above terrain ({terrainSorting})");

            Camera camera = Camera.main;
            if (camera != null)
                Pass("Main Camera exists");
            else
                Fail(ref fails, "Main Camera missing");

            if (camera != null && camera.orthographic)
                Pass("Main Camera is Orthographic");
            else
                Fail(ref fails, "Main Camera is not Orthographic");

            THCameraFollow follow = camera != null ? camera.GetComponent<THCameraFollow>() : null;
            if (follow != null)
                Pass("Main Camera has THCameraFollow");
            else
                Fail(ref fails, "THCameraFollow missing on Main Camera");

            if (follow != null && hero != null && follow.Target == hero.transform)
                Pass("Camera follows Hero");
            else
                Fail(ref fails, "THCameraFollow target is not active Hero");

            if (camera != null && camera.orthographicSize >= 5f && camera.orthographicSize <= 9f)
                Pass("Camera orthographicSize in range");
            else
                Fail(ref fails, "Camera orthographicSize must be between 5 and 9");

            if (camera != null && Mathf.Abs(camera.transform.position.z + 10f) < 0.01f)
                Pass("Camera z is -10");
            else
                Fail(ref fails, "Camera z must be -10");

            if (!HasGiantCastleUi())
                Pass("No giant Castle UI");
            else
                Fail(ref fails, "Giant Castle UI still exists");

            Button castleButton = FindCastleButton();
            if (castleButton != null)
                Pass("Small CastleButton exists");
            else
                Fail(ref fails, "Small CastleButton missing");

            if (castleButton != null && IsSmallButton(castleButton.GetComponent<RectTransform>()))
                Pass("CastleButton size is safe");
            else
                Fail(ref fails, "CastleButton is larger than 250x100");

            if (MovementReferencesHero(hero))
                Pass("Movement script references active Hero");
            else
                Fail(ref fails, "Movement script does not reference active Hero");

            if (logSummary)
            {
                if (fails == 0)
                    Debug.Log("[TheHeroCameraValidation] PASS No giant Castle UI");
                Debug.Log($"[TheHeroCameraValidation] Summary: {15 - fails}/15 checks passed, {fails} failed.");
            }

            return fails;
        }

        private static IEnumerable<GameObject> FindHeroCandidates()
        {
            return UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(go =>
                {
                    if (go == null) return false;
                    if (go.GetComponentInParent<Canvas>() != null) return false;
                    if (go.name.StartsWith("Deprecated_", StringComparison.Ordinal)) return false;

                    return HeroNames.Any(n => go.name == n) ||
                           go.GetComponent<THStrictGridHeroMovement>() != null ||
                           go.GetComponent<THHero>() != null;
                })
                .Distinct();
        }

        private static bool IsZeroScale(Vector3 scale)
        {
            return Mathf.Approximately(scale.x, 0f) ||
                   Mathf.Approximately(scale.y, 0f) ||
                   Mathf.Approximately(scale.z, 0f);
        }

        private static int GetTerrainSortingOrder()
        {
            int order = 0;
            var tileRenderers = UnityEngine.Object.FindObjectsByType<THTile>(FindObjectsInactive.Include)
                .Select(t => t.GetComponent<SpriteRenderer>())
                .Where(sr => sr != null);

            foreach (var renderer in tileRenderers)
                order = Mathf.Max(order, renderer.sortingOrder);

            return order;
        }

        private static bool HasGiantCastleUi()
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button == null || !button.gameObject.activeInHierarchy) continue;
                bool castleRelated = IsCastleName(button.name) ||
                                     button.GetComponentsInChildren<Text>(true).Any(IsCastleText);
                if (!castleRelated) continue;

                if (!IsSmallButton(button.GetComponent<RectTransform>()))
                    return true;
            }

            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas == null || !canvas.gameObject.activeInHierarchy) continue;
                bool castleRelated = canvas.GetComponentsInChildren<Text>(true).Any(IsCastleText);
                if (canvas.renderMode == RenderMode.WorldSpace && castleRelated)
                    return true;
            }

            return false;
        }

        private static Button FindCastleButton()
        {
            return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .FirstOrDefault(b => b != null && b.gameObject.activeInHierarchy && b.name == "CastleButton");
        }

        private static bool IsSmallButton(RectTransform rect)
        {
            if (rect == null) return false;

            Vector2 rectSize = rect.rect.size;
            Vector2 delta = rect.sizeDelta;
            float width = Mathf.Max(Mathf.Abs(rectSize.x), Mathf.Abs(delta.x));
            float height = Mathf.Max(Mathf.Abs(rectSize.y), Mathf.Abs(delta.y));

            return width <= 250f && height <= 100f;
        }

        private static bool MovementReferencesHero(GameObject hero)
        {
            if (hero == null) return false;

            var mover = hero.GetComponent<THStrictGridHeroMovement>();
            if (mover == null || !mover.enabled) return false;

            var controllers = UnityEngine.Object.FindObjectsByType<THMapController>(FindObjectsInactive.Include);
            return controllers.Length == 0 || controllers.All(c => c.HeroMover == mover);
        }

        private static bool IsCastleText(Text text)
        {
            if (text == null || string.IsNullOrWhiteSpace(text.text)) return false;
            string value = text.text.Trim();
            return value.IndexOf("ЗАМОК", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("CASTLE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("Замок", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsCastleName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return name.IndexOf("Castle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("BaseButton", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("ЗАМОК", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void Pass(string message)
        {
            Debug.Log("[TheHeroCameraValidation] PASS " + message);
        }

        private static void Fail(ref int fails, string message)
        {
            fails++;
            Debug.LogError("[TheHeroCameraValidation] FAIL " + message);
        }
    }
}
