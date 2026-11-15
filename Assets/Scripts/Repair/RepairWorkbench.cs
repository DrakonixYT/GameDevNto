using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Data;
using HangarGame.CameraControl;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Repair
{
    public class RepairWorkbench : MonoBehaviour
    {
        public static RepairWorkbench Instance { get; private set; }

        [Header("Workbench Mount")]
        [SerializeField] private Transform partMountPoint;
        [SerializeField] private float tableSurfaceY = 0.82f;

        [Header("Mini-games")]
        [SerializeField] private BoltMiniGame boltGame;
        [SerializeField] private WeldingMiniGame weldingGame;
        [SerializeField] private WiringMiniGame wiringGame;

        private MechPartInstance currentPart;
        private Vector3 originalPartPosition;
        private Quaternion originalPartRotation;
        private Transform originalPartParent;
        private bool originalPartKinematic = true;

        public MechPartInstance CurrentPart => currentPart;
        public bool HasPart => currentPart != null;
        public Transform MountPoint => partMountPoint;

        private void Awake()
        {
            Instance = this;

            if (boltGame == null) boltGame = GetComponentInChildren<BoltMiniGame>(true);
            if (weldingGame == null) weldingGame = GetComponentInChildren<WeldingMiniGame>(true);
            if (wiringGame == null) wiringGame = GetComponentInChildren<WiringMiniGame>(true);

            if (partMountPoint == null)
            {
                Transform existing = transform.Find("Part_Mount_Point");
                if (existing != null)
                {
                    partMountPoint = existing;
                }
                else
                {
                    GameObject mountObj = new GameObject("Part_Mount_Point");
                    mountObj.transform.position = new Vector3(transform.position.x, tableSurfaceY, transform.position.z);
                    mountObj.transform.SetParent(transform);
                    partMountPoint = mountObj.transform;
                }
            }
        }

        private void Start()
        {
            if (boltGame != null) boltGame.OnAllBoltsCompleted += HandleBoltsDone;
            if (weldingGame != null) weldingGame.OnWeldingCompleted += HandleWeldingDone;
            if (wiringGame != null) wiringGame.OnWiringCompleted += HandleWiringDone;
        }

        private void OnDestroy()
        {
            if (boltGame != null) boltGame.OnAllBoltsCompleted -= HandleBoltsDone;
            if (weldingGame != null) weldingGame.OnWeldingCompleted -= HandleWeldingDone;
            if (wiringGame != null) wiringGame.OnWiringCompleted -= HandleWiringDone;
        }

        private void Update()
        {
            if (currentPart == null) return;

            bool esc = false;
            bool space = false;
            bool swapKey = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                esc = Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame;
                space = Keyboard.current.spaceKey.wasPressedThisFrame;
                swapKey = Keyboard.current.tKey.wasPressedThisFrame;
            }
#else
            esc = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
            space = Input.GetKeyDown(KeyCode.Space);
            swapKey = Input.GetKeyDown(KeyCode.T);
#endif

            if (esc)
            {
                ReturnPartToMech();
            }
            else if (space)
            {
                FinishRepair();
            }
            else if (swapKey)
            {
                CycleNextModuleForCurrentPart();
            }
        }

        public void ReceivePartForRepair(MechPartInstance part)
        {
            if (currentPart != null)
            {
                Debug.LogWarning("[RepairWorkbench] На верстаке уже лежит деталь!");
                return;
            }

            currentPart = part;
            originalPartPosition = part.transform.position;
            originalPartRotation = part.transform.rotation;
            originalPartParent = part.transform.parent;

            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb != null)
            {
                originalPartKinematic = rb.isKinematic;
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }

            Collider col = part.GetComponent<Collider>();
            float halfHeight = 0.25f;
            if (col != null)
            {
                halfHeight = col.bounds.extents.y;
            }

            Vector3 tableCenter = partMountPoint != null ? partMountPoint.position : new Vector3(0f, tableSurfaceY, 2.2f);
            
            if (part.Slot == PartSlot.Legs)
            {
                part.transform.rotation = Quaternion.Euler(0f, 90f, 90f);
                part.transform.position = new Vector3(tableCenter.x, tableSurfaceY + 0.25f, tableCenter.z);
            }
            else if (part.Slot == PartSlot.RightArm)
            {
                part.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                part.transform.position = new Vector3(tableCenter.x, tableSurfaceY + 0.25f, tableCenter.z);
            }
            else
            {
                part.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                part.transform.position = new Vector3(tableCenter.x, tableSurfaceY + halfHeight + 0.05f, tableCenter.z);
            }

            GameManager.Instance?.SwitchRoomView(RoomView.WorkbenchFocus);

            Vector3 miniGameCenter = new Vector3(tableCenter.x, tableSurfaceY + 0.35f, tableCenter.z);

            // Разнообразие мини-игр в зависимости от слота и урона
            if (part.Slot == PartSlot.Head && wiringGame != null)
            {
                wiringGame.StartMiniGameAt(miniGameCenter);
            }
            else if (part.DurabilityPercent < 0.5f && weldingGame != null)
            {
                weldingGame.StartMiniGameAt(miniGameCenter);
            }
            else if (boltGame != null)
            {
                boltGame.StartMiniGameAt(miniGameCenter);
            }

            EventBus.TriggerPartSentToWorkbench(part);
            Debug.Log($"<color=cyan>[RepairWorkbench]</color> Деталь {part.name} на верстаке. (Пробел - ремонт, T - сменить модуль, Esc - выход).");
        }

        public void CycleNextModuleForCurrentPart()
        {
            if (currentPart == null) return;

            List<MechPartData> availableModules = ModuleCatalog.GetModulesForSlot(currentPart.Slot);
            if (availableModules.Count <= 1)
            {
                Debug.LogWarning("[RepairWorkbench] Для этого слота нет других доступных модулей!");
                return;
            }

            int currentIndex = -1;
            for (int i = 0; i < availableModules.Count; i++)
            {
                if (currentPart.Data != null && currentPart.Data.partId == availableModules[i].partId)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % availableModules.Count;
            MechPartData nextModule = availableModules[nextIndex];

            // Устанавливаем новый модуль
            currentPart.Initialize(nextModule, currentPart.Slot, 100f);

            HangarAudio.Instance?.PlayBolt();
            CameraShaker.Instance?.Shake(0.2f, 0.08f);

            Debug.Log($"<color=green>[КАСТОМИЗАЦИЯ]</color> Установлен новый модуль: <b>{nextModule.partName}</b>! Урон: {nextModule.attackType} ({nextModule.attackPower}), Защита: {nextModule.resistanceType} ({nextModule.defensePower})");
        }

        private void HandleWeldingDone()
        {
            Vector3 center = partMountPoint != null ? partMountPoint.position : new Vector3(0f, tableSurfaceY, 2.2f);
            if (boltGame != null)
            {
                boltGame.StartMiniGameAt(new Vector3(center.x, tableSurfaceY + 0.35f, center.z));
            }
            else
            {
                FinishRepair();
            }
        }

        private void HandleWiringDone()
        {
            FinishRepair();
        }

        private void HandleBoltsDone()
        {
            FinishRepair();
        }

        public void FinishRepair()
        {
            if (currentPart == null) return;

            int creditCost = currentPart.Data != null ? currentPart.Data.repairCreditCost : 30;
            int alloyCost = currentPart.Data != null ? currentPart.Data.repairAlloyCost : 15;

            GameManager.Instance?.TrySpendResources(creditCost, alloyCost);
            currentPart.RepairFull();

            HangarAudio.Instance?.PlaySuccess();
            Debug.Log($"<color=green>[RepairWorkbench] Деталь {currentPart.name} отремонтирована на 100%!</color>");

            EventBus.TriggerPartRepairFinished(currentPart);
            ReturnPartToMech();
        }

        public void ReturnPartToMech()
        {
            if (currentPart == null) return;

            if (boltGame != null) boltGame.Cleanup();
            if (weldingGame != null) weldingGame.Cleanup();
            if (wiringGame != null) wiringGame.Cleanup();

            Rigidbody rb = currentPart.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = originalPartKinematic;
            }

            currentPart.ReturnToSocket();
            currentPart = null;

            GameManager.Instance?.SwitchRoomView(RoomView.Hangar);
        }
    }
}
