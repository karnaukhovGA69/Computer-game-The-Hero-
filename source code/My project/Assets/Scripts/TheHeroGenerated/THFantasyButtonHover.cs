using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace TheHero.Generated
{
    public class THFantasyButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private Vector3 _originalScale;
        private Coroutine _currentAnim;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StopAnim();
            _currentAnim = StartCoroutine(ScaleTo(_originalScale * 1.03f, 0.1f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StopAnim();
            _currentAnim = StartCoroutine(ScaleTo(_originalScale, 0.1f));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            StopAnim();
            StartCoroutine(ClickAnim());
        }

        private void StopAnim()
        {
            if (_currentAnim != null) StopCoroutine(_currentAnim);
        }

        private IEnumerator ScaleTo(Vector3 target, float duration)
        {
            Vector3 start = transform.localScale;
            float elapsed = 0;
            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(start, target, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            transform.localScale = target;
        }

        private IEnumerator ClickAnim()
        {
            yield return ScaleTo(_originalScale * 0.97f, 0.05f);
            yield return ScaleTo(_originalScale * 1.03f, 0.05f);
        }

        private void OnDisable()
        {
            transform.localScale = _originalScale;
        }
    }
}
