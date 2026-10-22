using System.Collections;
using UnityEngine;

namespace LSW._02._Code.Environments
{
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] private float openHeight = 4f;
        [SerializeField] private float openDuration = 0.5f;

        private Vector3 _closedPosition;
        private Coroutine _openCoroutine;

        private void Awake()
        {
            _closedPosition = transform.position;
        }

        public void OpenDoor()
        {
            if (_openCoroutine != null)
                StopCoroutine(_openCoroutine);

            _openCoroutine = StartCoroutine(OpenRoutine());
        }

        public void ResetDoor()
        {
            if (_openCoroutine != null)
                StopCoroutine(_openCoroutine);

            transform.position = _closedPosition;
        }

        private IEnumerator OpenRoutine()
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = _closedPosition + Vector3.up * openHeight;
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.position = targetPos;
        }
    }
}
