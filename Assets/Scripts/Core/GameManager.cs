using UnityEngine;

namespace HangarGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Starting Economy")]
        [SerializeField] private int initialCredits = 500;
        [SerializeField] private int initialAlloy = 200;

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Peaceful;
        [SerializeField] private RoomView currentView = RoomView.Hangar;

        public int Credits { get; private set; }
        public int Alloy { get; private set; }
        public float JaegerProgress { get; private set; } = 0f;
        public GameState CurrentState => currentState;
        public RoomView CurrentView => currentView;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Credits = initialCredits;
            Alloy = initialAlloy;
        }

        private void Start()
        {
            EventBus.TriggerResourcesUpdated(Credits, Alloy);
            EventBus.TriggerJaegerProgressUpdated(JaegerProgress);
            SwitchRoomView(RoomView.Hangar);
        }

        public void AddResources(int creditsToAdd, int alloyToAdd)
        {
            Credits = Mathf.Max(0, Credits + creditsToAdd);
            Alloy = Mathf.Max(0, Alloy + alloyToAdd);
            EventBus.TriggerResourcesUpdated(Credits, Alloy);
        }

        public bool TrySpendResources(int creditCost, int alloyCost)
        {
            if (Credits >= creditCost && Alloy >= alloyCost)
            {
                Credits -= creditCost;
                Alloy -= alloyCost;
                EventBus.TriggerResourcesUpdated(Credits, Alloy);
                return true;
            }
            return false;
        }

        public void InvestInJaeger(float progressAmount = 0.25f, int creditCost = 300, int alloyCost = 150)
        {
            if (JaegerProgress >= 1f) return;

            if (TrySpendResources(creditCost, alloyCost))
            {
                JaegerProgress = Mathf.Clamp01(JaegerProgress + progressAmount);
                EventBus.TriggerJaegerProgressUpdated(JaegerProgress);

                if (JaegerProgress >= 1f)
                {
                    currentState = GameState.Victory;
                    EventBus.TriggerGameWon();
                    Debug.Log("<color=green>[GameManager] Проект 'ЕГЕРЬ' завершен! Город спасен!</color>");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] Недостаточно ресурсов для строительства Егеря!");
            }
        }

        public void SwitchRoomView(RoomView newView)
        {
            currentView = newView;
            EventBus.TriggerRoomViewChanged(newView);
        }

        public void SetGameState(GameState newState)
        {
            currentState = newState;
        }
    }
}
