using System.Collections.Generic;
using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Repair;
using UnityEngine;

namespace HangarGame.Dispatch
{
    public class DispatchSystem : MonoBehaviour
    {
        public static DispatchSystem Instance { get; private set; }

        [Header("Волны")]
        public float firstEnemyDelay = 9f;
        public float minSpawnInterval = 18f;
        public float maxSpawnInterval = 27f;
        public float workbenchTimerMultiplier = 0.55f;
        public int maxActiveEnemies = 4;

        [Header("Прорыв к базе")]
        public int breachCreditsPenalty = 50;
        public int breachAlloyPenalty = 25;
        public float breachBaseDamage = 25f;

        [Header("Дистанция боя на радаре")]
        public float combatEngageRange = 185f;
        public float holdToFightRange = 145f;
        public float enemyAggroRange = 210f;

        public List<EnemyDot> Enemies { get; } = new List<EnemyDot>();
        public string LastBattleLog { get; private set; } = "Радар чист.";
        public int SelectedMechId { get; set; } = -1;
        public bool IsDeployMode { get; set; }
        public Vector2 BasePosition => basePos;

        private readonly Vector2 basePos = new Vector2(0f, -240f);
        private float spawnTimer;
        private int enemyCounter;

        private static readonly string[] EnemyNames = { "Разведчик", "Огнемётчик", "Штурмовик", "Кислотник" };
        private static readonly DamageType[] DamageTypes = { DamageType.Physical, DamageType.Plasma, DamageType.Thermal, DamageType.Corrosive };

        private void Awake()
        {
            Instance = this;
            spawnTimer = firstEnemyDelay;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && (gm.IsGameWon || gm.IsGameLost)) return;

            float tick = Time.deltaTime;
            if (gm != null && gm.CurrentView == RoomView.WorkbenchFocus)
                tick *= workbenchTimerMultiplier;

            spawnTimer -= tick;
            if (spawnTimer <= 0f)
            {
                float paceScale = Mathf.Max(0.65f, 1f - enemyCounter * 0.035f);
                spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval) * paceScale;

                if (Enemies.Count < maxActiveEnemies)
                {
                    SpawnEnemy();
                    if (enemyCounter >= 5 && Enemies.Count < maxActiveEnemies && Random.value < 0.30f)
                        SpawnEnemy();
                }
            }

