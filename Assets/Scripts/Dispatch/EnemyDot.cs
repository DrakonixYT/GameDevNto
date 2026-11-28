using HangarGame.Core;
using UnityEngine;

namespace HangarGame.Dispatch
{
    public class EnemyDot
    {
        public int id;
        public string name;
        public Vector2 position;
        public DamageType attackType;
        public DamageType weakAgainst;
        public float hp = 70f;
        public float maxHp = 70f;
        public float speed = 13f;
        public float attackPower = 9f;
        public int rewardCredits = 95;
        public int rewardAlloy = 45;
    }
}
