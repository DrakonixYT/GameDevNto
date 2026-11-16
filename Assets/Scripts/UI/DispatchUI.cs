using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
using HangarGame.Repair;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.UI
{
    public class DispatchUI : MonoBehaviour
    {
        [Header("Terminal Header & State")]
        [SerializeField] private Text txtTerminalStatus;
        [SerializeField] private Text txtThreatName;
        [SerializeField] private Text txtThreatDetails;
        [SerializeField] private Text txtMechStatusAlert;

        [Header("Tactical Decision State (QTE)")]
        [SerializeField] private GameObject tacticalPromptPanel;
        [SerializeField] private Text txtTacticalDirective;
        [SerializeField] private Text txtTacticalTimer;
        [SerializeField] private Button btnTacticalAction;

        [Header("Dispatch Controls")]
        [SerializeField] private Button btnDeployMech;
        [SerializeField] private Button btnScanRadar;
        [SerializeField] private Text txtDeployBtnLabel;

        [Header("Jaeger Construction Section on Terminal")]
        [SerializeField] private Text txtJaegerProjectInfo;
        [SerializeField] private Button btnBuildJaegerModule;

        [Header("Logs & Radio")]
        [SerializeField] private Text txtMissionLog;
        [SerializeField] private Text txtAdaptationAlert;

        [Header("Assigned Mech Reference")]
        [SerializeField] private MechInstance targetMech;

        private void Awake()
        {
            FindAndWireElements();
        }

        private void Start()
        {
            FindAndWireElements();

            EventBus.OnThreatAppeared += HandleThreatAppeared;
            EventBus.OnThreatResolved += HandleThreatResolved;
            EventBus.OnEnemyAdapted += HandleEnemyAdapted;
            EventBus.OnTacticalPromptAppeared += HandleTacticalPromptAppeared;
            EventBus.OnTacticalPromptCleared += HandleTacticalPromptCleared;
            EventBus.OnJaegerProgressUpdated += HandleJaegerProgressUpdated;

            UpdateUIState();
        }

        private void OnDestroy()
        {
            EventBus.OnThreatAppeared -= HandleThreatAppeared;
            EventBus.OnThreatResolved -= HandleThreatResolved;
            EventBus.OnEnemyAdapted -= HandleEnemyAdapted;
            EventBus.OnTacticalPromptAppeared -= HandleTacticalPromptAppeared;
            EventBus.OnTacticalPromptCleared -= HandleTacticalPromptCleared;
            EventBus.OnJaegerProgressUpdated -= HandleJaegerProgressUpdated;
        }

        public void FindAndWireElements()
        {
            if (targetMech == null)
            {
                targetMech = FindObjectOfType<MechInstance>();
            }

            if (btnDeployMech == null) btnDeployMech = transform.Find("Btn_Deploy")?.GetComponent<Button>();
            if (btnScanRadar == null) btnScanRadar = transform.Find("Btn_ScanRadar")?.GetComponent<Button>();
            if (btnTacticalAction == null) btnTacticalAction = transform.Find("TacticalPromptPanel/Btn_TacticalAction")?.GetComponent<Button>();
            if (btnBuildJaegerModule == null) btnBuildJaegerModule = transform.Find("Btn_BuildJaegerModule")?.GetComponent<Button>();

            if (btnDeployMech != null)
            {
                btnDeployMech.onClick.RemoveAllListeners();
                btnDeployMech.onClick.AddListener(OnDeployClicked);
            }

            if (btnScanRadar != null)
            {
                btnScanRadar.onClick.RemoveAllListeners();
                btnScanRadar.onClick.AddListener(OnScanRadarClicked);
            }

            if (btnTacticalAction != null)
            {
                btnTacticalAction.onClick.RemoveAllListeners();
                btnTacticalAction.onClick.AddListener(OnTacticalActionClicked);
            }

            if (btnBuildJaegerModule != null)
            {
                btnBuildJaegerModule.onClick.RemoveAllListeners();
                btnBuildJaegerModule.onClick.AddListener(OnBuildJaegerModuleClicked);
            }
        }

        private void Update()
        {
            if (targetMech == null)
            {
                targetMech = FindObjectOfType<MechInstance>();
            }

            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool inBattle = DispatchManager.Instance != null && DispatchManager.Instance.IsMissionInProgress;
            bool hasThreat = DispatchManager.Instance != null && DispatchManager.Instance.HasActiveThreat;

            bool partOnWorkbench = RepairWorkbench.Instance != null && RepairWorkbench.Instance.HasPart;
            bool mechDismantled = targetMech != null && targetMech.HasMissingOrDetachedParts();
            bool mechReady = targetMech != null && !targetMech.IsDeployed && !partOnWorkbench && !mechDismantled;

            // Статус готовности меха
            if (txtMechStatusAlert != null)
            {
                if (inBattle)
                {
                    txtMechStatusAlert.text = "⚡ МЕХ ВЕДЕТ БОЕВЫЕ ДЕЙСТВИЯ В ГОРОДЕ...";
                    txtMechStatusAlert.color = Color.yellow;
                }
                else if (partOnWorkbench || mechDismantled)
                {
                    txtMechStatusAlert.text = "⚠ ВНИМАНИЕ: РОБОТ РАЗОБРАН НА ВЕРСТАКЕ! ВЫЛЕТ ЗАПРЕЩЕН!";
                    txtMechStatusAlert.color = new Color(1f, 0.25f, 0.25f);
                }
                else
                {
                    txtMechStatusAlert.text = "✔ СТАТУС: БОЕГОТОВЕН (Все 5 модулей смонтированы)";
                    txtMechStatusAlert.color = new Color(0.2f, 1f, 0.4f);
                }
            }

            // Кнопка вылета
            if (btnDeployMech != null)
            {
                bool canDeploy = !inBattle && hasThreat && mechReady;
                btnDeployMech.interactable = canDeploy;

                if (txtDeployBtnLabel != null)
                {
                    if (inBattle) txtDeployBtnLabel.text = "МИССИЯ В ПРОЦЕССЕ...";
                    else if (!hasThreat) txtDeployBtnLabel.text = "НЕТ АКТИВНЫХ ЦЕЛЕЙ";
                    else if (!mechReady) txtDeployBtnLabel.text = "⚠ РОБОТ НЕ СОБРАН";
                    else txtDeployBtnLabel.text = "⚔ ВЫЛЕТ НА ПЕРЕХВАТ (SPACE)";
                }
            }

            // Тактическое QTE решение
            if (DispatchManager.Instance != null && DispatchManager.Instance.HasActiveTacticalPrompt)
            {
                if (tacticalPromptPanel != null && !tacticalPromptPanel.activeSelf)
                {
                    tacticalPromptPanel.SetActive(true);
                }

                if (txtTacticalTimer != null)
                {
                    txtTacticalTimer.text = $"⏱ Время на приказ: {DispatchManager.Instance.TacticalPromptTimer:F1}с";
                }
            }
            else
            {
                if (tacticalPromptPanel != null && tacticalPromptPanel.activeSelf)
                {
                    tacticalPromptPanel.SetActive(false);
                }
            }

            // Обновление секции Егеря на терминале
            UpdateJaegerSection();
        }

        private void UpdateJaegerSection()
        {
            if (JaegerProjectManager.Instance == null) return;

            if (txtJaegerProjectInfo != null)
            {
                int cur = JaegerProjectManager.Instance.CurrentStageIndex;
                int total = JaegerProjectManager.Instance.TotalStages;
                if (JaegerProjectManager.Instance.IsCompleted)
                {
                    txtJaegerProjectInfo.text = "ПРОЕКТ 'ЕГЕРЬ': 100% [ПОЛНОСТЬЮ БОЕСПОСОБЕН]";
                }
                else
                {
                    var stage = JaegerProjectManager.Instance.CurrentStage;
                    string sName = stage != null ? stage.stageName : $"Модуль {cur + 1}";
                    int cCost = stage != null ? stage.creditCost : 200;
                    int aCost = stage != null ? stage.alloyCost : 100;
                    txtJaegerProjectInfo.text = $"ПРОЕКТ 'ЕГЕРЬ' ({cur}/{total}): {sName}\n[Требуется: {cCost}¢, {aCost}т]";
                }
            }

            if (btnBuildJaegerModule != null)
            {
                btnBuildJaegerModule.interactable = JaegerProjectManager.Instance.CanStartNextStage();
            }
        }

        private void HandleThreatAppeared(string tName, DamageType attackType, DamageType weakness, float power)
        {
            if (txtTerminalStatus != null)
            {
                txtTerminalStatus.text = "🚨 ТАКТИЧЕСКАЯ ТРЕВОГА // СЕКТОР-7 🚨";
                txtTerminalStatus.color = Color.red;
            }

            if (txtThreatName != null)
            {
                txtThreatName.text = $"ОБНАРУЖЕН: {tName.ToUpper()}";
            }

            if (txtThreatDetails != null)
            {
                txtThreatDetails.text = $"Атака врага: {attackType}  |  Уязвимость: {weakness}  |  Мощь: {power:F0}";
            }
        }

        private void HandleThreatResolved(string logMessage)
        {
            if (txtTerminalStatus != null)
            {
                txtTerminalStatus.text = "СЕКТОР СТАБИЛЕН // ОЖИДАНИЕ УГРОЗ";
                txtTerminalStatus.color = Color.cyan;
            }

            if (txtThreatName != null)
            {
                txtThreatName.text = "РАДАР СКАНИРУЕТ ГОРОДСКОЙ ПЕРИМЕТР";
            }

            if (txtThreatDetails != null)
            {
                txtThreatDetails.text = "Сигнатур кайдзю не обнаружено. Проводите ТО роботов.";
            }

            if (txtMissionLog != null)
            {
                txtMissionLog.text = logMessage;
            }
        }

        private void HandleEnemyAdapted(string adaptationNote)
        {
            if (txtAdaptationAlert != null)
            {
                txtAdaptationAlert.gameObject.SetActive(true);
                txtAdaptationAlert.text = $"⚠ АДАПТАЦИЯ ВРАГА: {adaptationNote}";
            }
        }

        private void HandleTacticalPromptAppeared(string directive, string btnText, float duration)
        {
            if (tacticalPromptPanel != null)
            {
                tacticalPromptPanel.SetActive(true);
            }

            if (txtTacticalDirective != null)
            {
                txtTacticalDirective.text = directive;
            }

            if (btnTacticalAction != null)
            {
                Text t = btnTacticalAction.GetComponentInChildren<Text>();
                if (t != null) t.text = btnText;
            }
        }

        private void HandleTacticalPromptCleared()
        {
            if (tacticalPromptPanel != null)
            {
                tacticalPromptPanel.SetActive(false);
            }
        }

        private void HandleJaegerProgressUpdated(float progress)
        {
            UpdateJaegerSection();
        }

        public void OnDeployClicked()
        {
            HangarAudio.Instance?.PlayClick();

            if (DispatchManager.Instance == null) return;

            if (targetMech == null)
            {
                targetMech = FindObjectOfType<MechInstance>();
            }

            if (targetMech != null)
            {
                DispatchManager.Instance.TryDeployMech(targetMech);
            }
        }

        public void OnScanRadarClicked()
        {
            HangarAudio.Instance?.PlayClick();
            DispatchManager.Instance?.SpawnNextThreat();
        }

        public void OnTacticalActionClicked()
        {
            HangarAudio.Instance?.PlayClick();
            DispatchManager.Instance?.ExecuteTacticalAction();
        }

        public void OnBuildJaegerModuleClicked()
        {
            HangarAudio.Instance?.PlayClick();
            if (JaegerProjectManager.Instance != null)
            {
                JaegerProjectManager.Instance.StartAssemblingCurrentStage();
            }
        }
    }
}