            UpdateEnemies();
            UpdateMechs();
        }

        public void SpawnEnemy()
        {
            enemyCounter++;
            int tier = Mathf.Min(4, (enemyCounter - 1) / 3);
            int typeIdx = Random.Range(0, DamageTypes.Length);
            DamageType atk = DamageTypes[typeIdx];
            DamageType weak = DamageTypes[(typeIdx + 1) % DamageTypes.Length];

            var enemy = new EnemyDot
            {
                id = enemyCounter,
                name = $"{EnemyNames[Random.Range(0, EnemyNames.Length)]} #{enemyCounter}",
                position = new Vector2(Random.Range(-420f, 420f), 240f),
                attackType = atk,
                weakAgainst = weak,
                hp = Random.Range(115f, 165f) + tier * 35f,
                speed = Random.Range(12f, 19f) + tier * 1.5f,
                attackPower = Random.Range(12f, 18f) + tier * 2.5f,
                rewardCredits = Random.Range(85, 120) + tier * 15,
                rewardAlloy = Random.Range(35, 55) + tier * 8
            };
            enemy.maxHp = enemy.hp;

            Enemies.Add(enemy);
            HangarAlarmLight.Instance?.SetAlarm(true);
            LastBattleLog = $"{enemy.name} на подходе (уязв.: {weak.ToRu()}).";
        }

        private void UpdateEnemies()
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var enemy = Enemies[i];
                if (enemy.hp <= 0f)
                {
                    Enemies.RemoveAt(i);
                    GameManager.Instance?.AddResources(enemy.rewardCredits, enemy.rewardAlloy);
                    LastBattleLog = $"{enemy.name} уничтожен (+{enemy.rewardCredits} кр, +{enemy.rewardAlloy} м).";
                    HangarAudio.Play(Sfx.Success);
                    if (Enemies.Count == 0) HangarAlarmLight.Instance?.SetAlarm(false);
                    continue;
                }

                MechUnit targetMech = GetClosestMech(enemy.position, enemyAggroRange);
                Vector2 target = targetMech != null ? targetMech.battlefieldPos : basePos;
                enemy.position = Vector2.MoveTowards(enemy.position, target, enemy.speed * Time.deltaTime);

                if (Vector2.Distance(enemy.position, basePos) < 15f)
                {
                    Enemies.RemoveAt(i);
                    OnEnemyBreached(enemy);
                }
            }
        }

        private void OnEnemyBreached(EnemyDot enemy)
        {
            var gm = GameManager.Instance;
            int lostC = 0, lostA = 0;
            if (gm != null)
                (lostC, lostA) = gm.ApplyBreachPenalty(breachCreditsPenalty, breachAlloyPenalty, breachBaseDamage);

            RoomCamera.Instance?.Shake(0.35f, 0.16f);
            HangarAudio.Play(Sfx.Alarm);
            float baseHp = gm != null ? gm.BaseHp : 0f;
            LastBattleLog = $"Прорыв {enemy.name}: база {baseHp:F0}%, -{lostC} кр, -{lostA} м";
            if (Enemies.Count == 0) HangarAlarmLight.Instance?.SetAlarm(false);
        }

        private void UpdateMechs()
        {
            var mgr = HangarMechManager.Instance;
            if (mgr == null) return;

            foreach (var mech in mgr.Mechs)
            {
                if (mech == null || !mech.isDeployed) continue;

                float drain = (mech.isShieldActive ? 6.5f : 1.8f) * Time.deltaTime;
                mech.currentEnergy = Mathf.Max(0f, mech.currentEnergy - drain);
                if (mech.currentEnergy <= 0f)
                {
                    mech.isShieldActive = false;
                    if (!mech.isReturning)
                    {
                        RecallMech(mech.id);
                        LastBattleLog = $"{mech.mechName}: разряд батареи, возврат.";
                        HangarAudio.Play(Sfx.Alarm);
                    }
                }

                EnemyDot enemy = GetClosestEnemy(mech.battlefieldPos, combatEngageRange);
                bool holdToFight = !mech.isReturning
                    && enemy != null
                    && Vector2.Distance(mech.battlefieldPos, enemy.position) <= holdToFightRange
                    && Vector2.Distance(mech.targetPos, enemy.position) <= 220f;

                if (!holdToFight && Vector2.Distance(mech.battlefieldPos, mech.targetPos) > 4f)
                    mech.battlefieldPos = Vector2.MoveTowards(mech.battlefieldPos, mech.targetPos, mech.moveSpeed * Time.deltaTime);

                if (mech.isReturning && Vector2.Distance(mech.battlefieldPos, basePos) < 15f)
                {
                    mech.SetDeployed(false);
                    mech.isReturning = false;
                    mech.SyncPartsFromHp(mech.GetTotalHpPercent());
                    mgr.UpdatePodiumDisplay();
                    HangarAudio.Play(Sfx.Bolt);
                    LastBattleLog = $"{mech.mechName} в ангаре.";
                    continue;
                }

                if (enemy != null)
                {
                    var right = mech.GetPartBySlot(PartSlot.RightArm);
                    float atkMult = right?.Data != null
                        ? (right.Data.attackType == enemy.weakAgainst ? 1.55f : 0.90f)
                        : 1f;

                    var left = mech.GetPartBySlot(PartSlot.LeftArm);
                    float defMult = (left?.Data != null && left.Data.defenseType == enemy.attackType) ? 0.60f : 1f;

                    enemy.hp -= mech.attackPower * atkMult * Time.deltaTime;
                    mech.ApplyDamage(enemy.attackPower * 0.62f * defMult * Time.deltaTime);

                    if (mech.currentHp <= 6f && !mech.isReturning)
                    {
                        RecallMech(mech.id);
                        LastBattleLog = $"{mech.mechName}: критический урон, отход.";
                        HangarAudio.Play(Sfx.Alarm);
                    }
                }
            }
        }

        public bool DeployMech(int mechId, Vector2 destination)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.CanDeploy()) return false;

            var wb = RepairWorkbench.Instance;
            if (wb != null && wb.CurrentPart != null && mech.parts.Contains(wb.CurrentPart))
                return false;

            mech.SyncHpFromParts();
            mech.isReturning = false;
            mech.SetDeployed(true, basePos);
            mech.targetPos = destination;
            SelectedMechId = mechId;
            IsDeployMode = false;
            HangarAudio.Play(Sfx.Deploy);
            LastBattleLog = $"{mech.mechName} высажен.";
            return true;
        }

        public void RecallMech(int mechId)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.isDeployed || mech.isReturning) return;

            mech.isReturning = true;
            mech.targetPos = basePos;
            HangarAudio.Play(Sfx.Recall);
            LastBattleLog = $"{mech.mechName} отходит на базу.";
        }

        public void ToggleMechShield(int mechId)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.isDeployed) return;

            mech.ToggleShield();
            HangarAudio.Play(Sfx.Shield);
            LastBattleLog = mech.isShieldActive ? $"{mech.mechName}: щит включен." : $"{mech.mechName}: щит выключен.";
        }

        public void OrderMechMove(int mechId, Vector2 radarPos)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.isDeployed) return;

            mech.isReturning = false;
            mech.targetPos = radarPos;
            HangarAudio.Play(Sfx.Click);
            LastBattleLog = $"{mech.mechName} меняет позицию.";
        }

        public EnemyDot GetClosestEnemy(Vector2 pos, float maxRange)
        {
            EnemyDot best = null;
            float minDist = maxRange;
            foreach (var e in Enemies)
            {
                float d = Vector2.Distance(pos, e.position);
                if (d < minDist) { minDist = d; best = e; }
            }
            return best;
        }

        public MechUnit GetClosestMech(Vector2 pos, float maxRange)
        {
            var mgr = HangarMechManager.Instance;
            if (mgr == null) return null;

            MechUnit best = null;
            float minDist = maxRange;
            foreach (var m in mgr.Mechs)
            {
                if (m == null || !m.isDeployed) continue;
                float d = Vector2.Distance(pos, m.battlefieldPos);
                if (d < minDist) { minDist = d; best = m; }
            }
            return best;
        }
    }
}
