using UnityEngine;
using HangarGame.Core;

namespace HangarGame.Data
{
    [CreateAssetMenu(fileName = "NewMechPart", menuName = "HangarGame/Mech Part Data", order = 1)]
    public class MechPartData : ScriptableObject
    {
        [Header("Identity")]
        public string partId = "part_arm_shield";
        public string partName = "Тяжелый энерго-щит 'Бастион'";
        public PartSlot slot = PartSlot.LeftArm;

        [Header("Combat Attributes")]
        public DamageType resistanceType = DamageType.Physical;
        public DamageType attackType = DamageType.Physical;
        public float attackPower = 10f;
        public float defensePower = 40f;

        [Header("Durability & Repair")]
        public float maxDurability = 100f;
        public int repairCreditCost = 50;
        public int repairAlloyCost = 25;

        [Header("Description")]
        [TextArea(2, 4)]
        public string description = "Эффективен против огнестрельного и кинетического оружия врагов.";
    }
}
