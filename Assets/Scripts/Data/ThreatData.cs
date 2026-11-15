using UnityEngine;
using HangarGame.Core;

namespace HangarGame.Data
{
    [CreateAssetMenu(fileName = "NewThreat", menuName = "HangarGame/Threat Data", order = 2)]
    public class ThreatData : ScriptableObject
    {
        [Header("Identity")]
        public string threatId = "threat_kaiju_spitter";
        public string threatName = "Штурмовой дрон-опустошитель";
        [TextArea(2, 4)]
        public string description = "Атакует пулеметами и тяжелыми снарядами. Уязвим к плазменным клинкам и щитам.";

        [Header("Combat Profile")]
        public DamageType incomingAttackType = DamageType.Physical;
        public DamageType weakAgainst = DamageType.Plasma;
        public float basePower = 50f;
        public float missionDurationSeconds = 6f;

        [Header("Rewards on Win")]
        public int rewardCredits = 200;
        public int rewardAlloy = 80;
    }
}
