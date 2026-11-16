using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.CameraControl;
using HangarGame.Interaction;
using HangarGame.Repair;
using HangarGame.Mechs;

namespace HangarGame.UI
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Room Navigation Buttons (180 deg)")]
        [SerializeField] private Button btnLookLeft;
        [SerializeField] private Button btnLookRight;
        [SerializeField] private Text txtLookLeft;
        [SerializeField] private Text txtLookRight;

        [Header("Workbench Overlay UI")]
        [SerializeField] private GameObject workbenchPanel;
        [SerializeField] private Button btnExitWorkbench;
        [SerializeField] private Button btnInstantRepair;
        [SerializeField] private Button btnSwapModule;
        [SerializeField] private Text txtWorkbenchTitle;
        [SerializeField] private Text txtModuleInfo;

        [Header("Workbench 2-Axis Rotation Controls")]
        [SerializeField] private Button btnRotateYawMinus;
        [SerializeField] private Button btnRotateYawPlus;
        [SerializeField] private Button btnRotatePitchMinus;
        [SerializeField] private Button btnRotatePitchPlus;
        [SerializeField] private Button btnResetRotation;

        [Header("Status & Resources UI")]
        [SerializeField] private Text txtResources;
        [SerializeField] private Text txtRoomIndicator;

        [Header("Hover Prompt UI")]
        [SerializeField] private Text txtHoverPrompt;

        [Header("Victory Screen")]
        [SerializeField] private GameObject victoryPanel;

        [Header("Camera Reference")]
        [SerializeField] private RoomCameraController cameraController;

        private void Awake()
        {
            Instance = this;

            if (cameraController == null)
            {
                cameraController = FindObjectOfType<RoomCameraController>();
            }

            SetupButtons();
        }

        private void OnEnable()
        {
            EventBus.OnResourcesUpdated += HandleResourcesUpdated;
            EventBus.OnRoomViewChanged += HandleRoomViewChanged;
            EventBus.OnPartSentToWorkbench += HandlePartSentToWorkbench;
            EventBus.OnGameWon += HandleGameWon;
        }

        private void OnDisable()
        {
            EventBus.OnResourcesUpdated -= HandleResourcesUpdated;
            EventBus.OnRoomViewChanged -= HandleRoomViewChanged;
            EventBus.OnPartSentToWorkbench -= HandlePartSentToWorkbench;
            EventBus.OnGameWon -= HandleGameWon;
        }

        private void Update()
        {
            UpdateHoverPrompt();
        }

        public void SetupButtons()
        {
            if (btnLookLeft == null) btnLookLeft = transform.Find("Btn_TurnLeft")?.GetComponent<Button>();
            if (btnLookRight == null) btnLookRight = transform.Find("Btn_TurnRight")?.GetComponent<Button>();
            if (btnExitWorkbench == null) btnExitWorkbench = transform.Find("Workbench_Panel/Btn_ExitWorkbench")?.GetComponent<Button>();
            if (btnInstantRepair == null) btnInstantRepair = transform.Find("Workbench_Panel/Btn_InstantRepair")?.GetComponent<Button>();
            if (btnSwapModule == null) btnSwapModule = transform.Find("Workbench_Panel/Btn_SwapModule")?.GetComponent<Button>();
            if (workbenchPanel == null) workbenchPanel = transform.Find("Workbench_Panel")?.gameObject;
            if (victoryPanel == null) victoryPanel = transform.Find("Victory_Panel")?.gameObject;

            // Кнопки вращения детали
            if (btnRotateYawMinus == null) btnRotateYawMinus = transform.Find("Workbench_Panel/Btn_RotYawMinus")?.GetComponent<Button>();
            if (btnRotateYawPlus == null) btnRotateYawPlus = transform.Find("Workbench_Panel/Btn_RotYawPlus")?.GetComponent<Button>();
            if (btnRotatePitchMinus == null) btnRotatePitchMinus = transform.Find("Workbench_Panel/Btn_RotPitchMinus")?.GetComponent<Button>();
            if (btnRotatePitchPlus == null) btnRotatePitchPlus = transform.Find("Workbench_Panel/Btn_RotPitchPlus")?.GetComponent<Button>();
            if (btnResetRotation == null) btnResetRotation = transform.Find("Workbench_Panel/Btn_RotReset")?.GetComponent<Button>();

            if (btnLookLeft != null)
            {
                btnLookLeft.onClick.RemoveAllListeners();
                btnLookLeft.onClick.AddListener(() => cameraController?.Toggle180View());
            }

            if (btnLookRight != null)
            {
                btnLookRight.onClick.RemoveAllListeners();
                btnLookRight.onClick.AddListener(() => cameraController?.Toggle180View());
            }

            if (btnExitWorkbench != null)
            {
                btnExitWorkbench.onClick.RemoveAllListeners();
                btnExitWorkbench.onClick.AddListener(() =>
                {
                    RepairWorkbench.Instance?.CancelOrReturnCurrentPart();
                });
            }

            if (btnInstantRepair != null)
            {
                btnInstantRepair.onClick.RemoveAllListeners();
                btnInstantRepair.onClick.AddListener(() =>
                {
                    RepairWorkbench.Instance?.FinishRepair();
                });
            }

            if (btnSwapModule != null)
            {
                btnSwapModule.onClick.RemoveAllListeners();
                btnSwapModule.onClick.AddListener(() =>
                {
                    RepairWorkbench.Instance?.CycleNextModuleForCurrentPart();
                    UpdateWorkbenchModuleInfo();
                });
            }

            // Биндинг кнопок вращения детали по 2 осям
            if (btnRotateYawMinus != null)
            {
                btnRotateYawMinus.onClick.RemoveAllListeners();
                btnRotateYawMinus.onClick.AddListener(() => RepairWorkbench.Instance?.RotatePartYaw(-30f));
            }

            if (btnRotateYawPlus != null)
            {
                btnRotateYawPlus.onClick.RemoveAllListeners();
                btnRotateYawPlus.onClick.AddListener(() => RepairWorkbench.Instance?.RotatePartYaw(30f));
            }

            if (btnRotatePitchMinus != null)
            {
                btnRotatePitchMinus.onClick.RemoveAllListeners();
                btnRotatePitchMinus.onClick.AddListener(() => RepairWorkbench.Instance?.RotatePartPitch(-20f));
            }

            if (btnRotatePitchPlus != null)
            {
                btnRotatePitchPlus.onClick.RemoveAllListeners();
                btnRotatePitchPlus.onClick.AddListener(() => RepairWorkbench.Instance?.RotatePartPitch(20f));
            }

            if (btnResetRotation != null)
            {
                btnResetRotation.onClick.RemoveAllListeners();
                btnResetRotation.onClick.AddListener(() => RepairWorkbench.Instance?.ResetPartRotation());
            }
        }

        private void HandleResourcesUpdated(int credits, int alloy)
        {
            if (txtResources != null)
            {
                txtResources.text = $"КРЕДИТЫ: {credits} ¢   |   СПЛАВ: {alloy} т.";
            }
        }

        private void HandleRoomViewChanged(RoomView view)
        {
            bool isWorkbench = (view == RoomView.WorkbenchFocus);

            if (workbenchPanel != null)
            {
                workbenchPanel.SetActive(isWorkbench);
            }

            if (btnLookLeft != null)
            {
                btnLookLeft.gameObject.SetActive(!isWorkbench);
            }

            if (btnLookRight != null)
            {
                btnLookRight.gameObject.SetActive(!isWorkbench);
            }

            if (txtLookLeft != null)
            {
                txtLookLeft.text = (view == RoomView.Hangar) ? "◀ Диспетчерская\n(180° / A / Q)" : "◀ Ангар\n(180° / A / Q)";
            }

            if (txtLookRight != null)
            {
                txtLookRight.text = (view == RoomView.Hangar) ? "Диспетчерская ▶\n(180° / D / E)" : "Ангар ▶\n(180° / D / E)";
            }

            if (txtRoomIndicator != null)
            {
                if (view == RoomView.Hangar) txtRoomIndicator.text = "СЕКТОР: АНГАР [0°]";
                else if (view == RoomView.DispatchConsole) txtRoomIndicator.text = "СЕКТОР: ДИСПЕТЧЕРСКАЯ [180°]";
                else txtRoomIndicator.text = "РЕЖИМ: ВЕРСТАК ТО";
            }

            if (isWorkbench)
            {
                UpdateWorkbenchModuleInfo();
            }
        }

        private void HandlePartSentToWorkbench(object partObj)
        {
            UpdateWorkbenchModuleInfo();
        }

        public void UpdateWorkbenchModuleInfo()
        {
            if (RepairWorkbench.Instance == null) return;

            if (RepairWorkbench.Instance.IsJaegerModule)
            {
                if (txtWorkbenchTitle != null)
                {
                    txtWorkbenchTitle.text = $"🔨 СБОРКА МОДУЛЯ ЕГЕРЯ: {RepairWorkbench.Instance.CurrentItemName}";
                }

                if (txtModuleInfo != null)
                {
                    txtModuleInfo.text = "Осмотрите деталь (A/D/W/S или ПКМ). Выполните мини-игру для установки на каркас Егеря.";
                }

                if (btnSwapModule != null)
                {
                    btnSwapModule.gameObject.SetActive(false);
                }

                if (btnInstantRepair != null)
                {
                    Text t = btnInstantRepair.GetComponentInChildren<Text>();
                    if (t != null) t.text = "⚡ УСТАНОВИТЬ НА КАРКАС (SPACE)";
                }
            }
            else
            {
                MechPartInstance part = RepairWorkbench.Instance.CurrentPart;
                if (part != null)
                {
                    if (txtWorkbenchTitle != null)
                    {
                        txtWorkbenchTitle.text = $"🔧 ВЕРСТАК: {part.PartName} (Прочность: {part.DurabilityPercent * 100f:F0}%)";
                    }

                    if (txtModuleInfo != null)
                    {
                        txtModuleInfo.text = $"Атака: {part.AttackType} ({part.AttackPower:F0})  |  Защита: {part.ResistanceType} ({part.DefensePower:F0})";
                    }

                    if (btnSwapModule != null)
                    {
                        btnSwapModule.gameObject.SetActive(true);
                    }

                    if (btnInstantRepair != null)
                    {
                        Text t = btnInstantRepair.GetComponentInChildren<Text>();
                        if (t != null) t.text = "⚡ БЫСТРЫЙ РЕМОНТ (SPACE)";
                    }
                }
            }
        }

        private void HandleGameWon()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }

        private void UpdateHoverPrompt()
        {
            if (txtHoverPrompt != null)
            {
                string prompt = PlayerRaycaster.Instance != null ? PlayerRaycaster.Instance.CurrentPrompt : "";
                txtHoverPrompt.text = prompt;
            }
        }
    }
}
