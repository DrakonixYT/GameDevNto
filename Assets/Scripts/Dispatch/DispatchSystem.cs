using System;
using System.Collections;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Mechs;
using HangarGame.Repair;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HangarGame.Dispatch
{
    [Serializable]
    public class ThreatData
    {
        public string name; public DamageType attackType, weakness; public float power;
        public int rewardCredits = 250, rewardAlloy = 100;
    }

    public class DispatchSystem : MonoBehaviour
    {
        public static DispatchSystem Instance { get; private set; }
        public ThreatData CurrentThreat { get; private set; }
        public bool HasActiveThreat => CurrentThreat != null && !IsMissionInProgress;
        public bool IsMissionInProgress { get; private set; } = false;
        public bool HasQtePrompt { get; private set; } = false;
        public float QteTimer { get; private set; } = 0f;
        public string LastMissionLog { get; private set; } = "База на боевом дежурстве. Держите робота в боеготовности.";
        public string AdaptationAlert { get; private set; } = "";

        private float waveTimer = 3f, alarmTimer = 0f;
        private int adaptationStreak = 0;
        private DamageType counterWeakness = DamageType.Thermal;

        private void Awake() => Instance = this;

        private void Update()
        {
            if (!HasActiveThreat && !IsMissionInProgress)
            {
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0f) { SpawnThreat(); waveTimer = 22f; }
            }
            else if (HasActiveThreat && !IsMissionInProgress)
            {
                alarmTimer -= Time.deltaTime;
                if (alarmTimer <= 0f) { alarmTimer = 4.5f; HangarAudioLight.Instance?.SetAlarm(true); }
            }
            if (HasQtePrompt)
            {
                QteTimer -= Time.deltaTime;
                bool space = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null) space = Keyboard.current.spaceKey.wasPressedThisFrame;
#else
                space = Input.GetKeyDown(KeyCode.Space);
#endif
                if (space || QteTimer <= 0f) ExecuteQte();
            }
        }

        public void SpawnThreat()
        {
            if (IsMissionInProgress) return;
            string[] names = { "Кайдзю 'Бритва-5'", "Био-Титан 'Левиафан'", "Осадный Мех 'Цербер'" };
            DamageType[] types = { DamageType.Physical, DamageType.Thermal, DamageType.Plasma };
            CurrentThreat = new ThreatData {
                name = names[UnityEngine.Random.Range(0, names.Length)],
                attackType = types[UnityEngine.Random.Range(0, types.Length)],
                weakness = counterWeakness, power = UnityEngine.Random.Range(35f, 55f)
            };
            HangarAudioLight.Instance?.SetAlarm(true);
        }

        public bool CanDeploy()
        {
            if (!HasActiveThreat || IsMissionInProgress) return false;
            if (RepairWorkbench.Instance != null && RepairWorkbench.Instance.HasItem) return false;
            if (CombatMech.Instance == null || CombatMech.Instance.HasMissingOrDetachedParts()) return false;
            return true;
        }

        public bool TryDeploy()
        {
            if (!CanDeploy()) return false;
            StartCoroutine(MissionRoutine());
            return true;
        }

        private IEnumerator MissionRoutine()
        {
            IsMissionInProgress = true;
            HangarAudioLight.Instance?.SetAlarm(false);
            CombatMech.Instance?.SetDeployed(true);

            yield return new WaitForSeconds(1.5f);
            HasQtePrompt = true; QteTimer = 3.5f;
            while (HasQtePrompt) yield return null;
            yield return new WaitForSeconds(1.5f);

            float def = CombatMech.Instance.GetTotalDefense(CurrentThreat.attackType);
            float dmg = Mathf.Max(10f, CurrentThreat.power * 1.5f - def * 0.3f);
            CombatMech.Instance.ApplyMissionDamage(dmg);
            CombatMech.Instance.SetDeployed(false);

            GameManager.Instance?.AddResources(CurrentThreat.rewardCredits, CurrentThreat.rewardAlloy);
            LastMissionLog = $"Враг [{CurrentThreat.name}] уничтожен! +{CurrentThreat.rewardCredits}¢, +{CurrentThreat.rewardAlloy}т. Урон: {dmg:F0}";

            if (++adaptationStreak >= 2)
            {
                adaptationStreak = 0; counterWeakness = (DamageType)(((int)counterWeakness + 1) % 4);
                AdaptationAlert = $"Кайдзю адаптировались! Новая слабость: {counterWeakness}";
            }
            CurrentThreat = null; IsMissionInProgress = false; HangarAudioLight.Instance?.PlaySuccess();
        }

        public void ExecuteQte() { if (HasQtePrompt) { HasQtePrompt = false; HangarAudioLight.Instance?.PlayBolt(); } }
    }
}
