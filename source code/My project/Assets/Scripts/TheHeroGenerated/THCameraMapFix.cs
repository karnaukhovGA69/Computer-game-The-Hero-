using UnityEngine;
using System.Linq;

namespace TheHero.Generated
{
    public class THCameraMapFix : MonoBehaviour
    {
        public float margin = 1f;
        public float defaultOrthoSize = 7f;

        private void Start()
        {
            FixCamera();
        }

        [ContextMenu("Fix Camera Now")]
        public void FixCamera()
        {
            var tiles = GameObject.FindObjectsByType<THMapTile>(FindObjectsInactive.Include);
            if (tiles.Length == 0) return;

            float minX = tiles.Min(t => t.transform.position.x);
            float maxX = tiles.Max(t => t.transform.position.x);
            float minY = tiles.Min(t => t.transform.position.y);
            float maxY = tiles.Max(t => t.transform.position.y);

            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;

            transform.position = new Vector3(centerX, centerY, -10);

            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                // Calculate size to fit bounds
                float height = (maxY - minY + margin * 2) / 2f;
                float width = (maxX - minX + margin * 2) / 2f / cam.aspect;
                cam.orthographicSize = Mathf.Max(height, width, defaultOrthoSize);
            }
            
            Debug.Log($"[THCameraMapFix] Camera centered to {centerX}, {centerY}. Ortho size: {cam?.orthographicSize}");
        }

        public void CenterOnHero()
        {
            var hero = GameObject.Find("Hero");
            if (hero != null)
            {
                transform.position = new Vector3(hero.transform.position.x, hero.transform.position.y, -10);
                ClampCameraToMapBounds();
            }
        }

        public void ClampCameraToMapBounds()
        {
            // Implementation if smooth follow is used
        }
    }
}
