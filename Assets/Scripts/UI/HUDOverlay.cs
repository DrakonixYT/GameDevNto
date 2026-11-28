using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Interaction;
using HangarGame.Mechs;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.UI
{
    public class HUDOverlay : MonoBehaviour
    {
        [Header("Верхняя панель")]
        [SerializeField] private Text txtResources;
        [SerializeField] private Text txtRoomIndicator;

        [Header("Кнопки переворота")]
        [SerializeField] private Button btnTurnLeft;
        [SerializeField] private Button btnTurnRight;

        [Header("Панель подиума мехов")]
        [SerializeField] private GameObject mechSelectPanel;
        [SerializeField] private Button btnPrevMech;
        [SerializeField] private Button btnNextMech;
        [SerializeField] private Button btnRechargeBattery;
        [SerializeField] private Text txtCurrentMech;

        [Header("Подсказка и инфо")]
        [SerializeField] private Text txtHoverPrompt;

        [Header("Инфо-панель верстака (управление 3D кнопками)")]
        [SerializeField] private GameObject wbPanel;
        [SerializeField] private Text txtWbTitle;
        [SerializeField] private Text txtWbInfo;

        [Header("Экран победы")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button btnRestart;

        public void Configure(
            Text res, Text roomInd, Button turnL, Button turnR,
            GameObject mechPnl, Button prev, Button next, Button recharge,
            Text curMech, Text hoverPrompt,
            GameObject wbPnl, Text wbTitle, Text wbInfo, GameObject vicPnl, Button restartBtn = null)
        {
            txtResources = res;
            txtRoomIndicator = roomInd;
            btnTurnLeft = turnL;
            btnTurnRight = turnR;
            mechSelectPanel = mechPnl;
            btnPrevMech = prev;
            btnNextMech = next;
            btnRechargeBattery = recharge;
            txtCurrentMech = curMech;
            txtHoverPrompt = hoverPrompt;
            wbPanel = wbPnl;
            txtWbTitle = wbTitle;
            txtWbInfo = wbInfo;
            victoryPanel = vicPnl;
            btnRestart = restartBtn;
        }

        private void Start()
        {
            if (btnTurnLeft != null) btnTurnLeft.onClick.AddListener(() => RoomCamera.Instance?.TurnLeft());
            if (btnTurnRight != null) btnTurnRight.onClick.AddListener(() => RoomCamera.Instance?.TurnRight());

            if (btnPrevMech != null) btnPrevMech.onClick.AddListener(() => HangarMechManager.Instance?.PrevMech());
            if (btnNextMech != null) btnNextMech.onClick.AddListener(() => HangarMechManager.Instance?.NextMech());
            if (btnRechargeBattery != null)
            {
                btnRechargeBattery.onClick.AddListener(() =>
                {
                    HangarMechManager.Instance?.RechargeCurrentMech();
                    HangarAudio.Instance?.PlayBattery();
                });
            }

            if (btnRestart == null && victoryPanel != null)
                btnRestart = victoryPanel.transform.Find("bRest")?.GetComponent<Button>();

            if (btnRestart != null)
                btnRestart.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

            if (txtResources != null)
            {
                txtResources.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (txtRoomIndicator == null)
                {
                    var rt = txtResources.rectTransform;
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(760f, 40f);
                    txtResources.alignment = TextAnchor.MiddleCenter;
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnResourcesChanged += UpdateResources;
                GameManager.Instance.OnBaseHpChanged += UpdateBaseHp;
                GameManager.Instance.OnGameWon += ShowVictoryPanel;
                GameManager.Instance.OnGameLost += ShowDefeatPanel;
                UpdateResourceText();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnResourcesChanged -= UpdateResources;
                GameManager.Instance.OnBaseHpChanged -= UpdateBaseHp;
                GameManager.Instance.OnGameWon -= ShowVictoryPanel;
                GameManager.Instance.OnGameLost -= ShowDefeatPanel;
            }
        }

        private void ShowVictoryPanel()
        {
            if (victoryPanel == null) return;
            var t1 = victoryPanel.transform.Find("Txt_Vic1")?.GetComponent<Text>();
            var t2 = victoryPanel.transform.Find("Txt_Vic2")?.GetComponent<Text>();
            if (t1 != null)
            {
                t1.text = "Победа!";
                t1.color = Color.yellow;
            }
            if (t2 != null)
                t2.text = "Егерь собран.";
            victoryPanel.SetActive(true);
        }

        private void ShowDefeatPanel()
        {
            if (victoryPanel == null) return;
            var t1 = victoryPanel.transform.Find("Txt_Vic1")?.GetComponent<Text>();
            var t2 = victoryPanel.transform.Find("Txt_Vic2")?.GetComponent<Text>();
            if (t1 != null)
            {
                t1.text = "База потеряна";
                t1.color = new Color(1f, 0.3f, 0.25f);
            }
            if (t2 != null)
                t2.text = "Периметр прорван.";
            victoryPanel.SetActive(true);
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
            if (mechSelectPanel != null) mechSelectPanel.SetActive(isHangar);

            UpdateTurnButtons(isHangar, isWb);
            UpdateRoomLabel(isHangar, isDispatch);

            if (isHangar) UpdateHangarControls();
            if (isWb) UpdateWorkbenchInfo();
        }

        private void UpdateTurnButtons(bool isHangar, bool isWb)
        {
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
        }

        private void UpdateRoomLabel(bool isHangar, bool isDispatch)
        {
            if (txtRoomIndicator == null) return;
            if (isHangar)
                txtRoomIndicator.text = "Ангар  [Q/E — пульт]";
            else if (isDispatch)
                txtRoomIndicator.text = (RoomCamera.Instance != null && RoomCamera.Instance.IsArmoryFocused)
                    ? "Арсенал  [Tab — радар | Q/E — ангар]"
                    : "Пульт  [Q/E — ангар | Tab — арсенал]";
            else
                txtRoomIndicator.text = "Верстак  [3D кнопки на столе]";
        }

        private void UpdateHangarControls()
        {
            if (GameManager.Instance != null && (GameManager.Instance.IsGameWon || GameManager.Instance.IsGameLost))
                return;

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
            if (left) HangarMechManager.Instance?.PrevMech();
            if (right) HangarMechManager.Instance?.NextMech();

            var cur = HangarMechManager.Instance != null ? HangarMechManager.Instance.CurrentMech : null;
            if (cur == null) return;

            if (txtCurrentMech != null)
            {
                string status = cur.isDeployed ? "В бою" : "Ангар";
                txtCurrentMech.text = $"{cur.mechName} ({HangarMechManager.Instance.CurrentIndex + 1}/{HangarMechManager.Instance.Mechs.Count}) | {status} | {cur.currentHp:F0}/{cur.maxHp:F0} HP | {cur.currentEnergy:F0}%";
            }

            if (btnRechargeBattery != null)
            {
                btnRechargeBattery.interactable = !cur.isDeployed && cur.currentEnergy < 99f;
                var bTxt = btnRechargeBattery.GetComponentInChildren<Text>();
                if (bTxt != null) bTxt.text = cur.currentEnergy >= 99f ? "Батарея 100%" : "Зарядить";
            }
        }

        private void UpdateWorkbenchInfo()
        {
            if (RepairWorkbench.Instance == null) return;
            string itemName = RepairWorkbench.Instance.CurrentItemName ?? "Деталь";
            if (txtWbTitle != null) txtWbTitle.text = itemName;
            if (txtWbInfo != null) txtWbInfo.text = RepairWorkbench.Instance.RepairStepInfo;
        }

        private void UpdateResources(int credits, int alloy) => UpdateResourceText();
        private void UpdateBaseHp(float hp, float maxHp) => UpdateResourceText();

        private void UpdateResourceText()
        {
            if (txtResources == null || GameManager.Instance == null) return;
            var gm = GameManager.Instance;
            txtResources.text = $"{gm.Credits} ¢   |   {gm.Alloy} т   |   База: {gm.BaseHp:F0}%";
        }
    }
}
