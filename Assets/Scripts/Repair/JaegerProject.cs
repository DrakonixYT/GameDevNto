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
        private readonly string[] names = { "Термоядерный Реактор 'Гиперион'", "Титановый Силовой Экзоскелет", "Тяжелый Ионный Излучатель", "Нейро-Командный Шлем 'Апокалипсис'" };
        private readonly int[] credits = { 150, 200, 250, 300 }, alloys = { 60, 100, 120, 150 };

        private void Awake() => Instance = this;

        public void SetMarkers(Transform[] m) { stageVisualMarkers = m; UpdateMarkers(); }

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
            for (int i = 0; i < stageVisualMarkers.Length; i++) if (stageVisualMarkers[i] != null) stageVisualMarkers[i].gameObject.SetActive(i < CurrentStage);
        }

        private GameObject CreateStageModel(int s)
        {
            GameObject m = new GameObject($"Jaeger_Mod_{s + 1}");
            if (s == 0) { var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); c.transform.SetParent(m.transform, false); c.transform.localScale = new Vector3(0.5f, 0.25f, 0.5f); c.GetComponent<Renderer>().material.color = Color.cyan; }
            else if (s == 1) { var c = GameObject.CreatePrimitive(PrimitiveType.Cube); c.transform.SetParent(m.transform, false); c.transform.localScale = new Vector3(0.7f, 0.4f, 0.4f); c.GetComponent<Renderer>().material.color = new Color(0.3f, 0.35f, 0.4f); }
            else if (s == 2) { var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); c.transform.SetParent(m.transform, false); c.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f); c.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); c.GetComponent<Renderer>().material.color = new Color(0.9f, 0.4f, 0.1f); }
            else { var c = GameObject.CreatePrimitive(PrimitiveType.Cube); c.transform.SetParent(m.transform, false); c.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f); c.GetComponent<Renderer>().material.color = new Color(1f, 0.8f, 0.1f); }
            return m;
        }
    }
}
