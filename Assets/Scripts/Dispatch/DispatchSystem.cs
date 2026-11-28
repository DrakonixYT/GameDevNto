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

        [Header("Баланс волн")]
        [SerializeField] private float firstEnemyDelay = 9f;
        [SerializeField] private float minSpawnInterval = 18f;
        [SerializeField] private float maxSpawnInterval = 27f;
        [SerializeField] private float workbenchTimerMultiplier = 0.55f;
        [SerializeField] private int maxActiveEnemies = 4;

        [Header("Штраф за прорыв к базе")]
        [SerializeField] private int breachCreditsPenalty = 50;
        [SerializeField] private int breachAlloyPenalty = 25;
        [SerializeField] private float breachBaseDamage = 25f;

        public List<EnemyDot> Enemies { get; } = new List<EnemyDot>();
        public string LastBattleLog { get; private set; } = "База на боевом дежурстве. Радар чист.";
        public int SelectedMechId { get; set; } = -1;
        public bool IsDeployMode { get; set; }
        public Vector2 BasePosition => basePos;

        private readonly Vector2 basePos = new Vector2(0f, -240f);
        private float spawnTimer;
        private int enemyCounter;

        private void Awake()
        {
            Instance = this;
            spawnTimer = firstEnemyDelay;
        }

        private void Update()
        {
            if (GameManager.Instance != null && (GameManager.Instance.IsGameWon || GameManager.Instance.IsGameLost))
                return;

            float tick = Time.deltaTime;
            if (GameManager.Instance != null && GameManager.Instance.CurrentView == RoomView.WorkbenchFocus)
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
            UpdateMechsCombat();
        }

        public void SpawnEnemy()
        {
            enemyCounter++;
            int waveTier = Mathf.Min(4, (enemyCounter - 1) / 3);
            string[] names = { "Кайдзю-Дрон", "Шипохвост", "Осадный Краб", "Био-Скорпион" };
            DamageType[] types = { DamageType.Physical, DamageType.Plasma, DamageType.Thermal, DamageType.Corrosive };

            DamageType atk = types[Random.Range(0, types.Length)];
            DamageType weak = types[(System.Array.IndexOf(types, atk) + 1) % types.Length];

            var enemy = new EnemyDot
            {
                id = enemyCounter,
                name = $"{names[Random.Range(0, names.Length)]} #{enemyCounter}",
                position = new Vector2(Random.Range(-420f, 420f), 240f),
                attackType = atk,
                weakAgainst = weak,
                hp = Random.Range(115f, 165f) + waveTier * 35f,
                speed = Random.Range(12f, 19f) + waveTier * 1.5f,
                attackPower = Random.Range(12f, 18f) + waveTier * 2.5f,
                rewardCredits = Random.Range(85, 120) + waveTier * 15,
                rewardAlloy = Random.Range(35, 55) + waveTier * 8
            };
            enemy.maxHp = enemy.hp;

            Enemies.Add(enemy);
            HangarAlarmLight.Instance?.SetAlarm(true);
            LastBattleLog = $"ВНИМАНИЕ! Радар засек {enemy.name} (Атк: {atk}, уязвим к {weak}).";
        }

        private void UpdateEnemies()
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var enemy = Enemies[i];
                if (enemy.hp <= 0f)
                {
                    Enemies.RemoveAt(i);
                    OnEnemyDefeated(enemy);
                    continue;
                }

                Vector2 target = basePos;
                MechUnit closest = GetClosestMech(enemy.position, 130f);
                if (closest != null) target = closest.battlefieldPos;

                enemy.position = Vector2.MoveTowards(enemy.position, target, enemy.speed * Time.deltaTime);

                if (Vector2.Distance(enemy.position, basePos) < 15f)
                {
                    Enemies.RemoveAt(i);
                    OnEnemyBreached(enemy);
                    continue;
                }
            }
        }

        private void OnEnemyBreached(EnemyDot enemy)
        {
            int lostC = 0;
            int lostA = 0;
            GameManager.Instance?.ApplyBreachPenalty(breachCreditsPenalty, breachAlloyPenalty, breachBaseDamage, out lostC, out lostA);
            RoomCamera.Instance?.Shake(0.35f, 0.16f);
            HangarAudio.Instance?.PlayAlarm();
            float baseHp = GameManager.Instance != null ? GameManager.Instance.BaseHp : 0f;
            LastBattleLog = $"ПРОРЫВ ПЕРИМЕТРА! {enemy.name} ударил по Цитадели (База: {baseHp:F0}%, -{lostC}¢, -{lostA}т)!";
            if (Enemies.Count == 0) HangarAlarmLight.Instance?.SetAlarm(false);
        }

        private void UpdateMechsCombat()
        {
            if (HangarMechManager.Instance == null) return;

            foreach (var mech in HangarMechManager.Instance.Mechs)
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
                        LastBattleLog = $"Разряд батареи {mech.mechName}! Экстренный возврат на базу.";
                        HangarAudio.Instance?.PlayAlarm();
                    }
                }

                if (Vector2.Distance(mech.battlefieldPos, mech.targetPos) > 4f)
                    mech.battlefieldPos = Vector2.MoveTowards(mech.battlefieldPos, mech.targetPos, mech.moveSpeed * Time.deltaTime);

                if (mech.isReturning && Vector2.Distance(mech.battlefieldPos, basePos) < 15f)
                {
                    mech.SetDeployed(false);
                    mech.isReturning = false;
                    mech.SyncPartsFromHp(mech.GetTotalHpPercent());
                    HangarMechManager.Instance?.UpdatePodiumDisplay();
                    HangarAudio.Instance?.PlayBolt();
                    LastBattleLog = $"{mech.mechName} вернулся в ангар. Осмотрите сломанные модули.";
                    continue;
                }

                EnemyDot targetEnemy = GetClosestEnemy(mech.battlefieldPos, 125f);
                if (targetEnemy != null)
                {
                    float atkMult = GetAttackMultiplier(mech, targetEnemy);
                    float defMult = GetDefenseMultiplier(mech, targetEnemy);

                    targetEnemy.hp -= mech.attackPower * atkMult * Time.deltaTime;
                    mech.ApplyDamage(targetEnemy.attackPower * 0.62f * defMult * Time.deltaTime);

                    if (mech.currentHp <= 6f && !mech.isReturning)
                    {
                        RecallMech(mech.id);
                        LastBattleLog = $"Критический урон {mech.mechName}! Экстренная эвакуация в ангар.";
                        HangarAudio.Instance?.PlayAlarm();
                    }
                }
            }
        }

        private float GetAttackMultiplier(MechUnit mech, EnemyDot enemy)
        {
            var right = mech.GetPartBySlot(PartSlot.RightArm);
            if (right != null && right.Data != null)
                return right.Data.attackType == enemy.weakAgainst ? 1.55f : 0.90f;
            return 1f;
        }

        private float GetDefenseMultiplier(MechUnit mech, EnemyDot enemy)
        {
            var left = mech.GetPartBySlot(PartSlot.LeftArm);
            if (left != null && left.Data != null && left.Data.defenseType == enemy.attackType)
                return 0.60f;
            return 1f;
        }

        private void OnEnemyDefeated(EnemyDot enemy)
        {
            GameManager.Instance?.AddResources(enemy.rewardCredits, enemy.rewardAlloy);
            LastBattleLog = $"{enemy.name} уничтожен! Награда: +{enemy.rewardCredits}¢, +{enemy.rewardAlloy}т.";
            HangarAudio.Instance?.PlaySuccess();
            if (Enemies.Count == 0) HangarAlarmLight.Instance?.SetAlarm(false);
        }

        public bool DeployMech(int mechId, Vector2 destination)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.CanDeploy()) return false;

            if (RepairWorkbench.Instance != null && RepairWorkbench.Instance.HasItem && RepairWorkbench.Instance.CurrentPart != null)
            {
                if (mech.parts.Contains(RepairWorkbench.Instance.CurrentPart)) return false;
            }

            mech.SyncHpFromParts();
            mech.isReturning = false;
            mech.SetDeployed(true, basePos);
            mech.targetPos = destination;
            SelectedMechId = mechId;
            IsDeployMode = false;
            HangarAudio.Instance?.PlayDeploy();
            LastBattleLog = $"{mech.mechName} высажен в сектор [{destination.x:F0}, {destination.y:F0}].";
            return true;
        }

        public void RecallMech(int mechId)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.isDeployed || mech.isReturning) return;

            mech.isReturning = true;
            mech.targetPos = basePos;
            HangarAudio.Instance?.PlayRecall();
            LastBattleLog = $"{mech.mechName} отступает на базу для ремонта.";
        }

        public void ToggleMechShield(int mechId)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.isDeployed) return;

            mech.ToggleShield();
            HangarAudio.Instance?.PlayShield();
            LastBattleLog = mech.isShieldActive
                ? $"{mech.mechName}: ЭНЕРГОЩИТ ВКЛЮЧЕН (-65% урона)."
                : $"{mech.mechName}: энергощит выключен.";
        }

        public void OrderMechMove(int mechId, Vector2 radarPos)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.isDeployed) return;

            mech.isReturning = false;
            mech.targetPos = radarPos;
            HangarAudio.Instance?.PlayClick();
            LastBattleLog = $"{mech.mechName}: передислокация в [{radarPos.x:F0}, {radarPos.y:F0}].";
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
            if (HangarMechManager.Instance == null) return null;
            MechUnit best = null;
            float minDist = maxRange;
            foreach (var m in HangarMechManager.Instance.Mechs)
            {
                if (m == null || !m.isDeployed) continue;
                float d = Vector2.Distance(pos, m.battlefieldPos);
                if (d < minDist) { minDist = d; best = m; }
            }
            return best;
        }
    }
}
