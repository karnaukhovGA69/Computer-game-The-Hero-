using UnityEngine;

namespace TheHero.Generated
{
    public class THGlowEffect : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Color _baseColor;
        public float speed = 2f;
        public float intensity = 0.2f;

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        private void Update()
        {
            if (_sr == null) return;
            float pulse = Mathf.PingPong(Time.time * speed, intensity);
            _sr.color = _baseColor + new Color(pulse, pulse, pulse, 0);
        }
    }
}
