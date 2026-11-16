using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.CameraControl;
using HangarGame.Repair;
using HangarGame.Interaction;

namespace HangarGame.UI
{
    public class HUDOverlay : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Text txtResources;
        [SerializeField] private Text txtRoomIndicator;

        [Header("Navigation Buttons")]
        [SerializeField] private Button btnTurnLeft;
        [SerializeField] private Button btnTurnRight;

        [Header("Hover Prompt")]
        [SerializeField] private Text txtHoverPrompt;

        [Header("Workbench Panel")]
        [SerializeField] private GameObject wbPanel;
        [SerializeField] private Text txtWbTitle;
        [SerializeField] private Text txtWbInfo;
        [SerializeField] private Button btnRotYM, btnRotYP, btnRotPM, btnRotPP, btnResetRotation;
        [SerializeField] private Button btnFinish, btnSwap, btnExit;

        [Header("Victory Screen")]
        [SerializeField] private GameObject victoryPanel;

        private void Start()
        {
            if (btnTurnLeft != null) btnTurnLeft.onClick.AddListener(() => RoomCamera.Instance?.Toggle180());
            if (btnTurnRight != null) btnTurnRight.onClick.AddListener(() => RoomCamera.Instance?.Toggle180());

            if (btnRotYM != null) btnRotYM.onClick.AddListener(() => RepairWorkbench.Instance?.RotateYaw(-30f));
            if (btnRotYP != null) btnRotYP.onClick.AddListener(() => RepairWorkbench.Instance?.RotateYaw(30f));
            if (btnRotPM != null) btnRotPM.onClick.AddListener(() => RepairWorkbench.Instance?.RotatePitch(-20f));
            if (btnRotPP != null) btnRotPP.onClick.AddListener(() => RepairWorkbench.Instance?.RotatePitch(20f));
            if (btnResetRotation != null) btnResetRotation.onClick.AddListener(() => RepairWorkbench.Instance?.ResetRotation());

            if (btnFinish != null) btnFinish.onClick.AddListener(() => RepairWorkbench.Instance?.FinishWork());
            if (btnSwap != null) btnSwap.onClick.AddListener(() => RepairWorkbench.Instance?.CycleModule());
            if (btnExit != null) btnExit.onClick.AddListener(() => RepairWorkbench.Instance?.CancelWork());

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnResourcesChanged += (c, a) => UpdateResources(c, a);
                GameManager.Instance.OnGameWon += () => { if (victoryPanel != null) victoryPanel.SetActive(true); };
            }
        }

        private void Update()
        {
            if (txtHoverPrompt != null && PlayerRaycaster.Instance != null)
                txtHoverPrompt.text = PlayerRaycaster.Instance.CurrentPrompt;

            if (GameManager.Instance == null) return;

            bool isWb = GameManager.Instance.CurrentView == RoomView.WorkbenchFocus;
            if (wbPanel != null) wbPanel.SetActive(isWb);
            if (btnTurnLeft != null) btnTurnLeft.gameObject.SetActive(!isWb);
            if (btnTurnRight != null) btnTurnRight.gameObject.SetActive(!isWb);

            if (txtRoomIndicator != null)
            {
                if (GameManager.Instance.CurrentView == RoomView.Hangar) txtRoomIndicator.text = "СЕКТОР: АНГАР [0°]";
                else if (GameManager.Instance.CurrentView == RoomView.DispatchConsole) txtRoomIndicator.text = "СЕКТОР: ДИСПЕТЧЕРСКАЯ [180°]";
                else txtRoomIndicator.text = "РЕЖИМ: ВЕРСТАК ТО";
            }

            if (isWb && RepairWorkbench.Instance != null)
            {
                string name = RepairWorkbench.Instance.CurrentItemName ?? "Модуль";
                if (txtWbTitle != null) txtWbTitle.text = $"🔧 ВЕРСТАК: {name}";
                if (txtWbInfo != null) txtWbInfo.text = RepairWorkbench.Instance.IsJaegerModule
                    ? "Сборка модуля Егеря. Вращайте деталь (A/D/W/S или ПКМ), выполните калибровку."
                    : "Осмотр модуля робота. Вращайте (A/D/W/S или ПКМ), устраните дефекты.";
                if (btnSwap != null) btnSwap.gameObject.SetActive(!RepairWorkbench.Instance.IsJaegerModule);
            }
        }

        private void UpdateResources(int credits, int alloy)
        {
            if (txtResources != null) txtResources.text = $"КРЕДИТЫ: {credits} ¢   |   СПЛАВ: {alloy} т.";
        }
    }
}
