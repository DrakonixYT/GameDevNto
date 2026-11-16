using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Data;
using HangarGame.Mechs;
using HangarGame.Repair;
using HangarGame.CameraControl;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Dispatch
{
    public class DispatchManager : MonoBehaviour
    {
        public static DispatchManager Instance { get; private set; }

        [Header("Threat Wave Config")]
        [SerializeField] private List<ThreatData> threatDatabase = new List<ThreatData>();
        [SerializeField] private float threatIntervalSeconds = 25f;

        [Header("State")]
        [SerializeField] private ThreatData currentThreat;
        [SerializeField] private bool hasActiveThreat = false;
        [SerializeField] private bool isMissionInProgress = false;

        [Header("Tactical Decision State (QTE)")]
        [SerializeField] private bool hasActiveTacticalPrompt = false;
        [SerializeField] private float tacticalPromptTimer = 0f;
        private bool tacticalSuccess = false;
        private int currentTacticalType = 0; // 0: Щит, 1: Критический залп, 2: Продувка

        public ThreatData CurrentThreat => currentThreat;
        public bool HasActiveThreat => hasActiveThreat;
        public bool IsMissionInProgress => isMissionInProgress;
        public bool HasActiveTacticalPrompt => hasActiveTacticalPrompt;
        public float TacticalPromptTimer => tacticalPromptTimer;

        private float waveTimer;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            waveTimer = 3f; // Первая угроза появляется почти сразу
        }

        private void Update()
        {
            if (!hasActiveThreat && !isMissionInProgress)
            {
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0f)
                {
                    SpawnNextThreat();
                    waveTimer = threatIntervalSeconds;
                }
            }

            // Обработка горячей клавиши (Space) для быстрого тактического ответа
            if (hasActiveTacticalPrompt && isMissionInProgress)
            {
                bool spacePressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
                }
#else
                spacePressed = Input.GetKeyDown(KeyCode.Space);
#endif
                if (spacePressed)
                {
                    ExecuteTacticalAction();
                }
            }
        }

        public void SpawnNextThreat()
        {
            if (isMissionInProgress)
            {
                Debug.LogWarning("[DispatchManager] Нельзя вызвать новую угрозу, пока идет текущий бой!");
                return;
            }

            if (threatDatabase.Count > 0)
            {
                int idx = Random.Range(0, threatDatabase.Count);
                currentThreat = threatDatabase[idx];
            }
            else
            {
                // Fallback runtime threat
                ThreatData fallback = ScriptableObject.CreateInstance<ThreatData>();
                fallback.threatName = "Штурмовой Мех 'Цербер'";
                fallback.incomingAttackType = DamageType.Physical;
                fallback.weakAgainst = EnemyAdaptationSystem.Instance != null 
                    ? EnemyAdaptationSystem.Instance.GetCounterWeakness() 
                    : DamageType.Thermal;
                fallback.basePower = 40f;
                fallback.rewardCredits = 250;
                fallback.rewardAlloy = 100;
                fallback.missionDurationSeconds = 5f;
                currentThreat = fallback;
            }

            // Учитываем адаптацию
            if (EnemyAdaptationSystem.Instance != null)
            {
                currentThreat.weakAgainst = EnemyAdaptationSystem.Instance.GetCounterWeakness();
            }

            hasActiveThreat = true;
            Debug.Log($"<color=red>[УГРОЗА ГОРОДУ]</color> Обнаружен: {currentThreat.threatName}! Атака: {currentThreat.incomingAttackType}, Уязвимость: {currentThreat.weakAgainst}");

            EventBus.TriggerThreatAppeared(
                currentThreat.threatName, 
                currentThreat.incomingAttackType, 
                currentThreat.weakAgainst, 
                currentThreat.basePower
            );
        }

        public void DeployAvailableMech()
        {
            MechInstance mech = FindObjectOfType<MechInstance>();
            if (mech == null)
            {
                Debug.LogError("[DispatchManager] Ошибка: Мех не найден на сцене!");
                return;
            }

            TryDeployMech(mech);
        }

        public bool TryDeployMech(MechInstance mech)
        {
            if (mech == null)
            {
                Debug.LogWarning("[DispatchManager] Не указан мех для отправки!");
                return false;
            }

            if (isMissionInProgress)
            {
                Debug.LogWarning("[DispatchManager] Мех уже выполняет боевую задачу в городе! Дождитесь возвращения.");
                return false;
            }

            if (!hasActiveThreat || currentThreat == null)
            {
                Debug.LogWarning("[DispatchManager] Нет активной угрозы! Нажмите кнопку 'Сканировать радар'.");
                return false;
            }

            if (mech.IsDeployed)
            {
                Debug.LogWarning("[DispatchManager] Мех уже отправлен на боевое дежурство!");
                return false;
            }

            if (RepairWorkbench.Instance != null && RepairWorkbench.Instance.HasPart)
            {
                Debug.LogWarning("[DispatchManager] Нельзя отправить робота в бой: деталь находится на верстаке!");
                EventBus.TriggerThreatResolved("⚠ ОШИБКА: Робот разобран! Установите модуль на стапель перед вылетом.");
                return false;
            }

            if (mech.HasMissingOrDetachedParts())
            {
                Debug.LogWarning("[DispatchManager] Нельзя отправить робота в бой: робот разобран!");
                EventBus.TriggerThreatResolved("⚠ ОШИБКА: Робот разобран! Не все боевые модули установлены на корпус.");
                return false;
            }

            StartCoroutine(MissionRoutine(mech, currentThreat));
            return true;
        }

        public void ExecuteTacticalAction()
        {
            if (!hasActiveTacticalPrompt || !isMissionInProgress) return;

            tacticalSuccess = true;
            hasActiveTacticalPrompt = false;
            EventBus.TriggerTacticalPromptCleared();

            if (currentTacticalType == 0)
            {
                HangarAudio.Instance?.PlayBolt();
                CameraShaker.Instance?.Shake(0.35f, 0.15f);
                Debug.Log("<color=green>[ТАКТИКА УСПЕХ]</color> Защитные экраны активированы! Урон снижен вдвое.");
            }
            else if (currentTacticalType == 1)
            {
                HangarAudio.Instance?.PlaySuccess();
                CameraShaker.Instance?.Shake(0.45f, 0.2f);
                Debug.Log("<color=green>[ТАКТИКА УСПЕХ]</color> Критический выстрел произведен! Уничтожение подтверждено.");
            }
            else
            {
                HangarAudio.Instance?.PlaySuccess();
                CameraShaker.Instance?.Shake(0.25f, 0.1f);
                Debug.Log("<color=green>[ТАКТИКА УСПЕХ]</color> Реактор охлажден! Избегнут внутренний взрыв.");
            }
        }

        private IEnumerator MissionRoutine(MechInstance mech, ThreatData threat)
        {
            isMissionInProgress = true;
            hasActiveThreat = false;
            hasActiveTacticalPrompt = false;
            tacticalSuccess = false;
            mech.SetDeployed(true);

            CameraShaker.Instance?.Shake(0.3f, 0.1f);
            HangarAudio.Instance?.PlayClick();

            Debug.Log($"<color=cyan>[БОЕВАЯ ОПЕРАЦИЯ]</color> {mech.MechName} вылетел на перехват: {threat.threatName}!");

            // Фаза 1 боя: сближение
            yield return new WaitForSeconds(1.8f);
            if (this == null) yield break;

            // Запуск интерактивного тактического решения (QTE)
            currentTacticalType = Random.Range(0, 3);
            string promptText = "";
            string buttonText = "";
            float qteDuration = 3.0f;

            switch (currentTacticalType)
            {
                case 0:
                    promptText = "«ВРАГ ЗАХВАТИЛ ЦЕЛЬ! ПОДНЯТЬ ЭНЕРГОЩИТЫ?»";
                    buttonText = "🛡 ПОДНЯТЬ ЩИТЫ! (ПРОБЕЛ)";
                    break;
                case 1:
                    promptText = "«УЯЗВИМОСТЬ ОТКРЫТА! ПРОИЗВЕСТИ КРИТИЧЕСКИЙ ЗАЛП?»";
                    buttonText = "⚡ КРИТИЧЕСКИЙ ВЫСТРЕЛ! (ПРОБЕЛ)";
                    break;
                case 2:
                    promptText = "«ПЕРЕГРЕВ СЕРВОПРИВОДОВ! ПРОИЗВЕСТИ ПРОДУВКУ?»";
                    buttonText = "❄ ПРОДУВКА РЕАКТОРА! (ПРОБЕЛ)";
                    break;
            }

            hasActiveTacticalPrompt = true;
            tacticalPromptTimer = qteDuration;
            EventBus.TriggerTacticalPromptAppeared(promptText, buttonText, qteDuration);
            HangarAudio.Instance?.PlayWarning();

            // Ожидание действия игрока
            while (tacticalPromptTimer > 0f && hasActiveTacticalPrompt)
            {
                tacticalPromptTimer -= Time.deltaTime;
                yield return null;
            }

            if (hasActiveTacticalPrompt)
            {
                hasActiveTacticalPrompt = false;
                EventBus.TriggerTacticalPromptCleared();
                Debug.Log("<color=yellow>[ТАКТИКА ПРОПУЩЕНА]</color> Командир не успел отдать приказ вовремя!");
            }

            // Фаза 2 боя: завершение
            yield return new WaitForSeconds(1.2f);
            if (this == null) yield break;

            // Расчет исхода боя
            bool hasShieldCounter = mech.HasDefenseAgainst(threat.incomingAttackType);
            bool hasAttackCounter = mech.HasAttackAgainst(threat.weakAgainst);

            float damageTaken = 0f;
            int finalCredits = threat.rewardCredits;
            int finalAlloy = threat.rewardAlloy;
            string battleLog = "";

            if (hasShieldCounter && hasAttackCounter)
            {
                damageTaken = Random.Range(5f, 15f);
            }
            else if (hasShieldCounter || hasAttackCounter)
            {
                damageTaken = Random.Range(25f, 42f);
            }
            else
            {
                damageTaken = Random.Range(50f, 70f);
                finalCredits = Mathf.RoundToInt(finalCredits * 0.7f);
            }

            // Учитываем тактическое решение игрока
            if (tacticalSuccess)
            {
                if (currentTacticalType == 0) // Щиты
                {
                    damageTaken *= 0.45f;
                    battleLog = $"Блестящая победа! Своевременные щиты спасли {mech.MechName}. Получено {damageTaken:F0} ед. урона. Награда: +{finalCredits} кр, +{finalAlloy} спл.";
                }
                else if (currentTacticalType == 1) // Крит выстрел
                {
                    finalCredits += 120;
                    finalAlloy += 60;
                    battleLog = $"Критическое попадание разнесло врага в щепки! Бонус к награде: +{finalCredits} кр, +{finalAlloy} спл. Урон меха: {damageTaken:F0} ед.";
                }
                else // Продувка
                {
                    damageTaken *= 0.7f;
                    finalAlloy += 40;
                    battleLog = $"Охлаждение предотвратило перегрузку систем {mech.MechName}! Урон: {damageTaken:F0} ед. Награда: +{finalCredits} кр, +{finalAlloy} спл.";
                }
            }
            else
            {
                battleLog = $"Бой окончен. {mech.MechName} получил {damageTaken:F0} ед. урона. Награда: +{finalCredits} кр, +{finalAlloy} спл.";
            }

            // Наносим повреждения меху
            mech.ApplyCombatDamage(damageTaken);
            mech.SetDeployed(false);

            // Начисляем ресурсы
            GameManager.Instance?.AddResources(finalCredits, finalAlloy);

            // Регистрируем адаптацию врагов
            if (EnemyAdaptationSystem.Instance != null && hasAttackCounter)
            {
                EnemyAdaptationSystem.Instance.RegisterVictoryWithAttackType(threat.weakAgainst);
            }

            isMissionInProgress = false;
            currentThreat = null;

            CameraShaker.Instance?.Shake(0.2f, 0.08f);
            Debug.Log($"<color=yellow>[ОТЧЕТ МИССИИ]</color> {battleLog}");
            EventBus.TriggerThreatResolved(battleLog);
        }
    }
}
