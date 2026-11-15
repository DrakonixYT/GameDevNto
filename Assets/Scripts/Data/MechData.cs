using UnityEngine;

namespace HangarGame.Data
{
    [CreateAssetMenu(fileName = "NewMech", menuName = "HangarGame/Mech Data", order = 0)]
    public class MechData : ScriptableObject
    {
        public string mechId = "mech_titan_01";
        public string mechName = "Егерь 'Атлант-1'";
        
        [Header("Default Loadout")]
        public MechPartData defaultHead;
        public MechPartData defaultTorso;
        public MechPartData defaultLeftArm;  // e.g. Shield
        public MechPartData defaultRightArm; // e.g. Plasma Cannon
        public MechPartData defaultLegs;
    }
}
