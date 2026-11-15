using System;
using HangarGame.Core;

namespace HangarGame.Core
{
    public static class EventBus
    {
        // Камера и комнаты
        public static event Action<RoomView> OnRoomViewChanged;
        public static void TriggerRoomViewChanged(RoomView view) => OnRoomViewChanged?.Invoke(view);

        // Диспетчерская и угрозы
        public static event Action<string, DamageType, DamageType, float> OnThreatAppeared; 
        public static void TriggerThreatAppeared(string name, DamageType attackType, DamageType weakness, float power) 
            => OnThreatAppeared?.Invoke(name, attackType, weakness, power);

        public static event Action<string> OnThreatResolved;
        public static void TriggerThreatResolved(string logMessage) => OnThreatResolved?.Invoke(logMessage);

        public static event Action<string> OnEnemyAdapted;
        public static void TriggerEnemyAdapted(string adaptationNote) => OnEnemyAdapted?.Invoke(adaptationNote);

        // Тактические приказы в реальном времени во время боя
        public static event Action<string, string, float> OnTacticalPromptAppeared; // directiveText, actionButtonText, duration
        public static void TriggerTacticalPromptAppeared(string directiveText, string actionButtonText, float duration) 
            => OnTacticalPromptAppeared?.Invoke(directiveText, actionButtonText, duration);

        public static event Action OnTacticalPromptCleared;
        public static void TriggerTacticalPromptCleared() => OnTacticalPromptCleared?.Invoke();

        // Ремонт и детали
        public static event Action<object> OnPartSentToWorkbench;
        public static void TriggerPartSentToWorkbench(object part) => OnPartSentToWorkbench?.Invoke(part);

        public static event Action<object> OnPartRepairFinished;
        public static void TriggerPartRepairFinished(object part) => OnPartRepairFinished?.Invoke(part);

        // Ресурсы и прогресс Егеря
        public static event Action<int, int> OnResourcesUpdated; // credits, alloy
        public static void TriggerResourcesUpdated(int credits, int alloy) => OnResourcesUpdated?.Invoke(credits, alloy);

        public static event Action<float> OnJaegerProgressUpdated; // 0..1
        public static void TriggerJaegerProgressUpdated(float progress) => OnJaegerProgressUpdated?.Invoke(progress);

        public static event Action OnGameWon;
        public static void TriggerGameWon() => OnGameWon?.Invoke();

        public static void ClearAll()
        {
            OnRoomViewChanged = null;
            OnThreatAppeared = null;
            OnThreatResolved = null;
            OnEnemyAdapted = null;
            OnPartSentToWorkbench = null;
            OnPartRepairFinished = null;
            OnResourcesUpdated = null;
            OnJaegerProgressUpdated = null;
            OnGameWon = null;
            OnTacticalPromptAppeared = null;
            OnTacticalPromptCleared = null;
        }
    }
}
