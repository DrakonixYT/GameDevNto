using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HangarGame.UI
{
    public class HUDOverlay : MonoBehaviour
    {
        public static HUDOverlay Instance { get; private set; }

        [SerializeField] private Text txtResources;
        [SerializeField] private Text txtRoomIndicator;
        [SerializeField] private Button btnTurnLeft;
        [SerializeField] private Button btnTurnRight;
        [SerializeField] private GameObject mechSelectPanel;
        [SerializeField] private Button btnPrevMech;
        [SerializeField] private Button btnNextMech;
        [SerializeField] private Button btnRechargeBattery;
        [SerializeField] private Text txtCurrentMech;
        [SerializeField] private Text txtHoverPrompt;
        [SerializeField] private GameObject wbPanel;
        [SerializeField] private Text txtWbTitle;
        [SerializeField] private Text txtWbInfo;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Button btnRestart;

        public void BindFromHierarchy()
        {
            txtResources = transform.Find("TopBar/Txt_Res")?.GetComponent<Text>();
            txtRoomIndicator = transform.Find("TopBar/Txt_Room")?.GetComponent<Text>();
            btnTurnLeft = transform.Find("Btn_Left")?.GetComponent<Button>();
            btnTurnRight = transform.Find("Btn_Right")?.GetComponent<Button>();

            Transform mechPnl = transform.Find("MechSelectPanel");
            mechSelectPanel = mechPnl != null ? mechPnl.gameObject : null;
            if (mechPnl != null)
            {
                btnPrevMech = mechPnl.Find("Btn_PrevMech")?.GetComponent<Button>();
                btnNextMech = mechPnl.Find("Btn_NextMech")?.GetComponent<Button>();
                btnRechargeBattery = mechPnl.Find("Btn_RechargeBattery")?.GetComponent<Button>();
                txtCurrentMech = mechPnl.Find("Txt_CurrentMech")?.GetComponent<Text>();
            }

            txtHoverPrompt = transform.Find("PromptBox/Txt_Prompt")?.GetComponent<Text>();

            Transform wbT = transform.Find("WorkbenchPanel");
            wbPanel = wbT != null ? wbT.gameObject : null;
            if (wbT != null)
            {
                txtWbTitle = wbT.Find("Txt_WbTitle")?.GetComponent<Text>();
                txtWbInfo = wbT.Find("Txt_WbInfo")?.GetComponent<Text>();
            }

            Transform vicT = transform.Find("VictoryPanel");
            victoryPanel = vicT != null ? vicT.gameObject : null;
            btnRestart = vicT != null ? vicT.Find("Btn_Restart")?.GetComponent<Button>() : null;
        }

        private bool eventsBound;

        private void Awake()
        {
            Instance = this;
            if (txtResources == null) BindFromHierarchy();

            WireButton(btnTurnLeft, () => RoomCamera.Instance?.TurnLeft());
            WireButton(btnTurnRight, () => RoomCamera.Instance?.TurnRight());
            WireButton(btnPrevMech, () =>
            {
                HangarAudio.Instance?.PlayClick();
                HangarMechManager.Instance?.PrevMech();
            });
            WireButton(btnNextMech, () =>
            {
                HangarAudio.Instance?.PlayClick();
                HangarMechManager.Instance?.NextMech();
            });
            WireButton(btnRechargeBattery, () =>
            {
                HangarMechManager.Instance?.RechargeCurrentMech();
                UpdateMechInfo();
            });
            WireButton(btnRestart, () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        }

        private void Start()
        {
            BindEvents();
            RefreshAll();
        }

        private static void WireButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void OnEnable()
        {
            BindEvents();
            RefreshAll();
        }

        private void OnDisable() => UnbindEvents();

        private void BindEvents()
        {
            if (eventsBound) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnResourcesChanged += HandleResourcesChanged;
            gm.OnBaseHpChanged += HandleBaseHpChanged;
            gm.OnViewChanged += HandleViewChanged;
            gm.OnJaegerProgressChanged += HandleJaegerChanged;
            gm.OnGameWon += ShowVictory;
            gm.OnGameLost += ShowDefeat;
            if (HangarMechManager.Instance != null)
                HangarMechManager.Instance.OnMechSwitched += HandleMechSwitched;
            eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!eventsBound) return;
            eventsBound = false;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnResourcesChanged -= HandleResourcesChanged;
                gm.OnBaseHpChanged -= HandleBaseHpChanged;
                gm.OnViewChanged -= HandleViewChanged;
                gm.OnJaegerProgressChanged -= HandleJaegerChanged;
                gm.OnGameWon -= ShowVictory;
                gm.OnGameLost -= ShowDefeat;
            }
            if (HangarMechManager.Instance != null)
                HangarMechManager.Instance.OnMechSwitched -= HandleMechSwitched;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentView == RoomView.Hangar)
            {
                if (GameInput.KeyDown(KeyCode.A) || GameInput.KeyDown(KeyCode.LeftArrow))
                    HangarMechManager.Instance?.PrevMech();
                if (GameInput.KeyDown(KeyCode.D) || GameInput.KeyDown(KeyCode.RightArrow))
                    HangarMechManager.Instance?.NextMech();
            }

            if (gm != null && gm.CurrentView == RoomView.WorkbenchFocus && RepairWorkbench.Instance != null)
            {
                var wb = RepairWorkbench.Instance;
                if (txtWbTitle != null)
                {
                    string action = wb.CurrentJaegerModule != null ? "Сборка Егеря" : "Ремонт";
                    txtWbTitle.text = $"{action}: {wb.CurrentItemName}";
                }
                if (txtWbInfo != null) txtWbInfo.text = wb.RepairStepInfo;
            }

            if (mechSelectPanel != null && mechSelectPanel.activeSelf)
                UpdateMechInfo();
        }

        public void SetPrompt(string text)
        {
            if (txtHoverPrompt == null) return;
            if (!string.IsNullOrEmpty(text))
            {
                txtHoverPrompt.text = text;
                return;
            }

            RoomView view = GameManager.Instance != null ? GameManager.Instance.CurrentView : RoomView.Hangar;
            txtHoverPrompt.text = view == RoomView.Hangar
                ? "A / D — смена робота  •  Клик по детали — ремонт  •  Q / E — пульт"
                : (view == RoomView.DispatchConsole
                    ? "Клик по радару — высадка / приказ  •  Tab — оружейная  •  Q / E — ангар"
                    : "ПКМ — вращение  •  Колесо — масштаб  •  Пробел — готово");
        }

        public void RefreshAll()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                UpdateTopBar(gm.Credits, gm.Alloy, gm.BaseHp);
                HandleViewChanged(gm.CurrentView);
            }
            UpdateMechInfo();
            SetPrompt("");
        }

        private void HandleResourcesChanged(int c, int a) => UpdateTopBar(c, a, GameManager.Instance != null ? GameManager.Instance.BaseHp : 100f);
        private void HandleBaseHpChanged(float hp, float max) => UpdateTopBar(GameManager.Instance != null ? GameManager.Instance.Credits : 0, GameManager.Instance != null ? GameManager.Instance.Alloy : 0, hp);
        private void HandleJaegerChanged(float p) => UpdateTopBar(GameManager.Instance != null ? GameManager.Instance.Credits : 0, GameManager.Instance != null ? GameManager.Instance.Alloy : 0, GameManager.Instance != null ? GameManager.Instance.BaseHp : 100f);
        private void HandleMechSwitched(int idx) => UpdateMechInfo();

        private void UpdateTopBar(int credits, int alloy, float baseHp)
        {
            if (txtResources == null) return;
            var jp = JaegerProject.Instance;
            string jStr = jp != null ? $"{jp.CurrentStage}/{jp.TotalStages}" : "0/5";
            txtResources.text = $"Кредиты: {credits}   |   Сплав: {alloy}   |   База: {baseHp:F0}%   |   Егерь: {jStr}";
        }

        private void HandleViewChanged(RoomView view)
        {
            if (txtRoomIndicator != null)
                txtRoomIndicator.text = view == RoomView.Hangar ? "Ангар" : (view == RoomView.DispatchConsole ? "Пульт" : "Верстак");

            bool showNav = view != RoomView.WorkbenchFocus;
            if (btnTurnLeft != null) btnTurnLeft.gameObject.SetActive(showNav);
            if (btnTurnRight != null) btnTurnRight.gameObject.SetActive(showNav);

            if (showNav)
            {
                string label = view == RoomView.Hangar ? "Пульт" : "Ангар";
                var lTxt = btnTurnLeft != null ? btnTurnLeft.GetComponentInChildren<Text>() : null;
                var rTxt = btnTurnRight != null ? btnTurnRight.GetComponentInChildren<Text>() : null;
                if (lTxt != null) lTxt.text = $"◄ {label} (Q)";
                if (rTxt != null) rTxt.text = $"{label} (E) ►";
            }

            if (mechSelectPanel != null) mechSelectPanel.SetActive(view == RoomView.Hangar);
            if (wbPanel != null) wbPanel.SetActive(view == RoomView.WorkbenchFocus);
            SetPrompt("");
        }

        private void UpdateMechInfo()
        {
            var mgr = HangarMechManager.Instance;
            if (mgr == null || mgr.CurrentMech == null || txtCurrentMech == null) return;

            var m = mgr.CurrentMech;
            if (m.isDeployed)
            {
                txtCurrentMech.text = $"{m.mechName} [{mgr.CurrentIndex + 1}/{mgr.Mechs.Count}] — в бою";
                if (btnRechargeBattery != null) btnRechargeBattery.gameObject.SetActive(false);
                return;
            }

            txtCurrentMech.text = $"{m.mechName} [{mgr.CurrentIndex + 1}/{mgr.Mechs.Count}]  •  ХП {m.HpPercent:F0}%  •  Бат. {m.currentEnergy:F0}%";
            if (btnRechargeBattery != null) btnRechargeBattery.gameObject.SetActive(m.currentEnergy < m.maxEnergy * 0.99f);
        }

        private void ShowVictory() => ShowEndPanel("Проект «Егерь» завершён", "Егерь собран и готов к выходу из ангара.");
        private void ShowDefeat() => ShowEndPanel("База разрушена", "Противник прорвал периметр обороны.");

        private void ShowEndPanel(string title, string sub)
        {
            if (victoryPanel == null) return;
            victoryPanel.SetActive(true);
            var t1 = victoryPanel.transform.Find("Txt_Vic1")?.GetComponent<Text>();
            var t2 = victoryPanel.transform.Find("Txt_Vic2")?.GetComponent<Text>();
            if (t1 != null) t1.text = title;
            if (t2 != null) t2.text = sub;
        }
    }
}
