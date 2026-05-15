using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace TheHero.Generated
{
    [DisallowMultipleComponent]
    public class THCameraFollow : MonoBehaviour
    {
        [FormerlySerializedAs("Target")]
        public Transform target;

        [FormerlySerializedAs("SmoothSpeed")]
        public float followSpeed = 8f;

        public bool useBounds = true;
        public Bounds mapBounds;
        public float z = -10f;

        [FormerlySerializedAs("AutoFindTarget")]
        public bool autoFindTarget = true;

        public Vector2 MinBounds;
        public Vector2 MaxBounds;

        private Camera _camera;
        private bool _warnedMissingBounds;

        public Transform Target
        {
            get => target;
            set
            {
                if (target == value) return;
                target = value;
                if (target != null)
                    Debug.Log("[TheHeroCameraFix] Camera target set to Hero");
            }
        }

        public float SmoothSpeed
        {
            get => followSpeed;
            set => followSpeed = value;
        }

        public bool AutoFindTarget
        {
            get => autoFindTarget;
            set => autoFindTarget = value;
        }

        private void Awake()
        {
            ConfigureCameraComponent();

            if (autoFindTarget && target == null)
                FindTarget();

            if (!HasUsableMapBounds() && TryCalculateSceneMapBounds(out Bounds detectedBounds))
            {
                mapBounds = detectedBounds;
                SyncLegacyBoundsFromMapBounds();
                Debug.Log("[TheHeroCameraFix] Map bounds detected: " + FormatBounds(mapBounds));
            }

            Debug.Log("[TheHeroCameraFix] Camera follow installed");
        }

        private void Start()
        {
            ConfigureCameraComponent();

            if (autoFindTarget && target == null)
                FindTarget();

            CenterImmediately();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                if (autoFindTarget)
                    FindTarget();
                return;
            }

            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, z);
            desiredPosition = ClampToMapBounds(desiredPosition);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }

        public void FindHero()
        {
            FindTarget();
        }

        public bool FindTarget()
        {
            Transform found = FindBestHeroTransform();
            if (found == null)
            {
                Debug.LogWarning("[TheHeroCameraFix] Hero target not found");
                return false;
            }

            Debug.Log("[TheHeroCameraFix] Hero found: " + found.name);
            Target = found;
            return true;
        }

        public void Configure(Transform newTarget, Bounds bounds, bool enableBounds = true)
        {
            target = newTarget;
            mapBounds = bounds;
            useBounds = enableBounds;
            ConfigureCameraComponent();
            SyncLegacyBoundsFromMapBounds();

            if (target != null)
            {
                Debug.Log("[TheHeroCameraFix] Camera target set to Hero");
            }

            if (HasUsableMapBounds())
            {
                Debug.Log("[TheHeroCameraFix] Map bounds detected: " + FormatBounds(mapBounds));
            }

            CenterImmediately();
        }

        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            MinBounds = new Vector2(minX, minY);
            MaxBounds = new Vector2(maxX, maxY);
        }

        public void CenterImmediately()
        {
            if (target == null) return;

            ConfigureCameraComponent();
            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, z);
            transform.position = ClampToMapBounds(desiredPosition);
            Debug.Log("[TheHeroCameraFix] Camera centered on Hero");
        }

        public Vector3 ClampToMapBounds(Vector3 desiredPosition)
        {
            desiredPosition.z = z;

            if (!useBounds)
                return desiredPosition;

            if (HasUsableMapBounds())
                return ClampToBounds(desiredPosition, mapBounds);

            if (TryCalculateSceneMapBounds(out Bounds detectedBounds))
            {
                mapBounds = detectedBounds;
                SyncLegacyBoundsFromMapBounds();
                Debug.Log("[TheHeroCameraFix] Map bounds detected: " + FormatBounds(mapBounds));
                return ClampToBounds(desiredPosition, mapBounds);
            }

            if (HasLegacyCenterBounds())
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, MinBounds.x, MaxBounds.x);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, MinBounds.y, MaxBounds.y);
                return desiredPosition;
            }

            if (!_warnedMissingBounds)
            {
                _warnedMissingBounds = true;
                Debug.LogWarning("[TheHeroCameraFix] Map bounds not found; camera follows Hero without clamp");
            }

            return desiredPosition;
        }

        public static bool TryCalculateSceneMapBounds(out Bounds bounds)
        {
            bool found = false;
            bounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
            {
                Bounds tilemapBounds = GetTilemapBounds(tilemap);
                if (tilemapBounds.size.sqrMagnitude <= 0.001f) continue;
                AddBounds(ref bounds, tilemapBounds, ref found);
            }

            foreach (var tile in Object.FindObjectsByType<THTile>(FindObjectsInactive.Exclude))
            {
                if (tile == null) continue;

                var renderer = tile.GetComponent<Renderer>();
                if (renderer != null && renderer.bounds.size.sqrMagnitude > 0.001f)
                {
                    AddBounds(ref bounds, renderer.bounds, ref found);
                }
                else
                {
                    AddBounds(ref bounds, new Bounds(tile.transform.position, Vector3.one), ref found);
                }
            }

            return found;
        }

        private static Bounds GetTilemapBounds(Tilemap tilemap)
        {
            var renderer = tilemap.GetComponent<Renderer>();
            if (renderer != null && renderer.bounds.size.sqrMagnitude > 0.001f)
                return renderer.bounds;

            BoundsInt cellBounds = tilemap.cellBounds;
            if (cellBounds.size == Vector3Int.zero)
                return new Bounds(Vector3.zero, Vector3.zero);

            Vector3 min = tilemap.CellToWorld(cellBounds.min);
            Vector3 max = tilemap.CellToWorld(cellBounds.max);
            return new Bounds((min + max) * 0.5f, new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 1f));
        }

        private static void AddBounds(ref Bounds bounds, Bounds next, ref bool found)
        {
            if (!found)
            {
                bounds = next;
                found = true;
                return;
            }

            bounds.Encapsulate(next);
        }

        private Vector3 ClampToBounds(Vector3 desiredPosition, Bounds bounds)
        {
            ConfigureCameraComponent();
            if (_camera == null || !_camera.orthographic)
                return desiredPosition;

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            float minX = bounds.min.x + halfWidth;
            float maxX = bounds.max.x - halfWidth;
            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight;

            desiredPosition.x = minX > maxX ? bounds.center.x : Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = minY > maxY ? bounds.center.y : Mathf.Clamp(desiredPosition.y, minY, maxY);
            desiredPosition.z = z;
            return desiredPosition;
        }

        private void ConfigureCameraComponent()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();

            if (_camera == null)
                return;

            _camera.orthographic = true;
            _camera.orthographicSize = 7.5f;
            transform.rotation = Quaternion.identity;
            Vector3 p = transform.position;
            p.z = z;
            transform.position = p;
        }

        private bool HasUsableMapBounds()
        {
            return mapBounds.size.x > 0.01f && mapBounds.size.y > 0.01f;
        }

        private bool HasLegacyCenterBounds()
        {
            return Vector2.Distance(MinBounds, MaxBounds) > 0.01f;
        }

        private void SyncLegacyBoundsFromMapBounds()
        {
            if (!HasUsableMapBounds())
                return;

            MinBounds = new Vector2(mapBounds.min.x, mapBounds.min.y);
            MaxBounds = new Vector2(mapBounds.max.x, mapBounds.max.y);
        }

        private static Transform FindBestHeroTransform()
        {
            var mover = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Exclude)
                .Where(m => IsHeroCandidate(m.gameObject))
                .OrderByDescending(m => m.name == "Hero")
                .FirstOrDefault();

            if (mover != null)
                return mover.transform;

            var heroComponent = Object.FindObjectsByType<THHero>(FindObjectsInactive.Exclude)
                .Where(h => IsHeroCandidate(h.gameObject))
                .OrderByDescending(h => h.name == "Hero")
                .FirstOrDefault();

            if (heroComponent != null)
                return heroComponent.transform;

            foreach (string name in new[] { "Hero", "Player", "PlayerHero", "THHero", "MapHero", "HeroMarker" })
            {
                GameObject go = GameObject.Find(name);
                if (go != null && IsHeroCandidate(go))
                    return go.transform;
            }

            return null;
        }

        private static bool IsHeroCandidate(GameObject go)
        {
            if (go == null || !go.activeInHierarchy)
                return false;

            if (go.GetComponentInParent<Canvas>() != null)
                return false;

            string lower = go.name.ToLowerInvariant();
            return lower == "hero" ||
                   lower == "player" ||
                   lower == "playerhero" ||
                   lower == "thhero" ||
                   lower == "maphero" ||
                   lower == "heromarker" ||
                   go.GetComponent<THStrictGridHeroMovement>() != null ||
                   go.GetComponent<THHero>() != null;
        }

        private static string FormatBounds(Bounds bounds)
        {
            return $"min=({bounds.min.x:0.##},{bounds.min.y:0.##}) max=({bounds.max.x:0.##},{bounds.max.y:0.##})";
        }
    }
}
