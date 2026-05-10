using UnityEngine;

namespace TheHero.Generated
{
    public class THHeroVisuals : MonoBehaviour
    {
        public Sprite idleSprite;
        public Sprite move01;
        public Sprite move02;
        
        public float idleSwaySpeed = 4f;
        public float idleSwayAmount = 0.05f;
        public float moveAnimSpeed = 0.2f;

        private SpriteRenderer _sr;
        private THStrictGridHeroMovement _movement;
        private float _timer;
        private int _moveFrame;
        private float _moveTimer;
        private Vector3 _initialScale;

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            _movement = GetComponent<THStrictGridHeroMovement>();
            _initialScale = transform.localScale;
            
            if (_sr != null)
            {
                _sr.sortingOrder = 50;
            }
        }

        private void Update()
        {
            if (_sr == null || _movement == null) return;

            if (_movement.isMoving)
            {
                // Movement animation
                _moveTimer += Time.deltaTime;
                if (_moveTimer >= moveAnimSpeed)
                {
                    _moveTimer = 0;
                    _moveFrame = (_moveFrame + 1) % 2;
                    _sr.sprite = _moveFrame == 0 ? move01 : move02;
                }
                
                // Reset scale to normal when moving
                transform.localScale = _initialScale;
            }
            else
            {
                // Idle animation
                _sr.sprite = idleSprite;
                float sway = 1f + Mathf.Sin(Time.time * idleSwaySpeed) * idleSwayAmount;
                transform.localScale = new Vector3(_initialScale.x, _initialScale.y * sway, _initialScale.z);
                _moveTimer = 0;
            }
        }
    }
}
