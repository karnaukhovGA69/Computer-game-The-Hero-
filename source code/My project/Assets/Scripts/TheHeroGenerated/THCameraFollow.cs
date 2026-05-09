using UnityEngine;

namespace TheHero.Generated
{
    public class THCameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float SmoothSpeed = 5f;
        public Vector2 MinBounds;
        public Vector2 MaxBounds;

        private void LateUpdate()
        {
            if (Target == null) return;
            Vector3 desiredPosition = new Vector3(Target.position.x, Target.position.y, transform.position.z);
            
            // Clamp to bounds
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, MinBounds.x, MaxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, MinBounds.y, MaxBounds.y);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, SmoothSpeed * Time.deltaTime);
        }
    }
}