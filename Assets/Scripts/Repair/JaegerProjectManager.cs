using UnityEngine;
using HangarGame.Core;

namespace HangarGame.Repair
{
    public class JaegerProjectManager : MonoBehaviour
    {
        public static JaegerProjectManager Instance { get; private set; }

        [Header("Project Jaeger Stages")]
        [SerializeField] private GameObject jaegerHologramOrModel;
        [SerializeField] private Transform[] stageVisualMarkers;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnJaegerProgressUpdated += HandleProgressUpdated;
        }

        private void OnDisable()
        {
            EventBus.OnJaegerProgressUpdated -= HandleProgressUpdated;
        }

        public void InvestInJaeger()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.InvestInJaeger(0.25f, 300, 150);
            }
        }

        private void HandleProgressUpdated(float progress01)
        {
            Debug.Log($"[JaegerProject] Прогресс сборки Егеря: {progress01 * 100f:F0}%");

            if (stageVisualMarkers != null)
            {
                for (int i = 0; i < stageVisualMarkers.Length; i++)
                {
                    if (stageVisualMarkers[i] != null)
                    {
                        float stageThreshold = (i + 1) / (float)stageVisualMarkers.Length;
                        stageVisualMarkers[i].gameObject.SetActive(progress01 >= stageThreshold);
                    }
                }
            }
        }
    }
}
