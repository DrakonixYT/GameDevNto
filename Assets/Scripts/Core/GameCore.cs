using System;
using UnityEngine;

namespace HangarGame.Core
{
    public enum DamageType { Physical, Thermal, Plasma, Corrosive }
    public enum PartSlot { Torso, Head, LeftArm, RightArm, Legs }
    public enum RoomView { Hangar, DispatchConsole, WorkbenchFocus }

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

    public static class ModuleCatalog
    {
        public static MechPartData GetNextModule(MechPartData cur)
        {
            if (cur.slot == PartSlot.RightArm)
                return cur.attackType == DamageType.Plasma
                    ? new MechPartData { partName = "Рельсотрон 'Копьё'", slot = PartSlot.RightArm, attackType = DamageType.Physical, attackPower = 65f, defenseType = DamageType.Physical, defensePower = 20f }
                    : new MechPartData { partName = "Плазменная Пушка 'Сверхновая'", slot = PartSlot.RightArm, attackType = DamageType.Plasma, attackPower = 60f, defenseType = DamageType.Thermal, defensePower = 20f };
            if (cur.slot == PartSlot.LeftArm)
                return cur.defenseType == DamageType.Physical
                    ? new MechPartData { partName = "Энергетический Щит 'Эгида'", slot = PartSlot.LeftArm, attackType = DamageType.Physical, attackPower = 10f, defenseType = DamageType.Plasma, defensePower = 85f }
                    : new MechPartData { partName = "Тяжелый Щит 'Бастион'", slot = PartSlot.LeftArm, attackType = DamageType.Physical, attackPower = 10f, defenseType = DamageType.Physical, defensePower = 80f };
            return cur;
        }
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public int Credits { get; private set; } = 500;
        public int Alloy { get; private set; } = 200;
        public float JaegerProgress { get; private set; } = 0f;
        public RoomView CurrentView { get; private set; } = RoomView.Hangar;
        public bool IsGameWon { get; private set; } = false;

        public event Action<int, int> OnResourcesChanged;
        public event Action<RoomView> OnViewChanged;
        public event Action<float> OnJaegerProgressChanged;
        public event Action OnGameWon;

        private void Awake() => Instance = this;

        public bool TrySpend(int credits, int alloy)
        {
            if (Credits < credits || Alloy < alloy) return false;
            Credits -= credits; Alloy -= alloy;
            OnResourcesChanged?.Invoke(Credits, Alloy);
            return true;
        }

        public void AddResources(int credits, int alloy)
        {
            Credits += credits; Alloy += alloy;
            OnResourcesChanged?.Invoke(Credits, Alloy);
        }

        public void SwitchView(RoomView v) { CurrentView = v; OnViewChanged?.Invoke(v); }
        public void SetJaegerProgress(float p) { JaegerProgress = Mathf.Clamp01(p); OnJaegerProgressChanged?.Invoke(JaegerProgress); }
        public void TriggerVictory() { IsGameWon = true; OnGameWon?.Invoke(); }
    }
}
