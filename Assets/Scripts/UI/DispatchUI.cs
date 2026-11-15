using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;

namespace HangarGame.UI
{
    public class DispatchUI : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private GameObject dispatchContainer;
        [SerializeField] private GameObject activeThreatCard;
        [SerializeField] private GameObject missionProgressCard;

        [Header("Threat UI Elements")]
        [SerializeField] private Text txtThreatName;
        [SerializeField] private Text txtThreatDetails;
        [SerializeField] private Button btnDeployMech;

        [Header("Adaptation & Logs")]
        [SerializeField] private Text txtAdaptationAlert;
        [SerializeField] private Text txtMissionLog;

        [Header("Assigned Mech Reference")]
        [SerializeField] private MechInstance targetMech;

        private void Start()
        {
            if (targetMech == null)
            {
                targetMech = FindObjectOfType<MechInstance>();
            }

            if (btnDeployMech != null)
            {
                btnDeployMech.onClick.AddListener(OnDeployClicked);
            }

            EventBus.OnThreatAppeared += HandleThreatAppeared;
            EventBus.OnThreatResolved += HandleThreatResolved;
            EventBus.OnEnemyAdapted += HandleEnemyAdapted;
            EventBus.OnRoomViewChanged += HandleRoomViewChanged;

            UpdateUIState();
        }

        private void OnDestroy()
        {
            EventBus.OnThreatAppeared -= HandleThreatAppeared;
            EventBus.OnThreatResolved -= HandleThreatResolved;
            EventBus.OnEnemyAdapted -= HandleEnemyAdapted;
            EventBus.OnRoomViewChanged -= HandleRoomViewChanged;
        }

        private void Update()
        {
            if (DispatchManager.Instance != null && missionProgressCard != null)
            {
                missionProgressCard.SetActive(DispatchManager.Instance.IsMissionInProgress);
            }
        }

        private void HandleThreatAppeared(string tName, DamageType attackType, DamageType weakness, float power)
        {
            if (activeThreatCard != null)
            {
                activeThreatCard.SetActive(true);
            }

            if (txtThreatName != null)
            {
                txtThreatName.text = $"ТРЕВОГА: {tName.ToUpper()}";
            }

            if (txtThreatDetails != null)
            {
                txtThreatDetails.text = $"Тип атаки врага: {attackType} (Нужен щит!)\n" +
                                       $"Уязвимость врага: {weakness}\n" +
                                       $"Мощь: {power:F0} ед.";
            }

            if (btnDeployMech != null)
            {
                btnDeployMech.interactable = true;
            }
        }

        private void HandleThreatResolved(string logMessage)
        {
            if (activeThreatCard != null)
            {
                activeThreatCard.SetActive(false);
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
                txtAdaptationAlert.text = $"⚠ ВНИМАНИЕ: {adaptationNote}";
            }
        }

        private void HandleRoomViewChanged(RoomView view)
        {
            // Можно показывать экран терминала только при взгляде на консоль (180 градусов)
            if (dispatchContainer != null)
            {
                dispatchContainer.SetActive(view == RoomView.DispatchConsole);
            }
        }

        private void OnDeployClicked()
        {
            if (targetMech == null)
            {
                targetMech = FindObjectOfType<MechInstance>();
            }

            if (targetMech != null && DispatchManager.Instance != null)
            {
                bool success = DispatchManager.Instance.TryDeployMech(targetMech);
                if (success && btnDeployMech != null)
                {
                    btnDeployMech.interactable = false;
                }
            }
        }

        private void UpdateUIState()
        {
            if (activeThreatCard != null)
            {
                activeThreatCard.SetActive(DispatchManager.Instance != null && DispatchManager.Instance.HasActiveThreat);
            }
        }
    }
}
