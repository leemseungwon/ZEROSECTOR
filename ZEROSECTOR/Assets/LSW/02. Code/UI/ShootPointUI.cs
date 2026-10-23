using UnityEngine;
using UnityEngine.InputSystem;

namespace LSW._02._Code.UI
{
    public class ShootPointUI : MonoBehaviour
    {
        [Header("Scale Animation")]
        [SerializeField] private float startScale = 1f;
        [SerializeField] private float endScale = 0f;
        
        [Header("Laser")]
        [SerializeField] private LineRenderer laserLine;
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask laserLayers = ~0;

        private RectTransform _rectTransform;
        private Vector3 _initialScale;
        private float _duration;
        private float _elapsedTime;
        private bool _isShown;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialScale = _rectTransform.localScale;
            Hide();
        }

        private void Update()
        {
            if(!_isShown)
                return;
            
            if (Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                _rectTransform.position = mousePosition;

                Camera cam = Camera.main;

                if (cam != null && laserLine != null && firePoint != null)
                {
                    Vector3 mouseWorld = cam.ScreenToWorldPoint(
                        new Vector3(mousePosition.x, mousePosition.y, 
                            Mathf.Abs(cam.transform.position.z - firePoint.position.z)));

                    Vector2 start = firePoint.position;
                    Vector2 direction = ((Vector2)mouseWorld - start).normalized;

                    RaycastHit2D hit = Physics2D.Raycast(start,
                        direction, Mathf.Infinity, laserLayers);

                    laserLine.SetPosition(0, start);
                    laserLine.SetPosition(1, hit.point);
                }
            }

            _elapsedTime += Time.unscaledDeltaTime;

            float t = _duration > 0f
                ? Mathf.Clamp01(_elapsedTime / _duration)
                : 1f;

            float scale = Mathf.Lerp(startScale, endScale, t);
            _rectTransform.localScale = _initialScale * scale;

            if (t >= 1f)
                Hide();
        }

        public void Show(float duration)
        {
            _duration = duration;
            _elapsedTime = 0f;
            _rectTransform.localScale = _initialScale * startScale;
            _isShown = true;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _isShown = false;
        }
    }
}