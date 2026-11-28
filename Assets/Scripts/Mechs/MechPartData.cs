using System;
using HangarGame.Core;

namespace HangarGame.Mechs
{
    [Serializable]
    public class MechPartData
    {
        public string partName;
        public PartSlot slot;
        public DamageType attackType;
        public float attackPower;
        public DamageType defenseType;
        public float defensePower;
        public int repairCredits = 40;
        public int repairAlloy = 20;
    }
}
