using System.Collections;
using UnityEngine;

namespace HangarGame.CameraControl
{
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        public Vector3 CurrentOffset { get; private set; } = Vector3.zero;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            Instance = this;
        }

        public void Shake(float duration = 0.4f, float magnitude = 0.15f)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = (Random.value * 2f - 1f) * magnitude;
                float y = (Random.value * 2f - 1f) * magnitude;
                float z = (Random.value * 2f - 1f) * (magnitude * 0.5f);

                CurrentOffset = new Vector3(x, y, z);

                elapsed += Time.deltaTime;
                magnitude = Mathf.Lerp(magnitude, 0f, elapsed / duration);
                yield return null;
            }

            CurrentOffset = Vector3.zero;
            shakeCoroutine = null;
        }
    }
}
