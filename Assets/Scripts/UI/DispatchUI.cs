using UnityEngine;
using UnityEngine.UI;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        [Header("Tactical In-Battle Decision UI")]
        [SerializeField] private GameObject tacticalPromptPanel;
        [SerializeField] private Text txtTacticalDirective;
        [SerializeField] private Text txtTacticalTimer;
        [SerializeField] private Button btnTacticalAction;

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
            EventBus.OnRoomViewChanged += HandleRoomViewChanged;
            EventBus.OnTacticalPromptAppeared += HandleTacticalPromptAppeared;
            EventBus.OnTacticalPromptCleared += HandleTacticalPromptCleared;

            UpdateUIState();
        }

        private void OnDestroy()
        {
            EventBus.OnThreatAppeared -= HandleThreatAppeared;
            EventBus.OnThreatResolved -= HandleThreatResolved;
            EventBus.OnEnemyAdapted -= HandleEnemyAdapted;
            EventBus.OnRoomViewChanged -= HandleRoomViewChanged;
            EventBus.OnTacticalPromptAppeared -= HandleTacticalPromptAppeared;
            EventBus.OnTacticalPromptCleared -= HandleTacticalPromptCleared;
        }

        public void FindAndWireElements()
        {
            if (targetMech == null)
            {
                targetMech = FindObjectOfType<MechInstance>();
            }

            if (btnDeployMech == null)
            {
                Button[] allButtons = GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.name == "Btn_Deploy")
                    {
                        btnDeployMech = b;
                        break;
                    }
                }
            }

            if (btnDeployMech != null)
            {
                btnDeployMech.onClick.RemoveAllListeners();
                btnDeployMech.onClick.AddListener(OnDeployClicked);
            }

            if (activeThreatCard == null)
            {
                Transform card = transform.Find("ThreatCard");
                if (card != null) activeThreatCard = card.gameObject;
            }

            if (missionProgressCard == null)
            {
                Transform mCard = transform.Find("MissionProgressCard");
                if (mCard != null) missionProgressCard = mCard.gameObject;
            }

            if (btnTacticalAction == null)
            {
                Button[] allButtons = GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.name == "Btn_TacticalAction")
                    {
                        btnTacticalAction = b;
                        break;
                    }
                }
            }

            if (btnTacticalAction != null)
            {
                btnTacticalAction.onClick.RemoveAllListeners();
                btnTacticalAction.onClick.AddListener(OnTacticalActionClicked);
            }

            if (tacticalPromptPanel == null && missionProgressCard != null)
            {
                Transform tacPanel = missionProgressCard.transform.Find("TacticalPromptPanel");
                if (tacPanel != null) tacticalPromptPanel = tacPanel.gameObject;
            }
        }

        private void Update()
        {
            if (targetMech == null)
            {
                targetMech = FindObjectOfType<MechInstance>();
            }

            bool inBattle = DispatchManager.Instance != null && DispatchManager.Instance.IsMissionInProgress;

            if (missionProgressCard != null)
            {
                missionProgressCard.SetActive(inBattle);
            }

            if (btnDeployMech != null && DispatchManager.Instance != null)
            {
                bool canDeploy = !inBattle && 
                                 DispatchManager.Instance.HasActiveThreat && 
                                 (targetMech != null && !targetMech.IsDeployed);
                btnDeployMech.interactable = canDeploy;
            }

            // Обновление таймера тактического QTE решения
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

            // Горячие клавиши (Space или Enter) для отправки в бой при открытом терминале
            if (GameManager.Instance != null && GameManager.Instance.CurrentView == RoomView.DispatchConsole)
            {
                bool enterPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    enterPressed = Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
                }
#else
                enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
                if (enterPressed && btnDeployMech != null && btnDeployMech.interactable && !inBattle)
                {
                    OnDeployClicked();
                }
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

            if (tacticalPromptPanel != null)
            {
                tacticalPromptPanel.SetActive(false);
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
                Text bText = btnTacticalAction.GetComponentInChildren<Text>();
                if (bText != null) bText.text = btnText;
            }
        }

        private void HandleTacticalPromptCleared()
        {
            if (tacticalPromptPanel != null)
            {
                tacticalPromptPanel.SetActive(false);
            }
        }

        private void HandleRoomViewChanged(RoomView view)
        {
            if (dispatchContainer != null)
            {
                dispatchContainer.SetActive(view == RoomView.DispatchConsole);
            }
        }

        public void OnDeployClicked()
        {
            Debug.Log("<color=cyan>[DispatchUI]</color> Нажата кнопка 'ОТПРАВИТЬ ЕГЕРЯ В БОЙ'!");

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
            else
            {
                Debug.LogError("[DispatchUI] Ошибка: targetMech или DispatchManager.Instance равен null!");
            }
        }

        public void OnTacticalActionClicked()
        {
            Debug.Log("<color=yellow>[DispatchUI]</color> Выполнен тактический приказ командира!");
            DispatchManager.Instance?.ExecuteTacticalAction();
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
