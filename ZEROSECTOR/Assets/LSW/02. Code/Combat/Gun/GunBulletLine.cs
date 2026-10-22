using System.Collections;
using LSW._02._Code.System;
using LSW._02._Code.System.PoolSystems;
using UnityEngine;

namespace LSW._02._Code.Combat.Gun
{
    [RequireComponent(typeof(LineRenderer))]
    public class GunBulletLine : MonoBehaviour, IPoolable
    {
        [Header("Pool Settings")] 
        [SerializeField] private string poolName;
        
        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 1f;

        private PoolSystem _poolSystem;
        private LineRenderer _lineRenderer;
        private Coroutine _fadeCoroutine;
        
        private Color _initialStartColor;
        private Color _initialEndColor;
        
        public void Initialize()
        {
            _lineRenderer = GetComponent<LineRenderer>();

            _initialStartColor = _lineRenderer.startColor;
            _initialEndColor = _lineRenderer.endColor;

            SystemHandler.Instance.GetSystem(out _poolSystem);
        }

        public void Spawn() { }
        
        public void ShowLine(Vector2 start, Vector2 end)
        {
            _lineRenderer.startColor = _initialStartColor;
            _lineRenderer.endColor = _initialEndColor;
            
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);

            _lineRenderer.enabled = true;

            _fadeCoroutine = StartCoroutine(FadeOut());
        }
        
        private IEnumerator FadeOut()
        {
            float elapsed = 0f;

            Color startColor = _lineRenderer.startColor;
            Color endColor = _lineRenderer.endColor;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;

                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);

                startColor.a = alpha;
                endColor.a = alpha;

                _lineRenderer.startColor = startColor;
                _lineRenderer.endColor = endColor;

                yield return null;
            }

            startColor.a = 0f;
            endColor.a = 0f;

            _lineRenderer.startColor = startColor;
            _lineRenderer.endColor = endColor;

            _lineRenderer.enabled = false;
            _fadeCoroutine = null;
            
            _poolSystem.DespawnPool(poolName, this);
        }
        
        public void Despawn() { }
    }
}