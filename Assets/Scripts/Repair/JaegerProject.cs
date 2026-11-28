using System.Collections;
using HangarGame.CameraControl;
using HangarGame.Core;
using UnityEngine;

namespace HangarGame.Repair
{
    public class JaegerProject : MonoBehaviour
    {
        public static JaegerProject Instance { get; private set; }

        public Transform[] stageVisualParts = new Transform[5];
        public GameObject[] stageWorkbenchPrefabs = new GameObject[5];
        public GameObject platePrefab;

        public string[] stageNames = { "Голова", "Левая рука", "Правая рука", "Щит", "Копьё" };
        public int[] stageCredits = { 160, 210, 260, 330, 420 };
        public int[] stageAlloy = { 70, 95, 120, 150, 190 };

        public int CurrentStage { get; private set; }
        public int TotalStages => 5;
        public bool IsComplete => CurrentStage >= TotalStages;

        private void Awake() => Instance = this;

        private void Start()
        {
            RefreshVisualStages();
            if (RepairWorkbench.Instance != null && platePrefab != null)
                RepairWorkbench.Instance.platePrefab = platePrefab;
        }

        public string GetStageName(int idx) => (stageNames != null && idx >= 0 && idx < stageNames.Length) ? stageNames[idx] : $"Этап {idx + 1}";
        public int GetStageCredits(int idx) => (stageCredits != null && idx >= 0 && idx < stageCredits.Length) ? stageCredits[idx] : 0;
        public int GetStageAlloy(int idx) => (stageAlloy != null && idx >= 0 && idx < stageAlloy.Length) ? stageAlloy[idx] : 0;

        public void RefreshVisualStages()
        {
            if (stageVisualParts == null) return;
            for (int i = 0; i < stageVisualParts.Length; i++)
                if (stageVisualParts[i] != null) stageVisualParts[i].gameObject.SetActive(i < CurrentStage);
        }

        public bool CanStartNext()
        {
            return !IsComplete
                && RepairWorkbench.Instance != null
                && !RepairWorkbench.Instance.HasItem
                && GameManager.Instance != null
                && GameManager.Instance.CanAfford(stageCredits[CurrentStage], stageAlloy[CurrentStage]);
        }

        public void StartNextStage()
        {
            if (!CanStartNext()) return;
            if (!GameManager.Instance.TrySpend(stageCredits[CurrentStage], stageAlloy[CurrentStage])) return;

            if (platePrefab != null && RepairWorkbench.Instance != null)
                RepairWorkbench.Instance.platePrefab = platePrefab;

            GameObject model = CreateWorkbenchModel(CurrentStage);
            RepairWorkbench.Instance?.ReceiveJaegerModule(model, stageNames[CurrentStage], OnStageCompleted);
        }

        private void OnStageCompleted()
        {
            int doneIdx = CurrentStage++;
            if (stageVisualParts != null && doneIdx < stageVisualParts.Length && stageVisualParts[doneIdx] != null)
            {
                var part = stageVisualParts[doneIdx];
                part.gameObject.SetActive(true);
                if (Application.isPlaying) StartCoroutine(AnimateStageMount(part));
            }

            HangarAudio.Play(Sfx.JaegerStage);
            RoomCamera.Instance?.Shake(0.32f, 0.14f);
            GameManager.Instance?.SetJaegerProgress(CurrentStage / (float)TotalStages);

            if (IsComplete)
            {
                HangarAudio.Play(Sfx.Victory);
                GameManager.Instance?.TriggerVictory();
            }
        }

        private static IEnumerator AnimateStageMount(Transform part)
        {
            Vector3 targetScale = part.localScale;
            part.localScale = targetScale * 0.1f;

            float t = 0f;
            const float dur = 0.4f;
            while (t < dur && part != null)
            {
                t += Time.deltaTime;
                part.localScale = Vector3.Lerp(targetScale * 0.1f, targetScale, Mathf.SmoothStep(0f, 1f, t / dur));
                yield return null;
            }
            if (part != null) part.localScale = targetScale;
        }

        private GameObject CreateWorkbenchModel(int stageIndex)
        {
            GameObject prefab = stageWorkbenchPrefabs != null && stageIndex < stageWorkbenchPrefabs.Length
                ? stageWorkbenchPrefabs[stageIndex]
                : null;

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                instance.name = $"Jaeger_Work_{stageIndex}";
                return instance;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = $"Jaeger_Work_{stageIndex}";
            fallback.transform.localScale = new Vector3(0.45f, 0.35f, 0.45f);
            return fallback;
        }
    }
}
