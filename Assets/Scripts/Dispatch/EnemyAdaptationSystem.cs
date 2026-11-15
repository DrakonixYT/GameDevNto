using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;

namespace HangarGame.Dispatch
{
    public class EnemyAdaptationSystem : MonoBehaviour
    {
        public static EnemyAdaptationSystem Instance { get; private set; }

        [Header("Adaptation Thresholds")]
        [SerializeField] private int thresholdWinsToAdapt = 2;

        private Dictionary<DamageType, int> playerSuccessfulAttackCounts = new Dictionary<DamageType, int>();
        private DamageType currentAdaptedResistance = DamageType.Physical;
        public DamageType CurrentAdaptedResistance => currentAdaptedResistance;

        private void Awake()
        {
            Instance = this;
            foreach (DamageType dt in System.Enum.GetValues(typeof(DamageType)))
            {
                playerSuccessfulAttackCounts[dt] = 0;
            }
        }

        public void RegisterVictoryWithAttackType(DamageType typeUsed)
        {
            playerSuccessfulAttackCounts[typeUsed]++;

            // Если игрок злоупотребляет одной тактикой — враги мутируют/адаптируются
            if (playerSuccessfulAttackCounts[typeUsed] >= thresholdWinsToAdapt && currentAdaptedResistance != typeUsed)
            {
                currentAdaptedResistance = typeUsed;
                playerSuccessfulAttackCounts[typeUsed] = 0;

                string adaptationMessage = GetAdaptationDescription(typeUsed);
                Debug.Log($"<color=orange>[АДАПТАЦИЯ ВРАГОВ]</color> {adaptationMessage}");
                EventBus.TriggerEnemyAdapted(adaptationMessage);
            }
        }

        private string GetAdaptationDescription(DamageType resistantTo)
        {
            switch (resistantTo)
            {
                case DamageType.Physical:
                    return "Враги нарастили тяжелую броню! Физические пули бесполезны. Используйте ТЕРМО или ПЛАЗМУ!";
                case DamageType.Thermal:
                    return "Враги покрылись огнеупорной чешуей! Огнеметы неэффективны. Используйте КИНЕТИКУ или КИСЛОТУ!";
                case DamageType.Plasma:
                    return "Враги установили энергопоглотители! Плазма отражается. Требуется ТЯЖЕЛЫЙ КИНЕТИЧЕСКИЙ урон!";
                default:
                    return "Противник сменил сигнатуру брони и тактику!";
            }
        }

        public DamageType GetCounterWeakness()
        {
            // Уязвимость врага становится противоположной их текущей адаптации
            switch (currentAdaptedResistance)
            {
                case DamageType.Physical: return DamageType.Thermal;
                case DamageType.Thermal: return DamageType.Physical;
                case DamageType.Plasma: return DamageType.Physical;
                default: return DamageType.Plasma;
            }
        }
    }
}
