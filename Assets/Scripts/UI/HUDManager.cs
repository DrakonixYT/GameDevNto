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
        [SerializeField] private Button btnExitWorkbench;

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
            if (btnLookLeft != null)
            {
                btnLookLeft.onClick.AddListener(() => cameraController?.Toggle180View());
            }

            if (btnLookRight != null)
            {
                btnLookRight.onClick.AddListener(() => cameraController?.Toggle180View());
            }

            if (btnExitWorkbench != null)
            {
                btnExitWorkbench.onClick.AddListener(() =>
                {
                    RepairWorkbench.Instance?.ReturnPartToMech();
                });
            }

            if (btnBuildJaeger != null)
            {
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
            if (btnExitWorkbench != null)
            {
                btnExitWorkbench.gameObject.SetActive(view == RoomView.WorkbenchFocus);
            }

            if (btnLookLeft != null)
            {
                btnLookLeft.gameObject.SetActive(view != RoomView.WorkbenchFocus);
            }

            if (btnLookRight != null)
            {
                btnLookRight.gameObject.SetActive(view != RoomView.WorkbenchFocus);
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
