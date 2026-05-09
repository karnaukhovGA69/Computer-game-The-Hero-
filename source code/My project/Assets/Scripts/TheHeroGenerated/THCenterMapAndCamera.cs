using UnityEngine;
using System.Linq;

namespace TheHero.Generated
{
    public class THCenterMapAndCamera : MonoBehaviour
    {
        public Transform mapRoot;
        public Camera mainCamera;
        public float padding = 1f;

        private void Start()
        {
            CenterNow();
        }

        [ContextMenu("Center Now")]
        public void CenterNow()
        {
            if (mapRoot == null) mapRoot = GameObject.Find("MapRoot")?.transform;
            if (mainCamera == null) mainCamera = Camera.main;

            if (mapRoot == null || mainCamera == null) return;

            // Find all tiles (children of mapRoot/Tiles or just all Tile_* objects)
            var tiles = mapRoot.GetComponentsInChildren<SpriteRenderer>()
                .Where(sr => sr.gameObject.name.StartsWith("Tile_"))
                .ToList();

            if (tiles.Count == 0) return;

            float minX = tiles.Min(t => t.transform.position.x);
            float maxX = tiles.Max(t => t.transform.position.x);
            float minY = tiles.Min(t => t.transform.position.y);
            float maxY = tiles.Max(t => t.transform.position.y);

            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;

            // Calculate offset to move center to (0,0)
            Vector3 offset = new Vector3(-centerX, -centerY, 0);

            // Move MapRoot so that its center is at (0,0)
            mapRoot.position += offset;

            // Align Hero if it's not a child of MapRoot
            var hero = GameObject.Find("Hero");
            if (hero != null && hero.transform.parent != mapRoot)
            {
                hero.transform.position += offset;
            }

            // Set camera to (0,0,-10)
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.orthographic = true;

            // Calculate ortho size
            float mapWidth = maxX - minX + padding * 2;
            float mapHeight = maxY - minY + padding * 2;

            float screenAspect = (float)Screen.width / Screen.height;
            float targetHalfHeight = mapHeight / 2f;
            float targetHalfWidth = mapWidth / (2f * screenAspect);

            mainCamera.orthographicSize = Mathf.Max(targetHalfHeight, targetHalfWidth);
            
            // Set background
            mainCamera.backgroundColor = new Color(0.05f, 0.15f, 0.05f); // Dark Green

            Debug.Log($"[THCenterMapAndCamera] Map centered. Offset: {offset}. Camera Size: {mainCamera.orthographicSize}");
        }
    }
}
