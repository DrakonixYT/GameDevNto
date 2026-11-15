using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Data;
using HangarGame.Mechs;

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

        public ThreatData CurrentThreat => currentThreat;
        public bool HasActiveThreat => hasActiveThreat;
        public bool IsMissionInProgress => isMissionInProgress;

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

            StartCoroutine(MissionRoutine(mech, currentThreat));
            return true;
        }

        private IEnumerator MissionRoutine(MechInstance mech, ThreatData threat)
        {
            isMissionInProgress = true;
            hasActiveThreat = false;
            mech.SetDeployed(true);

            Debug.Log($"<color=cyan>[БОЕВАЯ ОПЕРАЦИЯ]</color> {mech.MechName} вылетел на перехват: {threat.threatName}!");

            float duration = threat.missionDurationSeconds > 0 ? threat.missionDurationSeconds : 5f;
            yield return new WaitForSeconds(duration);

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
                battleLog = $"Блестящая победа! {mech.MechName} идеально нейтрализовал угрозу. Минимум урона ({damageTaken:F0} ед.). Награда: +{finalCredits} кр, +{finalAlloy} спл.";
            }
            else if (hasShieldCounter || hasAttackCounter)
            {
                damageTaken = Random.Range(25f, 45f);
                battleLog = $"Победа с повреждениями. {mech.MechName} выполнил задачу, но получил {damageTaken:F0} ед. урона. Награда: +{finalCredits} кр, +{finalAlloy} спл.";
            }
            else
            {
                damageTaken = Random.Range(55f, 75f);
                finalCredits = Mathf.RoundToInt(finalCredits * 0.6f);
                battleLog = $"Тяжелейший бой! Неподходящее вооружение привело к критическим повреждениям ({damageTaken:F0} ед.). Награда урезана: +{finalCredits} кр.";
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

            Debug.Log($"<color=yellow>[ОТЧЕТ МИССИИ]</color> {battleLog}");
            EventBus.TriggerThreatResolved(battleLog);
        }
    }
}
