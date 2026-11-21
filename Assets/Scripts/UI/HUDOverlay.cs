using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.CameraControl;
using HangarGame.Repair;
using HangarGame.Interaction;
using HangarGame.Mechs;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        [Header("Podium Mech Switcher")]
        [SerializeField] private GameObject mechSelectPanel;
        [SerializeField] private Button btnPrevMech;
        [SerializeField] private Button btnNextMech;
        [SerializeField] private Button btnRechargeBattery;
        [SerializeField] private Text txtCurrentMech;

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
            if (btnTurnLeft != null) btnTurnLeft.onClick.AddListener(() => RoomCamera.Instance?.TurnLeft());
            if (btnTurnRight != null) btnTurnRight.onClick.AddListener(() => RoomCamera.Instance?.TurnRight());

            if (btnPrevMech != null) btnPrevMech.onClick.AddListener(() => HangarMechManager.Instance?.PrevMech());
            if (btnNextMech != null) btnNextMech.onClick.AddListener(() => HangarMechManager.Instance?.NextMech());
            if (btnRechargeBattery != null) btnRechargeBattery.onClick.AddListener(() =>
            {
                HangarMechManager.Instance?.RechargeCurrentMech();
                HangarAudioLight.Instance?.PlaySuccess();
                HangarAudioLight.Instance?.PlayBolt();
            });

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

            bool isHangar = GameManager.Instance.CurrentView == RoomView.Hangar;
            bool isDispatch = GameManager.Instance.CurrentView == RoomView.DispatchConsole;
            bool isWb = GameManager.Instance.CurrentView == RoomView.WorkbenchFocus;

            if (wbPanel != null) wbPanel.SetActive(isWb);
            if (btnTurnLeft != null)
            {
                btnTurnLeft.gameObject.SetActive(!isWb);
                var t = btnTurnLeft.GetComponentInChildren<Text>();
                if (t != null) t.text = isHangar ? "◀ Рубка (Q)" : "◀ Ангар (Q)";
            }
            if (btnTurnRight != null)
            {
                btnTurnRight.gameObject.SetActive(!isWb);
                var t = btnTurnRight.GetComponentInChildren<Text>();
                if (t != null) t.text = isHangar ? "Рубка ▶ (E)" : "Ангар ▶ (E)";
            }
            if (mechSelectPanel != null) mechSelectPanel.SetActive(isHangar);

            if (txtRoomIndicator != null)
            {
                if (isHangar) txtRoomIndicator.text = "СЕКТОР: АНГАР [0°] | [Q/E - Рубка]";
                else if (isDispatch) txtRoomIndicator.text = "СЕКТОР: РУБКА УПРАВЛЕНИЯ [180°] | [Q/E - Ангар]";
                else txtRoomIndicator.text = "РЕЖИМ: ВЕРСТАК ТО";
            }

            if (isHangar && HangarMechManager.Instance != null)
            {
                bool left = false, right = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    left = Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
                    right = Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
                }
#else
                left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
                right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
                if (left) HangarMechManager.Instance.PrevMech();
                if (right) HangarMechManager.Instance.NextMech();

                var cur = HangarMechManager.Instance.CurrentMech;
                if (cur != null)
                {
                    if (txtCurrentMech != null)
                    {
                        string status = cur.isDeployed ? "В БОЮ" : "В АНГАРЕ";
                        txtCurrentMech.text = $"◀ [{HangarMechManager.Instance.CurrentIndex + 1}/{HangarMechManager.Instance.Mechs.Count}] {cur.mechName} | {status} | ♥ {cur.currentHp:F0}% | ⚡ {cur.currentEnergy:F0}% ▶";
                    }

                    if (btnRechargeBattery != null)
                    {
                        btnRechargeBattery.interactable = !cur.isDeployed && cur.currentEnergy < 99f;
                        var bTxt = btnRechargeBattery.GetComponentInChildren<Text>();
                        if (bTxt != null) bTxt.text = cur.currentEnergy >= 99f ? "⚡ БАТАРЕЯ 100%" : "⚡ ЗАРЯДИТЬ БАТАРЕЮ";
                    }
                }
            }

            if (isWb && RepairWorkbench.Instance != null)
            {
                string name = RepairWorkbench.Instance.CurrentItemName ?? "Модуль";
                if (txtWbTitle != null) txtWbTitle.text = $"🔧 ВЕРСТАК: {name}";
                if (txtWbInfo != null) txtWbInfo.text = RepairWorkbench.Instance.RepairStepInfo;
                if (btnSwap != null) btnSwap.gameObject.SetActive(!RepairWorkbench.Instance.IsJaegerModule);
            }
        }

        private void UpdateResources(int credits, int alloy)
        {
            if (txtResources != null) txtResources.text = $"КРЕДИТЫ: {credits} ¢   |   СПЛАВ: {alloy} т.";
        }
    }
}
