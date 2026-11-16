using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.CameraControl;

namespace HangarGame.Repair
{
    [System.Serializable]
    public class JaegerStageData
    {
        public string stageName;
        public string description;
        public int creditCost;
        public int alloyCost;
        public int miniGameType; // 0 = Wiring, 1 = Welding, 2 = Bolts
    }

    public class JaegerProjectManager : MonoBehaviour
    {
        public static JaegerProjectManager Instance { get; private set; }

        [Header("Stages Configuration")]
        [SerializeField] private List<JaegerStageData> stages = new List<JaegerStageData>();
        [SerializeField] private int currentStageIndex = 0;

        [Header("3D Jaeger Frame Model References")]
        [SerializeField] private GameObject jaegerChassisRoot;
        [SerializeField] private Transform[] stageVisualMarkers;

        public int CurrentStageIndex => currentStageIndex;
        public int TotalStages => stages != null && stages.Count > 0 ? stages.Count : 4;
        public float Progress01 => Mathf.Clamp01((float)currentStageIndex / TotalStages);
        public bool IsCompleted => currentStageIndex >= TotalStages;

        public JaegerStageData CurrentStage => (currentStageIndex < TotalStages) ? stages[currentStageIndex] : null;

        private void Awake()
        {
            Instance = this;
            InitializeDefaultStages();
        }

        private void OnEnable()
        {
            EventBus.OnJaegerProgressUpdated += HandleProgressUpdated;
        }

        private void OnDisable()
        {
            EventBus.OnJaegerProgressUpdated -= HandleProgressUpdated;
        }

        private void InitializeDefaultStages()
        {
            if (stages != null && stages.Count > 0) return;

            stages = new List<JaegerStageData>
            {
                new JaegerStageData {
                    stageName = "Термоядерный Реактор 'Гиперион'",
                    description = "Энергетическое сердце Егеря. Требуется калибровка магнитных ловушек и закрутка якорных болтов.",
                    creditCost = 150,
                    alloyCost = 60,
                    miniGameType = 0 // Wiring
                },
                new JaegerStageData {
                    stageName = "Титановый Силовой Экзоскелет",
                    description = "Несущий каркас позвоночника и торса. Требуется сварка титанового шва и закрутка фиксаторов.",
                    creditCost = 200,
                    alloyCost = 100,
                    miniGameType = 1 // Welding
                },
                new JaegerStageData {
                    stageName = "Тяжелый Ионный Излучатель",
                    description = "Осадное орудие правого плеча для пробития брони кайдзю 5-й категории.",
                    creditCost = 250,
                    alloyCost = 120,
                    miniGameType = 1 // Welding
                },
                new JaegerStageData {
                    stageName = "Нейро-Командный Шлем 'Апокалипсис'",
                    description = "Двухместная кабина дрифт-синхронизации пилотов с бронированным панорамным забралом.",
                    creditCost = 300,
                    alloyCost = 150,
                    miniGameType = 0 // Wiring
                }
            };
        }

        public void SetStageVisualMarkers(Transform[] markers, GameObject chassisRoot = null)
        {
            stageVisualMarkers = markers;
            if (chassisRoot != null) jaegerChassisRoot = chassisRoot;
            UpdateVisualMarkers();
        }

        public bool CanStartNextStage()
        {
            if (IsCompleted) return false;
            if (RepairWorkbench.Instance != null && RepairWorkbench.Instance.HasPart) return false;

            JaegerStageData stage = CurrentStage;
            if (stage == null || GameManager.Instance == null) return false;

            return GameManager.Instance.Credits >= stage.creditCost && GameManager.Instance.Alloy >= stage.alloyCost;
        }

        public bool StartAssemblingCurrentStage()
        {
            if (!CanStartNextStage())
            {
                Debug.LogWarning("[JaegerProjectManager] Нельзя начать сборку: недостаточно ресурсов или верстак занят!");
                return false;
            }

            JaegerStageData stage = CurrentStage;
            if (!GameManager.Instance.TrySpendResources(stage.creditCost, stage.alloyCost))
            {
                return false;
            }

            // Создаем 3D модель модуля на верстаке
            GameObject moduleObj = Create3DWorkbenchModule(currentStageIndex);
            RepairWorkbench.Instance.ReceiveJaegerModule(moduleObj, stage.stageName, stage.miniGameType, OnModuleAssemblyFinished);

            return true;
        }

        // Для обратной совместимости или быстрой сборки
        public void InvestInJaeger()
        {
            if (CanStartNextStage())
            {
                StartAssemblingCurrentStage();
            }
            else if (!IsCompleted && GameManager.Instance != null)
            {
                JaegerStageData stage = CurrentStage;
                int c = stage != null ? stage.creditCost : 200;
                int a = stage != null ? stage.alloyCost : 100;
                Debug.LogWarning($"[JaegerProjectManager] Недостаточно ресурсов для сборки: требуется {c} кредитов и {a} сплава!");
            }
        }

