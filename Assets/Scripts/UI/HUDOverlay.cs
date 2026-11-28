using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Interaction;
using HangarGame.Mechs;
using HangarGame.Repair;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HangarGame.UI
{
    public class HUDOverlay : MonoBehaviour
    {
        [Header("Ссылки")]
        public GameManager game;
        public HangarMechManager hangar;
        public RepairWorkbench workbench;
        public JaegerProject jaeger;
        public RoomCamera roomCamera;
        public PlayerRaycaster raycaster;

        [Header("Виджеты")]
        public Text txtResources;
        public Text txtRoomIndicator;
        public Button btnTurnLeft;
        public Button btnTurnRight;
        public GameObject mechSelectPanel;
        public Button btnPrevMech;
        public Button btnNextMech;
        public Button btnRechargeBattery;
        public Text txtCurrentMech;
        public Text txtHoverPrompt;
        public GameObject wbPanel;
        public Text txtWbTitle;
        public Text txtWbInfo;
        public GameObject victoryPanel;
        public Text txtVicTitle;
        public Text txtVicSub;
        public Button btnRestart;

        private string lastPrompt;

        private void Awake()
        {
            game ??= GameManager.Instance ?? FindAnyObjectByType<GameManager>();
            hangar ??= game?.hangar ?? FindAnyObjectByType<HangarMechManager>();
            workbench ??= game?.workbench ?? FindAnyObjectByType<RepairWorkbench>();
            jaeger ??= game?.jaeger ?? FindAnyObjectByType<JaegerProject>();
            roomCamera ??= game?.roomCamera ?? FindAnyObjectByType<RoomCamera>();
            raycaster ??= game?.raycaster ?? FindAnyObjectByType<PlayerRaycaster>();

            if (btnTurnLeft != null) { btnTurnLeft.onClick.RemoveAllListeners(); btnTurnLeft.onClick.AddListener(() => roomCamera?.TurnLeft()); }
            if (btnTurnRight != null) { btnTurnRight.onClick.RemoveAllListeners(); btnTurnRight.onClick.AddListener(() => roomCamera?.TurnRight()); }
            if (btnPrevMech != null) { btnPrevMech.onClick.RemoveAllListeners(); btnPrevMech.onClick.AddListener(() => { game?.PlaySfx(Sfx.Click); hangar?.PrevMech(); }); }
            if (btnNextMech != null) { btnNextMech.onClick.RemoveAllListeners(); btnNextMech.onClick.AddListener(() => { game?.PlaySfx(Sfx.Click); hangar?.NextMech(); }); }
            if (btnRechargeBattery != null) { btnRechargeBattery.onClick.RemoveAllListeners(); btnRechargeBattery.onClick.AddListener(() => { hangar?.RechargeCurrentMech(); UpdateMechInfo(); }); }
            if (btnRestart != null) { btnRestart.onClick.RemoveAllListeners(); btnRestart.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)); }
        }

        private void Start() => HookEvents(true);
        private void OnEnable() => HookEvents(true);
        private void OnDisable() => HookEvents(false);

        private void HookEvents(bool subscribe)
        {
            if (game != null)
            {
                game.OnResourcesChanged -= OnResourcesChanged;
                game.OnBaseHpChanged -= OnBaseHpChanged;
                game.OnJaegerProgressChanged -= OnJaegerChanged;
                game.OnViewChanged -= HandleViewChanged;
                game.OnGameWon -= ShowVictory;
                game.OnGameLost -= ShowDefeat;

                if (subscribe)
                {
                    game.OnResourcesChanged += OnResourcesChanged;
                    game.OnBaseHpChanged += OnBaseHpChanged;
                    game.OnJaegerProgressChanged += OnJaegerChanged;
                    game.OnViewChanged += HandleViewChanged;
                    game.OnGameWon += ShowVictory;
                    game.OnGameLost += ShowDefeat;
                }
            }

            if (hangar != null)
            {
                hangar.OnMechSwitched -= HandleMechSwitched;
                if (subscribe) hangar.OnMechSwitched += HandleMechSwitched;
            }

            if (workbench != null)
            {
                workbench.OnWorkChanged -= UpdateWorkbenchInfo;
                if (subscribe) workbench.OnWorkChanged += UpdateWorkbenchInfo;
            }

            if (subscribe) RefreshAll();
        }

        private void Update()
        {
            if (game != null && game.CurrentView == RoomView.Hangar)
            {
                if (GameInput.PrevMech) hangar?.PrevMech();
                if (GameInput.NextMech) hangar?.NextMech();
            }

            string prompt = raycaster != null ? raycaster.CurrentPrompt : "";
            if (prompt != lastPrompt)
            {
                lastPrompt = prompt;
                SetPrompt(prompt);
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

            RoomView view = game != null ? game.CurrentView : RoomView.Hangar;
            txtHoverPrompt.text = view switch
            {
                RoomView.Hangar => "A / D — смена робота  •  Клик по детали — ремонт  •  Q / E — пульт",
                RoomView.DispatchConsole => "Клик по радару — высадка / приказ  •  Tab — оружейная  •  Q / E — ангар",
                _ => "ПКМ — вращение  •  Колесо — масштаб  •  Пробел — готово"
            };
        }

        public void RefreshAll()
        {
            if (game != null)
            {
                RefreshTopBar();
                HandleViewChanged(game.CurrentView);
            }
            UpdateMechInfo();
            UpdateWorkbenchInfo();
            SetPrompt("");
        }

        private void OnResourcesChanged(int c, int a) => RefreshTopBar();
        private void OnBaseHpChanged(float hp, float max) => RefreshTopBar();
        private void OnJaegerChanged(float p) => RefreshTopBar();
        private void HandleMechSwitched(int idx) => UpdateMechInfo();

        private void RefreshTopBar()
        {
            if (txtResources == null || game == null) return;
            string jStr = jaeger != null ? $"{jaeger.CurrentStage}/{jaeger.TotalStages}" : "0/5";
            txtResources.text = $"Кредиты: {game.Credits}   |   Сплав: {game.Alloy}   |   База: {game.BaseHp:F0}%   |   Егерь: {jStr}";
        }

        private void HandleViewChanged(RoomView view)
        {
            if (txtRoomIndicator != null)
            {
                txtRoomIndicator.text = view switch
                {
                    RoomView.Hangar => "Ангар",
                    RoomView.DispatchConsole => "Пульт",
                    _ => "Верстак"
                };
            }

            bool showNav = view != RoomView.WorkbenchFocus;
            if (btnTurnLeft != null) btnTurnLeft.gameObject.SetActive(showNav);
            if (btnTurnRight != null) btnTurnRight.gameObject.SetActive(showNav);

            if (showNav)
            {
                string label = view == RoomView.Hangar ? "Пульт" : "Ангар";
                var lTxt = btnTurnLeft?.GetComponentInChildren<Text>();
                var rTxt = btnTurnRight?.GetComponentInChildren<Text>();
                if (lTxt != null) lTxt.text = $"◄ {label} (Q)";
                if (rTxt != null) rTxt.text = $"{label} (E) ►";
            }

            if (mechSelectPanel != null) mechSelectPanel.SetActive(view == RoomView.Hangar);
            if (wbPanel != null) wbPanel.SetActive(view == RoomView.WorkbenchFocus);
            if (view == RoomView.WorkbenchFocus) UpdateWorkbenchInfo();
            SetPrompt("");
        }

        private void UpdateWorkbenchInfo()
        {
            if (workbench == null || !workbench.HasItem) return;
            if (txtWbTitle != null)
            {
                string action = workbench.CurrentJaegerModule != null ? "Сборка Егеря" : "Ремонт";
                txtWbTitle.text = $"{action}: {workbench.CurrentItemName}";
            }
            if (txtWbInfo != null) txtWbInfo.text = workbench.RepairStepInfo;
        }

        private void UpdateMechInfo()
        {
            if (hangar?.CurrentMech == null || txtCurrentMech == null) return;

            var m = hangar.CurrentMech;
            if (m.isDeployed)
            {
                txtCurrentMech.text = $"{m.mechName} [{hangar.CurrentIndex + 1}/{hangar.Mechs.Count}] — в бою";
                if (btnRechargeBattery != null) btnRechargeBattery.gameObject.SetActive(false);
                return;
            }

            txtCurrentMech.text = $"{m.mechName} [{hangar.CurrentIndex + 1}/{hangar.Mechs.Count}]  •  ХП {m.HpPercent:F0}%  •  Бат. {m.currentEnergy:F0}%";
            if (btnRechargeBattery != null) btnRechargeBattery.gameObject.SetActive(m.currentEnergy < m.maxEnergy * 0.99f);
        }

        private void ShowVictory() => ShowEndPanel("Проект «Егерь» завершён", "Егерь собран и готов к выходу из ангара.");
        private void ShowDefeat() => ShowEndPanel("База разрушена", "Противник прорвал периметр обороны.");

        private void ShowEndPanel(string title, string sub)
        {
            if (victoryPanel == null) return;
            victoryPanel.SetActive(true);
            if (txtVicTitle != null) txtVicTitle.text = title;
            if (txtVicSub != null) txtVicSub.text = sub;
        }
    }
}
