using UnityEngine;
using System.Linq;

namespace TheHero.Generated
{
    public class THMapBounds : MonoBehaviour
    {
        public float minX, maxX, minY, maxY;
        public bool initialized = false;

        public void CalculateBounds()
        {
            var tiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            if (tiles.Length == 0) return;

            minX = tiles.Min(t => t.transform.position.x);
            maxX = tiles.Max(t => t.transform.position.x);
            minY = tiles.Min(t => t.transform.position.y);
            maxY = tiles.Max(t => t.transform.position.y);
            initialized = true;
            
            Debug.Log($"[THMapBounds] Calculated: X({minX} to {maxX}), Y({minY} to {maxY})");
        }

        private void Start()
        {
            if (!initialized) CalculateBounds();
        }

        public Vector3 ClampPosition(Vector3 pos)
        {
            if (!initialized) return pos;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            return pos;
        }
    }
}
