using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheHero.Editor
{
    public static class TheHeroFixCameraAndHeroFollow
    {
        private const string MapScenePath = "Assets/Scenes/Map.unity";
        private const string ReportPath = "Assets/CodeAudit/Camera_Hero_Follow_Fix_Report.md";
        private static readonly string[] HeroNames = { "Hero", "Player", "PlayerHero", "THHero", "MapHero", "HeroMarker" };

        [MenuItem("The Hero/Map/Fix Camera And Hero Follow")]
        public static void FixCameraAndHeroFollow()
        {
            var report = new FixReport();
            var scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
            Debug.Log("[TheHeroCameraFix] Map opened");

            GameObject hero = FindOrCreateActiveHero(report);
            Debug.Log("[TheHeroCameraFix] Hero checked");
            Debug.Log("[TheHeroCameraFix] Active hero selected: " + hero.name);

            THStrictGridHeroMovement mover = FixHeroVisualsAndMovement(hero, report);
            FixDuplicateHeroes(hero, report);
            Debug.Log("[TheHeroCameraFix] Duplicate heroes fixed");

            VerifyHeroVisualPosition(hero, mover, report);
            Debug.Log("[TheHeroCameraFix] Hero visual position verified");

            FixMovementReferences(hero, mover, report);
            Camera camera = ConfigureMainCamera(hero.transform, report);
            Debug.Log("[TheHeroCameraFix] Main Camera configured");

            ConfigureCameraFollow(camera, hero.transform, report);
            Debug.Log("[TheHeroCameraFix] Camera follow installed");
            Debug.Log("[TheHeroCameraFix] Camera bounds updated");

            Canvas canvas = EnsureScreenCanvas();
            Button smallCastleButton = EnsureSmallCastleButton(canvas);
            int removedUiCount = RemoveGiantCastleUi(smallCastleButton.gameObject, report);
            Debug.Log("[TheHeroCameraFix] Giant Castle UI removed");
            Debug.Log("[TheHeroCameraFix] Small Castle button fixed");
            Debug.Log("[TheHeroCameraFix] Small Castle button ready");

            FixHeroSorting(hero);
            Debug.Log("[TheHeroCameraFix] Hero sorting fixed");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            report.GiantUiRemovedCount = removedUiCount;
            report.ValidationFails = TheHeroValidateCameraFollow.RunValidation(false);
            WriteReport(report);
            AssetDatabase.Refresh();

            if (report.ValidationFails == 0)
            {
                Debug.Log("[TheHeroCameraFix] Ready for testing");
            }
            else
            {
                Debug.LogError($"[TheHeroCameraFix] Validation failed: {report.ValidationFails} issue(s)");
            }
        }

        private static GameObject FindOrCreateActiveHero(FixReport report)
        {
            List<GameObject> heroes = FindHeroCandidates(FindObjectsInactive.Include).ToList();
            foreach (var hero in heroes)
                Debug.Log("[TheHeroCameraFix] Hero found: " + hero.name);

            report.DuplicateHeroes = heroes.Count > 1
                ? heroes.Select(h => h.name).ToList()
                : new List<string>();

            GameObject selected = heroes
                .Where(h => h.activeInHierarchy)
                .OrderByDescending(h => h.GetComponent<THStrictGridHeroMovement>() != null)
                .ThenByDescending(h => h.name == "Hero")
                .FirstOrDefault();

            if (selected == null)
            {
                selected = heroes
                    .OrderByDescending(h => h.GetComponent<THStrictGridHeroMovement>() != null)
                    .ThenByDescending(h => h.name == "Hero")
                    .FirstOrDefault();
            }

            if (selected == null)
            {
                selected = new GameObject("Hero");
                selected.transform.position = FindNearestWalkableTilePosition(out Vector2Int grid);
                var mover = selected.AddComponent<THStrictGridHeroMovement>();
                mover.currentX = grid.x;
                mover.currentY = grid.y;
                report.HeroCreated = true;
            }

            selected.name = "Hero";
            selected.SetActive(true);
            report.SelectedHeroName = selected.name;
            return selected;
        }

        private static THStrictGridHeroMovement FixHeroVisualsAndMovement(GameObject hero, FixReport report)
        {
            var sr = hero.GetComponent<SpriteRenderer>() ?? hero.AddComponent<SpriteRenderer>();
            sr.enabled = true;
            if (sr.sprite == null)
                sr.sprite = FindFallbackHeroSprite();
            sr.sortingOrder = 100;

            Vector3 scale = hero.transform.localScale;
            if (Mathf.Approximately(scale.x, 0f)) scale.x = 1f;
            if (Mathf.Approximately(scale.y, 0f)) scale.y = 1f;
            if (Mathf.Approximately(scale.z, 0f)) scale.z = 1f;
            hero.transform.localScale = scale;

            var mover = hero.GetComponent<THStrictGridHeroMovement>() ?? hero.AddComponent<THStrictGridHeroMovement>();
            mover.enabled = true;

            foreach (var other in hero.GetComponents<MonoBehaviour>())
            {
                if (other == null || other == mover) continue;
                string typeName = other.GetType().Name;
                if (typeName == nameof(THGuaranteedHeroMovement) ||
                    typeName == nameof(THReliableHeroMovement) ||
                    typeName == nameof(THHeroMover))
                {
                    other.enabled = false;
                }
            }

            report.MovementScript = nameof(THStrictGridHeroMovement);
            return mover;
        }

        private static void FixDuplicateHeroes(GameObject activeHero, FixReport report)
        {
            foreach (var hero in FindHeroCandidates(FindObjectsInactive.Include))
            {
                if (hero == activeHero) continue;

                if (!hero.name.StartsWith("Deprecated_", StringComparison.Ordinal))
                    hero.name = "Deprecated_" + hero.name;
                hero.SetActive(false);
            }
        }

        private static void VerifyHeroVisualPosition(GameObject hero, THStrictGridHeroMovement mover, FixReport report)
        {
            THTile currentTile = FindTileAt(mover.currentX, mover.currentY);
            if (currentTile == null || !currentTile.walkable)
            {
                Vector3 safePosition = FindNearestWalkableTilePosition(out Vector2Int safeGrid);
                mover.currentX = safeGrid.x;
                mover.currentY = safeGrid.y;
                hero.transform.position = new Vector3(safePosition.x, safePosition.y, 0f);
                report.HeroMovedToWalkableTile = true;
            }
            else
            {
                Vector3 target = currentTile.transform.position;
                hero.transform.position = new Vector3(target.x, target.y, 0f);
            }

            report.HeroGrid = new Vector2Int(mover.currentX, mover.currentY);
            report.HeroWorld = hero.transform.position;
            report.TransformMovementStatus = "THStrictGridHeroMovement updates Hero.transform in SetPositionImmediate and MoveAlongPath.";
        }

        private static void FixMovementReferences(GameObject hero, THStrictGridHeroMovement mover, FixReport report)
        {
            foreach (var controller in UnityEngine.Object.FindObjectsByType<THMapController>(FindObjectsInactive.Include))
            {
                controller.HeroMover = mover;
                EditorUtility.SetDirty(controller);
            }

            report.MovementReferences = "THMapController.HeroMover and THCameraFollow target point to the active Hero.";
        }

        private static Camera ConfigureMainCamera(Transform hero, FixReport report)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)
                    .OrderByDescending(c => c.name == "Main Camera")
                    .FirstOrDefault();
            }

            if (camera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                camera = cameraGo.AddComponent<Camera>();
                cameraGo.AddComponent<AudioListener>();
            }

            camera.gameObject.name = "Main Camera";
            camera.gameObject.SetActive(true);
            camera.enabled = true;
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.transform.rotation = Quaternion.identity;
            camera.transform.position = new Vector3(hero.position.x, hero.position.y, -10f);

            foreach (var duplicate in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (duplicate == camera) continue;
                duplicate.enabled = false;
                if (duplicate.CompareTag("MainCamera"))
                    duplicate.tag = "Untagged";
            }

            report.CameraSettings = "Main Camera: Orthographic, size 7.5, z -10, THCameraFollow target = Hero.";
            return camera;
        }

        private static void ConfigureCameraFollow(Camera camera, Transform hero, FixReport report)
        {
            var follow = camera.GetComponent<THCameraFollow>() ?? camera.gameObject.AddComponent<THCameraFollow>();
            follow.followSpeed = 8f;
            follow.z = -10f;
            follow.useBounds = true;

            if (THCameraFollow.TryCalculateSceneMapBounds(out Bounds bounds))
            {
                follow.Configure(hero, bounds, true);
                report.CameraBounds = $"min=({bounds.min.x:0.##},{bounds.min.y:0.##}) max=({bounds.max.x:0.##},{bounds.max.y:0.##})";
            }
            else
            {
                follow.Target = hero;
                follow.useBounds = false;
                follow.CenterImmediately();
                report.CameraBounds = "Map bounds were not detected; follow remains enabled without clamp.";
            }
        }

        private static Canvas EnsureScreenCanvas()
        {
            Canvas canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
                .Where(c => c.renderMode != RenderMode.WorldSpace)
                .OrderByDescending(c => c.name == "Canvas")
                .FirstOrDefault();

            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.gameObject.SetActive(true);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            return canvas;
        }

        private static Button EnsureSmallCastleButton(Canvas canvas)
        {
            Button button = canvas.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(b => b.name == "CastleButton");

            if (button == null)
            {
                var go = new GameObject("CastleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
                go.transform.SetParent(canvas.transform, false);
                button = go.GetComponent<Button>();
            }

            button.gameObject.name = "CastleButton";
            button.gameObject.SetActive(true);
            button.transform.SetParent(canvas.transform, false);

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(150f, 50f);
            rect.localScale = Vector3.one;

            var image = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            image.raycastTarget = true;
            button.targetGraphic = image;

            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.4f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var text = button.GetComponentsInChildren<Text>(true).FirstOrDefault();
            if (text == null)
            {
                var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textGo.transform.SetParent(button.transform, false);
                text = textGo.GetComponent<Text>();
            }

            text.gameObject.SetActive(true);
            text.text = "ЗАМОК";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            textRect.localScale = Vector3.one;

            button.onClick.RemoveAllListeners();
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);

            var controller = UnityEngine.Object.FindFirstObjectByType<THMapController>();
            if (controller != null)
                UnityEventTools.AddPersistentListener(button.onClick, controller.GoToBase);

            EditorUtility.SetDirty(button);
            return button;
        }

        private static int RemoveGiantCastleUi(GameObject smallButton, FixReport report)
        {
            int removed = 0;
            var removedNames = new List<string>();

            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (IsProtectedCastleButtonHierarchy(canvas.gameObject, smallButton))
                    continue;

                bool containsCastleText = canvas.GetComponentsInChildren<Text>(true).Any(IsCastleText);
                bool containsCastleButton = canvas.GetComponentsInChildren<Button>(true)
                    .Any(b => b != null && IsCastleName(b.name));

                if (canvas.renderMode == RenderMode.WorldSpace && (containsCastleText || containsCastleButton))
                {
                    DisableDeprecatedUi(canvas.gameObject, removedNames);
                    removed++;
                }
            }

            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button == null || IsProtectedCastleButtonHierarchy(button.gameObject, smallButton)) continue;

                bool castleRelated = IsCastleName(button.name) ||
                                     button.GetComponentsInChildren<Text>(true).Any(IsCastleText);
                if (!castleRelated) continue;

                var rect = button.GetComponent<RectTransform>();
                if (rect != null && IsGiant(rect))
                {
                    DisableDeprecatedUi(button.gameObject, removedNames);
                    removed++;
                }
            }

            foreach (var rect in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
            {
                if (rect == null || IsProtectedCastleButtonHierarchy(rect.gameObject, smallButton)) continue;
                if (!IsCastleName(rect.name) && !rect.GetComponentsInChildren<Text>(true).Any(IsCastleText))
                    continue;

                if (IsGiant(rect))
                {
                    DisableDeprecatedUi(rect.gameObject, removedNames);
                    removed++;
                }
            }

            report.RemovedCastleUi = removedNames;
            return removed;
        }

        private static bool IsProtectedCastleButtonHierarchy(GameObject candidate, GameObject smallButton)
        {
            if (candidate == null || smallButton == null)
                return false;

            Transform candidateTransform = candidate.transform;
            Transform buttonTransform = smallButton.transform;
            return candidate == smallButton ||
                   candidateTransform.IsChildOf(buttonTransform) ||
                   buttonTransform.IsChildOf(candidateTransform);
        }

        private static void FixHeroSorting(GameObject hero)
        {
            var heroRenderer = hero.GetComponent<SpriteRenderer>() ?? hero.AddComponent<SpriteRenderer>();
            heroRenderer.enabled = true;
            heroRenderer.sortingOrder = 100;
        }

        private static IEnumerable<GameObject> FindHeroCandidates(FindObjectsInactive inactiveMode)
        {
            return UnityEngine.Object.FindObjectsByType<GameObject>(inactiveMode, FindObjectsSortMode.None)
                .Where(IsHeroCandidate)
                .Distinct();
        }

        private static bool IsHeroCandidate(GameObject go)
        {
            if (go == null) return false;
            if (go.GetComponentInParent<Canvas>() != null) return false;
            if (go.name.StartsWith("Deprecated_", StringComparison.Ordinal)) return false;

            return HeroNames.Any(n => go.name == n) ||
                   go.GetComponent<THStrictGridHeroMovement>() != null ||
                   go.GetComponent<THHero>() != null;
        }

        private static THTile FindTileAt(int x, int y)
        {
            return UnityEngine.Object.FindObjectsByType<THTile>(FindObjectsInactive.Include)
                .FirstOrDefault(t => t != null && t.x == x && t.y == y);
        }

        private static Vector3 FindNearestWalkableTilePosition(out Vector2Int grid)
        {
            var blockers = new HashSet<Vector2Int>(UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                .Where(o => o != null && o.blocksMovement && o.type != THMapObject.ObjectType.Base)
                .Select(o => new Vector2Int(o.targetX, o.targetY)));

            Vector3 reference = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                .Where(o => o != null && o.type == THMapObject.ObjectType.Base)
                .OrderByDescending(o => o.id == "Castle_Player" || o.name == "Castle_Player")
                .Select(o => o.transform.position)
                .DefaultIfEmpty(Vector3.zero)
                .First();

            THTile tile = UnityEngine.Object.FindObjectsByType<THTile>(FindObjectsInactive.Include)
                .Where(t => t != null && t.walkable && !blockers.Contains(new Vector2Int(t.x, t.y)))
                .OrderBy(t => Vector2.Distance(t.transform.position, reference))
                .FirstOrDefault();

            if (tile == null)
            {
                grid = new Vector2Int(4, 3);
                return new Vector3(4f, 3f, 0f);
            }

            grid = new Vector2Int(tile.x, tile.y);
            return new Vector3(tile.transform.position.x, tile.transform.position.y, 0f);
        }

        private static Sprite FindFallbackHeroSprite()
        {
            string[] guids = AssetDatabase.FindAssets("Warrior_Idle t:Sprite");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    return sprite;

                sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
                if (sprite != null)
                    return sprite;
            }

            return AssetDatabase.FindAssets("t:Sprite")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .FirstOrDefault(s => s != null);
        }

        private static bool IsCastleText(Text text)
        {
            if (text == null || string.IsNullOrWhiteSpace(text.text)) return false;
            string value = text.text.Trim();
            return value.IndexOf("ЗАМОК", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("ЗАМOК", StringComparison.OrdinalIgnoreCase) >= 0 ||
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

        private static bool IsGiant(RectTransform rect)
        {
            Vector2 rectSize = rect.rect.size;
            Vector2 delta = rect.sizeDelta;
            float width = Mathf.Max(Mathf.Abs(rectSize.x), Mathf.Abs(delta.x));
            float height = Mathf.Max(Mathf.Abs(rectSize.y), Mathf.Abs(delta.y));
            Vector2 stretch = rect.anchorMax - rect.anchorMin;

            return width > 250f ||
                   height > 100f ||
                   (stretch.x > 0.65f && stretch.y > 0.65f);
        }

        private static void DisableDeprecatedUi(GameObject go, List<string> removedNames)
        {
            if (go == null || !go.activeSelf) return;

            removedNames.Add(go.name);
            if (!go.name.StartsWith("Deprecated_", StringComparison.Ordinal))
                go.name = "Deprecated_" + go.name;
            go.SetActive(false);
            EditorUtility.SetDirty(go);
        }

        private static void WriteReport(FixReport report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            var sb = new StringBuilder();
            sb.AppendLine("# Camera Hero Follow Fix Report");
            sb.AppendLine();
            sb.AppendLine($"1. Hero selected: {report.SelectedHeroName} at grid {report.HeroGrid.x},{report.HeroGrid.y}, world {report.HeroWorld.x:0.##},{report.HeroWorld.y:0.##}.");
            sb.AppendLine($"2. Duplicate heroes: {(report.DuplicateHeroes.Count == 0 ? "none" : string.Join(", ", report.DuplicateHeroes))}.");
            sb.AppendLine($"3. Hero Transform movement: {report.TransformMovementStatus}");
            sb.AppendLine($"4. Movement script: {report.MovementScript}. {report.MovementReferences}");
            sb.AppendLine($"5. Main Camera: {report.CameraSettings}");
            sb.AppendLine($"6. Camera bounds: {report.CameraBounds}");
            sb.AppendLine($"7. Castle UI cleanup: {(report.RemovedCastleUi.Count == 0 ? "no giant Castle UI objects were active; CastleButton was normalized to 150x50" : string.Join(", ", report.RemovedCastleUi))}.");
            sb.AppendLine($"8. Validation: {(report.ValidationFails == 0 ? "PASS" : "FAIL")} ({report.ValidationFails} issue(s)).");
            sb.AppendLine("9. Manual checks: Play, MainMenu -> New Game, verify Hero is visible, camera follows after clicks, and only the small Castle button remains in the lower-left corner.");
            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        }

        private sealed class FixReport
        {
            public string SelectedHeroName = "unknown";
            public bool HeroCreated;
            public bool HeroMovedToWalkableTile;
            public List<string> DuplicateHeroes = new List<string>();
            public Vector2Int HeroGrid;
            public Vector3 HeroWorld;
            public string TransformMovementStatus = "not checked";
            public string MovementScript = "unknown";
            public string MovementReferences = "not checked";
            public string CameraSettings = "not checked";
            public string CameraBounds = "not checked";
            public List<string> RemovedCastleUi = new List<string>();
            public int GiantUiRemovedCount;
            public int ValidationFails = -1;
        }
    }
}
