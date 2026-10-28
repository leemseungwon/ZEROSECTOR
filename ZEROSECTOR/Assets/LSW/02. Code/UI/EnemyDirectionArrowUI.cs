
using LSW._02._Code.System.PoolSystems;
using UnityEngine;
using UnityEngine.UI;

namespace LSW._02._Code.UI
{
    public class EnemyDirectionArrowUI : MonoBehaviour, IPoolable
    {
        [Header("Arrow")]
        [SerializeField, Min(0f)] private float screenPadding = 50f;
        [SerializeField] private Image arrowImage;

        private RectTransform _rectTransform;
        private Vector3 _initialLocalScale;
        private Canvas _canvas;
        private Camera _targetCamera;
        private Transform _target;

        public void Initialize()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialLocalScale = _rectTransform.localScale;

            if (arrowImage == null)
                arrowImage = GetComponent<Image>();
        }

        public void Spawn()
        {
            // PoolSystem reparents pooled objects. Restore the prefab scale so
            // parent scale changes cannot accumulate across room activations.
            if (_rectTransform != null)
                _rectTransform.localScale = _initialLocalScale;

            if (arrowImage != null)
                arrowImage.enabled = true;
        }

        public void Initialize(Transform target, Canvas canvas,
            Camera targetCamera)
        {
            if (_rectTransform == null)
                Initialize();

            _target = target;
            _canvas = canvas;
            _targetCamera = targetCamera;

            if (canvas != null)
            {
                transform.SetParent(canvas.transform, false);
            }

            // SetParent(false) preserves the existing local values. Reapply the
            // authored scale after changing from the pool hierarchy to the UI.
            _rectTransform.localScale = _initialLocalScale;
            
            if (arrowImage != null)
                arrowImage.enabled = true;
        }

        private void LateUpdate()
        {
            if (_target == null || _canvas == null || _targetCamera == null)
            {
                HideArrow();
                return;
            }

            Vector3 viewport =
                _targetCamera.WorldToViewportPoint(_target.position);

            bool isVisible = viewport is { z: > 0f, x: >= 0f and <= 1f, y: >= 0f and <= 1f };

            if (isVisible)
            {
                HideArrow();
                return;
            }

            if (arrowImage != null)
                arrowImage.enabled = true;
            
            Vector2 screenCenter = new(
                Screen.width * 0.5f,
                Screen.height * 0.5f
            );

            Vector3 screenPosition =
                _targetCamera.WorldToScreenPoint(_target.position);

            Vector2 direction = (Vector2)screenPosition - screenCenter;
            
            if (viewport.z < 0f)
                direction = -direction;

            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.up;

            direction.Normalize();
            
            float halfWidth = Mathf.Max(0f, Screen.width * 0.5f - screenPadding);
            float halfHeight = Mathf.Max(0f, Screen.height * 0.5f - screenPadding);

            float scaleX = halfWidth / Mathf.Max(Mathf.Abs(direction.x), 0.0001f);
            float scaleY = halfHeight / Mathf.Max(Mathf.Abs(direction.y), 0.0001f);
            float distance = Mathf.Min(scaleX, scaleY);

            Vector2 arrowScreenPosition = screenCenter + direction * distance;
            
            Camera canvasCamera =
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : _canvas.worldCamera;

            RectTransform parentRect = _rectTransform.parent as RectTransform;

            if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, arrowScreenPosition, canvasCamera, out Vector2 localPoint))
            {
                _rectTransform.anchoredPosition = localPoint;
            }
            
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            _rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, angle);
        }

        private void HideArrow()
        {
            if (arrowImage != null)
                arrowImage.enabled = false;
        }

        public void Despawn()
        {
            _target = null;
            _canvas = null;
            _targetCamera = null;

            HideArrow();
        }
    }
}
