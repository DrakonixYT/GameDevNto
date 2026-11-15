using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;
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

            if (partMountPoint == null)
            {
                // Ищем или создаем точку крепления точно над поверхностью стола
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
            if (boltGame != null)
            {
                boltGame.OnAllBoltsCompleted += HandleBoltsDone;
            }
            if (weldingGame != null)
            {
                weldingGame.OnWeldingCompleted += HandleWeldingDone;
            }
        }

        private void OnDestroy()
        {
            if (boltGame != null)
            {
                boltGame.OnAllBoltsCompleted -= HandleBoltsDone;
            }
            if (weldingGame != null)
            {
                weldingGame.OnWeldingCompleted -= HandleWeldingDone;
            }
        }

        private void Update()
        {
            if (currentPart == null) return;

            // Горячие клавиши на верстаке: ESC - выход, Пробел - быстрый ремонт
            bool esc = false;
            bool space = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                esc = Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame;
                space = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
#else
            esc = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
            space = Input.GetKeyDown(KeyCode.Space);
#endif

            if (esc)
            {
                ReturnPartToMech();
            }
            else if (space)
            {
                FinishRepair();
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

            // Отключаем физику, чтобы деталь не падала
            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb != null)
            {
                originalPartKinematic = rb.isKinematic;
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }

            // Вычисляем высоту детали, чтобы положить ее АККУРАТНО НА СТОЛ, а не под стол
            Collider col = part.GetComponent<Collider>();
            float halfHeight = 0.25f;
            if (col != null)
            {
                halfHeight = col.bounds.extents.y;
            }

            Vector3 tableCenter = partMountPoint != null ? partMountPoint.position : new Vector3(0f, tableSurfaceY, 2.2f);
            
            // Если это длинная нога — кладем её горизонтально на бок
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

            // Переключаем камеру на верстак
            GameManager.Instance?.SwitchRoomView(RoomView.WorkbenchFocus);

            // Запускаем мини-игру (позиция точно над деталью)
            Vector3 miniGameCenter = new Vector3(tableCenter.x, tableSurfaceY + 0.35f, tableCenter.z);

            if (part.DurabilityPercent < 0.6f && weldingGame != null)
            {
                weldingGame.StartMiniGameAt(miniGameCenter);
            }
            else if (boltGame != null)
            {
                boltGame.StartMiniGameAt(miniGameCenter);
            }

            EventBus.TriggerPartSentToWorkbench(part);
            Debug.Log($"<color=cyan>[RepairWorkbench]</color> Деталь {part.name} на верстаке. (Пробел - мгновенный ремонт, Esc - выход).");
        }

        private void HandleWeldingDone()
        {
            if (boltGame != null)
            {
                Vector3 center = partMountPoint != null ? partMountPoint.position : new Vector3(0f, tableSurfaceY, 2.2f);
                boltGame.StartMiniGameAt(new Vector3(center.x, tableSurfaceY + 0.35f, center.z));
            }
            else
            {
                FinishRepair();
            }
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

            // Восстанавливаем физику
            Rigidbody rb = currentPart.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = originalPartKinematic;
            }

            currentPart.ReturnToSocket();
            currentPart = null;

            // Возвращаем камеру в ангар
            GameManager.Instance?.SwitchRoomView(RoomView.Hangar);
        }
    }
}