        private void OnModuleAssemblyFinished()
        {
            int completedIndex = currentStageIndex;
            currentStageIndex++;

            Debug.Log($"<color=green><b>[ПРОЕКТ 'ЕГЕРЬ']</b> Модуль '{stages[completedIndex].stageName}' успешно собран и смонтирован на шасси!</color>");

            // Звук гидропривода и сотрясение
            HangarAudio.Instance?.PlayBolt();
            CameraShaker.Instance?.Shake(0.4f, 0.2f);

            UpdateVisualMarkers();

            float progress = Progress01;
            EventBus.TriggerJaegerProgressUpdated(progress);

            if (currentStageIndex >= TotalStages)
            {
                Debug.Log("<color=yellow><b>[ТРИУМФ]</b> Сборка гигантского Егеря полностью завершена! Земля спасена!</color>");
                EventBus.TriggerGameWon();
            }
        }

        private void HandleProgressUpdated(float progress01)
        {
            UpdateVisualMarkers();
        }

        private void UpdateVisualMarkers()
        {
            if (stageVisualMarkers == null) return;

            for (int i = 0; i < stageVisualMarkers.Length; i++)
            {
                if (stageVisualMarkers[i] != null)
                {
                    stageVisualMarkers[i].gameObject.SetActive(i < currentStageIndex);
                }
            }
        }

        private GameObject Create3DWorkbenchModule(int stageIndex)
        {
            GameObject root = new GameObject($"Jaeger_Module_Stage_{stageIndex + 1}");

            switch (stageIndex)
            {
                case 0: // Реактор 'Гиперион'
                    // Ядро реактора (светящийся цилиндр)
                    GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    core.name = "Reactor_Core_Cylinder";
                    core.transform.localScale = new Vector3(0.5f, 0.25f, 0.5f);
                    core.transform.SetParent(root.transform, false);
                    SetColor(core, new Color(0.1f, 0.9f, 1f)); // Неоновый циан

                    // Защитные кольца
                    GameObject ring1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ring1.name = "Containment_Ring_1";
                    ring1.transform.localScale = new Vector3(0.65f, 0.05f, 0.65f);
                    ring1.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                    ring1.transform.SetParent(root.transform, false);
                    SetColor(ring1, new Color(0.2f, 0.25f, 0.3f));

                    GameObject ring2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ring2.name = "Containment_Ring_2";
                    ring2.transform.localScale = new Vector3(0.65f, 0.05f, 0.65f);
                    ring2.transform.localPosition = new Vector3(0f, -0.12f, 0f);
                    ring2.transform.SetParent(root.transform, false);
                    SetColor(ring2, new Color(0.2f, 0.25f, 0.3f));
                    break;

                case 1: // Торсовая Рама и Экзоскелет
                    GameObject spinePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    spinePlate.name = "Titanium_Spine_Plate";
                    spinePlate.transform.localScale = new Vector3(0.7f, 0.5f, 0.35f);
                    spinePlate.transform.SetParent(root.transform, false);
                    SetColor(spinePlate, new Color(0.3f, 0.35f, 0.4f));

                    GameObject pistonL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pistonL.transform.localScale = new Vector3(0.08f, 0.3f, 0.08f);
                    pistonL.transform.localPosition = new Vector3(-0.25f, 0f, 0.15f);
                    pistonL.transform.SetParent(root.transform, false);
                    SetColor(pistonL, new Color(0.8f, 0.7f, 0.2f));

                    GameObject pistonR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pistonR.transform.localScale = new Vector3(0.08f, 0.3f, 0.08f);
                    pistonR.transform.localPosition = new Vector3(0.25f, 0f, 0.15f);
                    pistonR.transform.SetParent(root.transform, false);
                    SetColor(pistonR, new Color(0.8f, 0.7f, 0.2f));
                    break;

                case 2: // Ионная Пушка
                    GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    barrel.name = "Ion_Cannon_Barrel";
                    barrel.transform.localScale = new Vector3(0.25f, 0.8f, 0.25f);
                    barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    barrel.transform.SetParent(root.transform, false);
                    SetColor(barrel, new Color(0.2f, 0.22f, 0.25f));

                    GameObject capacitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    capacitor.name = "Plasma_Capacitor";
                    capacitor.transform.localScale = new Vector3(0.45f, 0.35f, 0.45f);
                    capacitor.transform.localPosition = new Vector3(0f, 0f, -0.3f);
                    capacitor.transform.SetParent(root.transform, false);
                    SetColor(capacitor, new Color(0.9f, 0.45f, 0.1f)); // Оранжевый накопитель
                    break;

                case 3: // Нейро-Шлем
                    GameObject helm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    helm.name = "Neural_Helm_Cockpit";
                    helm.transform.localScale = new Vector3(0.55f, 0.4f, 0.5f);
                    helm.transform.SetParent(root.transform, false);
                    SetColor(helm, new Color(0.25f, 0.3f, 0.35f));

                    GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visor.name = "Visor_Panoramic";
                    visor.transform.localScale = new Vector3(0.45f, 0.12f, 0.15f);
                    visor.transform.localPosition = new Vector3(0f, 0.05f, 0.24f);
                    visor.transform.SetParent(root.transform, false);
                    SetColor(visor, new Color(1f, 0.85f, 0.1f)); // Золотистое забрало
                    break;
            }

            return root;
        }

        private void SetColor(GameObject obj, Color color)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color;
            }
        }
    }
}
