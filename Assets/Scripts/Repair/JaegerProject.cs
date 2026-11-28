using System.Collections;
using HangarGame.CameraControl;
using HangarGame.Core;
using UnityEngine;

namespace HangarGame.Repair
{
    public class JaegerProject : MonoBehaviour
    {
        public static JaegerProject Instance { get; private set; }

        [Header("Этапы Егеря: Голова, Л.Рука, П.Рука, Щит, Копьё")]
        [SerializeField] private Transform[] stageVisualParts = new Transform[5];
        [SerializeField] private GameObject[] stageWorkbenchPrefabs = new GameObject[5];
        [SerializeField] private GameObject platePrefab;

        [Header("Стоимость и названия")]
        [SerializeField] private string[] stageNames =
        {
            "Голова",
            "Левая рука",
            "Правая рука",
            "Щит",
            "Копьё"
        };
        [SerializeField] private int[] stageCredits = { 160, 210, 260, 330, 420 };
        [SerializeField] private int[] stageAlloy = { 70, 95, 120, 150, 190 };

        public int CurrentStage { get; private set; }
        public int TotalStages => 5;
        public bool IsComplete => CurrentStage >= TotalStages;

        public string GetStageName(int idx) => (stageNames != null && idx >= 0 && idx < stageNames.Length) ? stageNames[idx] : $"Этап {idx + 1}";
        public int GetStageCredits(int idx) => (stageCredits != null && idx >= 0 && idx < stageCredits.Length) ? stageCredits[idx] : 0;
        public int GetStageAlloy(int idx) => (stageAlloy != null && idx >= 0 && idx < stageAlloy.Length) ? stageAlloy[idx] : 0;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            RefreshVisualStages();
            if (RepairWorkbench.Instance != null && platePrefab != null)
                RepairWorkbench.Instance.SetPlatePrefab(platePrefab);
        }

        public void Configure(Transform[] sceneParts, GameObject[] workbenchPrefabs, GameObject plate)
        {
            stageVisualParts = sceneParts;
            stageWorkbenchPrefabs = workbenchPrefabs;
            platePrefab = plate;
            RefreshVisualStages();
        }

        public string GetNextStageInfo()
        {
            if (IsComplete)
                return "Егерь собран (5/5)";

            return $"Егерь {CurrentStage}/{TotalStages}: {stageNames[CurrentStage]} ({stageCredits[CurrentStage]} кр / {stageAlloy[CurrentStage]} м)";
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

            if (platePrefab != null)
                RepairWorkbench.Instance?.SetPlatePrefab(platePrefab);

            GameObject model = CreateWorkbenchModel(CurrentStage);
            RepairWorkbench.Instance?.ReceiveJaegerModule(model, stageNames[CurrentStage], OnStageCompleted);
        }

        private void OnStageCompleted()
        {
            int completedIndex = CurrentStage;
            CurrentStage++;

            if (stageVisualParts != null && completedIndex < stageVisualParts.Length && stageVisualParts[completedIndex] != null)
            {
                var part = stageVisualParts[completedIndex].gameObject;
                part.SetActive(true);
                if (Application.isPlaying) StartCoroutine(AnimateStageMount(part.transform));
            }

            HangarAudio.Instance?.PlayJaegerStage();
            RoomCamera.Instance?.Shake(0.32f, 0.14f);
            GameManager.Instance?.SetJaegerProgress(CurrentStage / (float)TotalStages);

            if (IsComplete)
            {
                HangarAudio.Instance?.PlayVictory();
                GameManager.Instance?.TriggerVictory();
            }
        }

        private void RefreshVisualStages()
        {
            if (stageVisualParts == null) return;
            for (int i = 0; i < stageVisualParts.Length; i++)
            {
                if (stageVisualParts[i] != null)
                    stageVisualParts[i].gameObject.SetActive(i < CurrentStage);
            }
        }

        private IEnumerator AnimateStageMount(Transform part)
        {
            Vector3 targetScale = part.localScale;
            part.localScale = targetScale * 0.1f;

            float t = 0f;
            const float dur = 0.4f;
            while (t < dur && part != null)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                part.localScale = Vector3.Lerp(targetScale * 0.1f, targetScale, k);
                yield return null;
            }
            if (part != null) part.localScale = targetScale;
        }

        private GameObject CreateWorkbenchModel(int stageIndex)
        {
            GameObject prefab = (stageWorkbenchPrefabs != null && stageIndex < stageWorkbenchPrefabs.Length)
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
