using UnityEngine;

namespace TheHero.Generated
{
    [RequireComponent(typeof(Camera))]
    public class THCameraClamp : MonoBehaviour
    {
        public THMapBounds mapBounds;
        private Camera _cam;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (mapBounds == null) mapBounds = Object.FindAnyObjectByType<THMapBounds>();
        }

        private void LateUpdate()
        {
            if (mapBounds == null || !mapBounds.initialized) return;

            float ortho = _cam.orthographicSize;
            float aspect = _cam.aspect;
            float camHeight = ortho;
            float camWidth = ortho * aspect;

            Vector3 pos = transform.position;

            // Clamp so we don't see past the edge of the map
            float leftLimit = mapBounds.minX + camWidth;
            float rightLimit = mapBounds.maxX - camWidth;
            float bottomLimit = mapBounds.minY + camHeight;
            float topLimit = mapBounds.maxY - camHeight;

            // If map is smaller than camera view, center it
            if (rightLimit < leftLimit) pos.x = (mapBounds.minX + mapBounds.maxX) / 2f;
            else pos.x = Mathf.Clamp(pos.x, leftLimit, rightLimit);

            if (topLimit < bottomLimit) pos.y = (mapBounds.minY + mapBounds.maxY) / 2f;
            else pos.y = Mathf.Clamp(pos.y, bottomLimit, topLimit);

            transform.position = pos;
        }
    }
}
