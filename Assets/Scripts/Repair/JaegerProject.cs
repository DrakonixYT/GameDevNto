using System;
using UnityEngine;
using HangarGame.Core;
using HangarGame.CameraControl;

namespace HangarGame.Repair
{
    public class JaegerProject : MonoBehaviour
    {
        public static JaegerProject Instance { get; private set; }
        public int CurrentStage { get; private set; } = 0;
        public int TotalStages => 4;
        public bool IsComplete => CurrentStage >= TotalStages;

        [SerializeField] private Transform[] stageVisualMarkers;
        [SerializeField] private GameObject platePrefab;

        private readonly string[] names = { "Термоядерный Реактор 'Гиперион'", "Титановый Силовой Экзоскелет", "Тяжелый Ионный Излучатель", "Нейро-Командный Шлем 'Апокалипсис'" };
        private readonly int[] credits = { 150, 200, 250, 300 }, alloys = { 60, 100, 120, 150 };

        private void Awake() => Instance = this;

        public void SetMarkers(Transform[] m) { stageVisualMarkers = m; UpdateMarkers(); }
        public void SetPlatePrefab(GameObject p) => platePrefab = p;

        public string GetNextStageInfo() => IsComplete ? "ПРОЕКТ 'ЕГЕРЬ': 100% (ПОСТРОЕН)" : $"ПРОЕКТ 'ЕГЕРЬ' ({CurrentStage}/4): {names[CurrentStage]} [{credits[CurrentStage]}¢, {alloys[CurrentStage]}т]";

        public bool CanStartNext() => !IsComplete && RepairWorkbench.Instance != null && !RepairWorkbench.Instance.HasItem && GameManager.Instance != null && GameManager.Instance.Credits >= credits[CurrentStage] && GameManager.Instance.Alloy >= alloys[CurrentStage];

        public void StartNextStage()
        {
            if (!CanStartNext() || !GameManager.Instance.TrySpend(credits[CurrentStage], alloys[CurrentStage])) return;
            RepairWorkbench.Instance.ReceiveJaegerModule(CreateStageModel(CurrentStage), names[CurrentStage], OnStageCompleted);
        }

        private void OnStageCompleted()
        {
            CurrentStage++; UpdateMarkers();
            HangarAudioLight.Instance?.PlayBolt(); RoomCamera.Instance?.Shake(0.35f, 0.15f);
            GameManager.Instance?.SetJaegerProgress(CurrentStage / (float)TotalStages);
            if (IsComplete) { HangarAudioLight.Instance?.PlayVictory(); GameManager.Instance?.TriggerVictory(); }
        }

        private void UpdateMarkers()
        {
            if (stageVisualMarkers == null) return;
            for (int i = 0; i < stageVisualMarkers.Length; i++)
            {
                if (stageVisualMarkers[i] != null) stageVisualMarkers[i].gameObject.SetActive(i < CurrentStage);
            }
        }

        private GameObject CreateStageModel(int s)
        {
            GameObject m;
            if (platePrefab != null)
            {
                m = new GameObject($"Jaeger_Plate_Mod_{s + 1}");
                GameObject sub = Instantiate(platePrefab, m.transform);
                sub.name = "Model";
                sub.transform.localPosition = new Vector3(5.19f, -1.46f, 0.40f);
                sub.transform.localRotation = Quaternion.identity;
                m.transform.localScale = Vector3.one * 0.7f;
                Color col = s == 0 ? Color.cyan : (s == 1 ? new Color(0.2f, 0.5f, 0.9f) : (s == 2 ? new Color(0.95f, 0.45f, 0.1f) : new Color(1f, 0.85f, 0.2f)));
                foreach (var r in m.GetComponentsInChildren<Renderer>())
                {
                    r.material.color = col;
                }
            }
            else
            {
                m = new GameObject($"Jaeger_Mod_{s + 1}");
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.SetParent(m.transform, false);
                c.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
            }
            return m;
        }
    }
}
