using System;
using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Repair;

namespace HangarGame.Dispatch
{
    public class EnemyDot
    {
        public int id;
        public string name;
        public Vector2 position;
        public float hp = 60f;
        public float maxHp = 60f;
        public float speed = 18f;
        public float attackPower = 10f;
        public int rewardCredits = 80;
        public int rewardAlloy = 35;
    }

    public class DispatchSystem : MonoBehaviour
    {
        public static DispatchSystem Instance { get; private set; }

        public List<EnemyDot> Enemies { get; } = new List<EnemyDot>();
        public string LastBattleLog { get; private set; } = "База на боевом дежурстве. Радар чист.";
        public int SelectedMechId { get; set; } = -1;
        public bool IsDeployMode { get; set; } = false;

        private float spawnTimer = 3f;
        private int enemyCounter = 0;
        private readonly Vector2 basePos = new Vector2(0f, -240f);

        private void Awake() => Instance = this;

        private void Update()
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = UnityEngine.Random.Range(10f, 15f);
                if (Enemies.Count < 6) SpawnEnemy();
            }

            UpdateEnemies();
            UpdateMechsCombat();
        }

        public void SpawnEnemy()
        {
            enemyCounter++;
            string[] names = { "Кайдзю-Дрон", "Шипохвост", "Осадный Краб", "Био-Скорпион" };
            var enemy = new EnemyDot
            {
                id = enemyCounter,
                name = $"{names[UnityEngine.Random.Range(0, names.Length)]} #{enemyCounter}",
                position = new Vector2(UnityEngine.Random.Range(-450f, 450f), 240f),
                hp = UnityEngine.Random.Range(50f, 85f),
                speed = UnityEngine.Random.Range(14f, 22f),
                attackPower = UnityEngine.Random.Range(8f, 14f),
                rewardCredits = UnityEngine.Random.Range(70, 120),
                rewardAlloy = UnityEngine.Random.Range(30, 55)
            };
            enemy.maxHp = enemy.hp;
            Enemies.Add(enemy);
            HangarAudioLight.Instance?.SetAlarm(true);
            LastBattleLog = $"ВНИМАНИЕ! Радар засек {enemy.name}!";
        }

        private void UpdateEnemies()
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var enemy = Enemies[i];
                if (enemy.hp <= 0f)
                {
                    OnEnemyDefeated(enemy);
                    Enemies.RemoveAt(i);
                    continue;
                }

                Vector2 target = basePos;
                MechUnit closestMech = GetClosestMech(enemy.position, 130f);
                if (closestMech != null) target = closestMech.battlefieldPos;

                enemy.position = Vector2.MoveTowards(enemy.position, target, enemy.speed * Time.deltaTime);

                if (Vector2.Distance(enemy.position, basePos) < 15f)
                {
                    enemy.position = new Vector2(UnityEngine.Random.Range(-400f, 400f), 230f);
                    HangarAudioLight.Instance?.PlayBolt();
                    LastBattleLog = $"⚠ {enemy.name} атаковал периметр базы!";
                }
            }
        }

        private void UpdateMechsCombat()
        {
            if (HangarMechManager.Instance == null) return;

            foreach (var mech in HangarMechManager.Instance.Mechs)
            {
                if (!mech.isDeployed) continue;

                if (Vector2.Distance(mech.battlefieldPos, mech.targetPos) > 4f)
                {
                    mech.battlefieldPos = Vector2.MoveTowards(mech.battlefieldPos, mech.targetPos, mech.moveSpeed * Time.deltaTime);
                }

                EnemyDot targetEnemy = GetClosestEnemy(mech.battlefieldPos, 120f);
                if (targetEnemy != null)
                {
                    targetEnemy.hp -= mech.attackPower * Time.deltaTime;
                    mech.ApplyDamage(targetEnemy.attackPower * 0.4f * Time.deltaTime);

                    if (mech.currentHp <= 5f)
                    {
                        RecallMech(mech.id);
                        LastBattleLog = $"🚨 Критический урон {mech.mechName}! Экстренная эвакуация в ангар!";
                        HangarAudioLight.Instance?.PlayAlarm();
                    }
                }
            }
        }

        private void OnEnemyDefeated(EnemyDot enemy)
        {
            GameManager.Instance?.AddResources(enemy.rewardCredits, enemy.rewardAlloy);
            LastBattleLog = $"✔ {enemy.name} уничтожен! Получено: +{enemy.rewardCredits}¢, +{enemy.rewardAlloy}т.";
            HangarAudioLight.Instance?.PlayWeld();
            if (Enemies.Count == 0) HangarAudioLight.Instance?.SetAlarm(false);
        }

        public bool DeployMech(int mechId, Vector2 radarPos)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.CanDeploy()) return false;

            if (RepairWorkbench.Instance != null && RepairWorkbench.Instance.HasItem && RepairWorkbench.Instance.CurrentPart != null)
            {
                if (mech.parts.Contains(RepairWorkbench.Instance.CurrentPart)) return false;
            }

            mech.SetDeployed(true, radarPos);
            HangarAudioLight.Instance?.PlaySuccess();
            LastBattleLog = $"🚀 {mech.mechName} высажен в сектор [{radarPos.x:F0}, {radarPos.y:F0}]!";
            return true;
        }

        public void RecallMech(int mechId)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.isDeployed) return;

            mech.SetDeployed(false);
            HangarMechManager.Instance?.UpdatePodiumDisplay();
            HangarAudioLight.Instance?.PlayBolt();
            LastBattleLog = $"🔙 {mech.mechName} отозван в ангар для техобслуживания.";
        }

        public void OrderMechMove(int mechId, Vector2 radarPos)
        {
            var mech = HangarMechManager.Instance?.GetMechById(mechId);
            if (mech == null || !mech.isDeployed) return;

            mech.targetPos = radarPos;
            HangarAudioLight.Instance?.PlayClick();
            LastBattleLog = $"📍 {mech.mechName}: движение к координатам [{radarPos.x:F0}, {radarPos.y:F0}].";
        }

        public EnemyDot GetClosestEnemy(Vector2 pos, float maxRange)
        {
            EnemyDot best = null;
            float minDst = maxRange;
            foreach (var e in Enemies)
            {
                float d = Vector2.Distance(pos, e.position);
                if (d < minDst) { minDst = d; best = e; }
            }
            return best;
        }

        public MechUnit GetClosestMech(Vector2 pos, float maxRange)
        {
            if (HangarMechManager.Instance == null) return null;
            MechUnit best = null;
            float minDst = maxRange;
            foreach (var m in HangarMechManager.Instance.Mechs)
            {
                if (!m.isDeployed) continue;
                float d = Vector2.Distance(pos, m.battlefieldPos);
                if (d < minDst) { minDst = d; best = m; }
            }
            return best;
        }
    }
}
