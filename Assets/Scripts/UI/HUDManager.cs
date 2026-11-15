using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.CameraControl;
using HangarGame.Interaction;
using HangarGame.Repair;

namespace HangarGame.UI
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Room Navigation Buttons (FNAF-style 180 deg)")]
        [SerializeField] private Button btnLookLeft;
        [SerializeField] private Button btnLookRight;
        [SerializeField] private Text txtLookLeft;
        [SerializeField] private Text txtLookRight;

        [Header("Workbench Overlay UI")]
        [SerializeField] private GameObject workbenchPanel;
        [SerializeField] private Button btnExitWorkbench;
        [SerializeField] private Button btnInstantRepair;
        [SerializeField] private Text txtWorkbenchTitle;

        [Header("Status & Resources UI")]
        [SerializeField] private Text txtResources;
        [SerializeField] private Text txtJaegerProgress;
        [SerializeField] private Slider sliderJaeger;
        [SerializeField] private Button btnBuildJaeger;

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
            EventBus.OnJaegerProgressUpdated += HandleJaegerProgressUpdated;
            EventBus.OnRoomViewChanged += HandleRoomViewChanged;
            EventBus.OnGameWon += HandleGameWon;
        }

        private void OnDisable()
        {
            EventBus.OnResourcesUpdated -= HandleResourcesUpdated;
            EventBus.OnJaegerProgressUpdated -= HandleJaegerProgressUpdated;
            EventBus.OnRoomViewChanged -= HandleRoomViewChanged;
            EventBus.OnGameWon -= HandleGameWon;
        }

        private void Update()
        {
            UpdateHoverPrompt();
        }

        private void SetupButtons()
        {
            if (btnLookLeft == null) btnLookLeft = transform.Find("Btn_TurnLeft")?.GetComponent<Button>();
            if (btnLookRight == null) btnLookRight = transform.Find("Btn_TurnRight")?.GetComponent<Button>();
            if (btnExitWorkbench == null) btnExitWorkbench = transform.Find("Workbench_Panel/Btn_ExitWorkbench")?.GetComponent<Button>();
            if (btnInstantRepair == null) btnInstantRepair = transform.Find("Workbench_Panel/Btn_InstantRepair")?.GetComponent<Button>();
            if (btnBuildJaeger == null) btnBuildJaeger = transform.Find("Btn_BuildJaeger")?.GetComponent<Button>();
            if (workbenchPanel == null) workbenchPanel = transform.Find("Workbench_Panel")?.gameObject;
            if (victoryPanel == null) victoryPanel = transform.Find("Victory_Panel")?.gameObject;

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
                    RepairWorkbench.Instance?.ReturnPartToMech();
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

            if (btnBuildJaeger != null)
            {
                btnBuildJaeger.onClick.RemoveAllListeners();
                btnBuildJaeger.onClick.AddListener(() =>
                {
                    JaegerProjectManager.Instance?.InvestInJaeger();
                });
            }
        }

        private void HandleResourcesUpdated(int credits, int alloy)
        {
            if (txtResources != null)
            {
                txtResources.text = $"КРЕДИТЫ: {credits} ¢   |   СПЛАВ: {alloy} т.";
            }
        }

        private void HandleJaegerProgressUpdated(float progress01)
        {
            if (sliderJaeger != null)
            {
                sliderJaeger.value = progress01;
            }

            if (txtJaegerProgress != null)
            {
                txtJaegerProgress.text = $"ПРОЕКТ 'ЕГЕРЬ': {Mathf.RoundToInt(progress01 * 100f)}% (Собрать 100% для победы)";
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
