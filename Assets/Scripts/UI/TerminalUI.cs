using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Repair;
using HangarGame.Mechs;

namespace HangarGame.UI
{
    public class TerminalUI : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private Text txtThreatTitle;
        [SerializeField] private Text txtThreatDetails;
        [SerializeField] private Text txtMechStatus;
        [SerializeField] private Text txtMissionLog;
        [SerializeField] private Text txtAdaptation;
        [SerializeField] private Text txtJaegerInfo;

        [Header("Buttons")]
        [SerializeField] private Button btnDeploy;
        [SerializeField] private Text txtDeployLabel;
        [SerializeField] private Button btnScan;
        [SerializeField] private Button btnBuildJaeger;

        [Header("QTE Panel")]
        [SerializeField] private GameObject qtePanel;
        [SerializeField] private Text txtQteTimer;
        [SerializeField] private Button btnQteAction;

        private void Start()
        {
            if (btnDeploy != null) btnDeploy.onClick.AddListener(() => { HangarAudioLight.Instance?.PlayClick(); DispatchSystem.Instance?.TryDeploy(); });
            if (btnScan != null) btnScan.onClick.AddListener(() => { HangarAudioLight.Instance?.PlayClick(); DispatchSystem.Instance?.SpawnThreat(); });
            if (btnBuildJaeger != null) btnBuildJaeger.onClick.AddListener(() => { HangarAudioLight.Instance?.PlayClick(); JaegerProject.Instance?.StartNextStage(); });
            if (btnQteAction != null) btnQteAction.onClick.AddListener(() => DispatchSystem.Instance?.ExecuteQte());
        }

        private void Update()
        {
            var dispatch = DispatchSystem.Instance;
            if (dispatch == null) return;

            // Threat info
            if (dispatch.HasActiveThreat && dispatch.CurrentThreat != null)
            {
                if (txtThreatTitle != null) { txtThreatTitle.text = $"🚨 ТРЕВОГА: {dispatch.CurrentThreat.name.ToUpper()}"; txtThreatTitle.color = Color.red; }
                if (txtThreatDetails != null) txtThreatDetails.text = $"Атака: {dispatch.CurrentThreat.attackType}  |  Слабость: {dispatch.CurrentThreat.weakness}  |  Мощь: {dispatch.CurrentThreat.power:F0}";
            }
            else
            {
                if (txtThreatTitle != null) { txtThreatTitle.text = "СЕКТОР-7: РАДАР СКАНИРУЕТ ПЕРИМЕТР"; txtThreatTitle.color = Color.cyan; }
                if (txtThreatDetails != null) txtThreatDetails.text = dispatch.IsMissionInProgress ? "Мех ведет операцию в городе..." : "Угроз нет. Проводите ТО робота на верстаке.";
            }

            // Mech status
            bool onBench = RepairWorkbench.Instance != null && RepairWorkbench.Instance.HasItem;
            bool missing = CombatMech.Instance != null && CombatMech.Instance.HasMissingOrDetachedParts();
            if (txtMechStatus != null)
            {
                if (dispatch.IsMissionInProgress) { txtMechStatus.text = "⚡ БОЙ В ГОРОДЕ В ПРОЦЕССЕ..."; txtMechStatus.color = Color.yellow; }
                else if (onBench || missing) { txtMechStatus.text = "⚠ ВНИМАНИЕ: РОБОТ РАЗОБРАН НА ВЕРСТАКЕ! ВЫЛЕТ ЗАПРЕЩЕН!"; txtMechStatus.color = new Color(1f, 0.25f, 0.25f); }
                else { txtMechStatus.text = "✔ СТАТУС: БОЕГОТОВЕН (Все 5 модулей смонтированы)"; txtMechStatus.color = new Color(0.2f, 1f, 0.4f); }
            }

            // Buttons
            if (btnDeploy != null)
            {
                bool can = dispatch.CanDeploy();
                btnDeploy.interactable = can;
                if (txtDeployLabel != null)
                {
                    if (dispatch.IsMissionInProgress) txtDeployLabel.text = "БОЙ В ГОРОДЕ...";
                    else if (!dispatch.HasActiveThreat) txtDeployLabel.text = "НЕТ ЦЕЛЕЙ НА РАДАРЕ";
                    else if (!can) txtDeployLabel.text = "⚠ РОБОТ НЕ СОБРАН";
                    else txtDeployLabel.text = "⚔ ВЫЛЕТ НА ПЕРЕХВАТ (SPACE)";
                }
            }

            // Jaeger section
            if (txtJaegerInfo != null && JaegerProject.Instance != null)
                txtJaegerInfo.text = JaegerProject.Instance.GetNextStageInfo();
            if (btnBuildJaeger != null && JaegerProject.Instance != null)
                btnBuildJaeger.interactable = JaegerProject.Instance.CanStartNext();

            // Logs & QTE
            if (txtMissionLog != null) txtMissionLog.text = dispatch.LastMissionLog;
            if (txtAdaptation != null) txtAdaptation.text = dispatch.AdaptationAlert;

            if (qtePanel != null)
            {
                qtePanel.SetActive(dispatch.HasQtePrompt);
                if (dispatch.HasQtePrompt && txtQteTimer != null)
                    txtQteTimer.text = $"⏱ Время: {dispatch.QteTimer:F1}с";
            }
        }
    }
}
