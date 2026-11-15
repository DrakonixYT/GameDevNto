using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;

namespace HangarGame.Repair
{
    public class RepairWorkbench : MonoBehaviour
    {
        public static RepairWorkbench Instance { get; private set; }

        [Header("Workbench Mount")]
        [SerializeField] private Transform partMountPoint;

        [Header("Mini-games")]
        [SerializeField] private BoltMiniGame boltGame;
        [SerializeField] private WeldingMiniGame weldingGame;

        private MechPartInstance currentPart;
        private Vector3 originalPartPosition;
        private Quaternion originalPartRotation;

        public MechPartInstance CurrentPart => currentPart;
        public bool HasPart => currentPart != null;

        private void Awake()
        {
            Instance = this;

            if (boltGame == null) boltGame = GetComponentInChildren<BoltMiniGame>(true);
            if (weldingGame == null) weldingGame = GetComponentInChildren<WeldingMiniGame>(true);

            if (partMountPoint == null)
            {
                partMountPoint = transform;
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

            // Перемещаем деталь на верстак
            part.transform.position = partMountPoint.position + Vector3.up * 0.2f;
            part.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Переключаем камеру на верстак
            GameManager.Instance.SwitchRoomView(RoomView.WorkbenchFocus);

            // Запускаем соответствующую мини-игру в зависимости от степени повреждений
            if (part.DurabilityPercent < 0.5f && weldingGame != null)
            {
                weldingGame.StartMiniGame(partMountPoint);
            }
            else if (boltGame != null)
            {
                boltGame.StartMiniGame(partMountPoint);
            }
            else
            {
                // Если мини-игр нет — мгновенный ремонт
                FinishRepair();
            }

            EventBus.TriggerPartSentToWorkbench(part);
        }

        private void HandleWeldingDone()
        {
            // После сварки — закручиваем болты
            if (boltGame != null)
            {
                boltGame.StartMiniGame(partMountPoint);
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

            // Списываем стоимость ремонта
            int creditCost = currentPart.Data != null ? currentPart.Data.repairCreditCost : 30;
            int alloyCost = currentPart.Data != null ? currentPart.Data.repairAlloyCost : 15;

            GameManager.Instance.TrySpendResources(creditCost, alloyCost);
            currentPart.RepairFull();

            Debug.Log($"<color=green>[RepairWorkbench] Деталь {currentPart.name} отремонтирована на 100%!</color>");

            EventBus.TriggerPartRepairFinished(currentPart);

            // Возвращаем деталь в меха
            ReturnPartToMech();
        }

        public void ReturnPartToMech()
        {
            if (currentPart == null) return;

            if (boltGame != null) boltGame.Cleanup();
            if (weldingGame != null) weldingGame.Cleanup();

            currentPart.ReturnToSocket();
            currentPart = null;

            // Возвращаем камеру в ангар
            GameManager.Instance.SwitchRoomView(RoomView.Hangar);
        }
    }
}
