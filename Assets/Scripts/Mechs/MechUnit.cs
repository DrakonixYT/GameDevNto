using System.Collections.Generic;
using HangarGame.Armory;
using HangarGame.Core;
using UnityEngine;

namespace HangarGame.Mechs
{
    public class MechUnit : MonoBehaviour
    {
        public int id;
        public string mechName;
        public Color themeColor = Color.cyan;
        public List<MechPart> parts = new List<MechPart>();

        public bool isDeployed;
        public bool isReturning;
        public bool isShieldActive;
        public float currentEnergy = 100f;
        public float maxEnergy = 100f;
        public float currentHp = 100f;
        public float maxHp = 100f;
        public Vector2 battlefieldPos;
        public Vector2 targetPos;
        public bool hasTarget;
        public float attackPower = 25f;
        public float moveSpeed = 65f;

        public string equippedLeftWeaponId = ArmoryCatalog.DefaultShieldId;
        public string equippedRightWeaponId = ArmoryCatalog.DefaultWeaponId;

        [SerializeField] private Transform bayVisualRoot;
        [SerializeField, HideInInspector] private float baseMaxHp;
        [SerializeField, HideInInspector] private float baseAttackPower;

        public Transform BayVisualRoot => bayVisualRoot;
        public void SetBayVisualRoot(Transform root) => bayVisualRoot = root;

        public float HpPercent => maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) * 100f : 100f;
        public Vector3 RestLocalPos { get; private set; }
        public Quaternion RestLocalRot { get; private set; }
        public Vector3 RestLocalScale { get; private set; }
        public bool HasRestPose { get; private set; }

        public float TotalDefense
        {
            get
            {
                float def = 0f;
                EnsurePartsLoaded();
                foreach (var p in parts)
                {
                    if (p != null && p.Data != null)
                        def += p.Data.defensePower;
                }
                return def;
            }
        }

        private void Awake()
        {
            CaptureRestPose();
            EnsureBaseStats();
            EnsurePartsLoaded();
            ApplyThemeColor();
            SyncHpFromParts();
        }

        public void CaptureRestPose()
        {
            if (HasRestPose) return;
            RestLocalPos = transform.localPosition;
            RestLocalRot = transform.localRotation;
            RestLocalScale = transform.localScale;
            HasRestPose = true;
        }

        public void RestoreRestPose()
        {
            if (!HasRestPose) return;
            transform.localPosition = RestLocalPos;
            transform.localRotation = RestLocalRot;
            transform.localScale = RestLocalScale;
        }

        private void EnsureBaseStats()
        {
            if (baseMaxHp <= 0f) baseMaxHp = maxHp;
            if (baseAttackPower <= 0f) baseAttackPower = attackPower;
        }

        public void EnsurePartsLoaded()
        {
            if (parts == null) parts = new List<MechPart>();
            if (parts.Count == 0)
                parts.AddRange(GetComponentsInChildren<MechPart>(true));
        }

        public void ApplyThemeColor()
        {
            EnsurePartsLoaded();
            foreach (var p in parts)
            {
                if (p == null) continue;
                var r = p.GetComponent<Renderer>();
                if (r == null) r = p.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.name.IndexOf("Holo", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    p.UpdateVisuals();
                    continue;
                }
                p.BaseColor = themeColor;
                p.UpdateVisuals();
            }
        }

        public MechPart GetPartBySlot(PartSlot slot)
        {
            EnsurePartsLoaded();
            foreach (var p in parts)
            {
                if (p != null && p.Data != null && p.Data.slot == slot)
                    return p;
            }
            return null;
        }

        public bool HasMissingOrDetachedParts()
        {
            EnsurePartsLoaded();
            foreach (var p in parts)
            {
                if (p != null && p.IsOnWorkbench) return true;
            }
            return false;
        }

        public bool CanDeploy()
        {
            return !isDeployed
                && !isReturning
                && !HasMissingOrDetachedParts()
                && (maxHp <= 0f || (currentHp / maxHp) > 0.10f)
                && currentEnergy > 10f;
        }

        public void RechargeBattery() => currentEnergy = maxEnergy;

        public void ToggleShield()
        {
            isShieldActive = currentEnergy > 5f && !isShieldActive;
        }

        public void SyncHpFromParts()
        {
            EnsurePartsLoaded();
            float total = 0f;
            int repairableCount = 0;

            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p == null || !p.IsRepairableSlot) continue;
                total += p.DurabilityPercent;
                repairableCount++;
            }

            if (repairableCount == 0)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i] != null)
                    {
                        total += parts[i].DurabilityPercent;
                        repairableCount++;
                    }
                }
            }

            float ratio = repairableCount > 0 ? (total / repairableCount) : 1f;
            currentHp = maxHp * ratio;
        }

        public void SyncPartsFromHp(float ratio)
        {
            EnsurePartsLoaded();
            ratio = Mathf.Clamp01(ratio);

            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p == null) continue;

                if (!p.IsRepairableSlot)
                {
                    p.SetDurabilityPercent(1f);
                    continue;
                }

                float partRatio = ratio < 0.98f
                    ? Mathf.Clamp01(ratio + Random.Range(-0.14f, 0.08f))
                    : 1f;

                p.SetDurabilityPercent(partRatio);
            }

            SyncHpFromParts();
        }

        public void ApplyLoadout(WeaponDefinition leftWeapon, WeaponDefinition rightWeapon, GameObject leftPrefab, GameObject rightPrefab)
        {
            EnsureBaseStats();
            float prevRatio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 1f;

            float hpBonus = leftWeapon != null ? leftWeapon.bonusMaxHp : 0f;
            float atkBonus = (rightWeapon != null ? rightWeapon.bonusAttack : 0f)
                           + (leftWeapon != null ? leftWeapon.bonusAttack : 0f);

            maxHp = baseMaxHp + hpBonus;
            attackPower = baseAttackPower + atkBonus;
            currentHp = maxHp * prevRatio;

            if (leftWeapon != null)
            {
                equippedLeftWeaponId = leftWeapon.id;
                var leftPart = GetPartBySlot(PartSlot.LeftArm);
                if (leftPart != null)
                {
                    leftPart.Data.defenseType = leftWeapon.damageType;
                    leftPart.Data.defensePower = 20f + leftWeapon.bonusDefense;
                    leftPart.MountWeaponVisual(leftWeapon, leftPrefab);
                }
                if (bayVisualRoot != null)
                    MechPart.MountWeaponVisualOnArm(bayVisualRoot.Find("LeftArmLow02"), leftWeapon, leftPrefab);
            }

            if (rightWeapon != null)
            {
                equippedRightWeaponId = rightWeapon.id;
                var rightPart = GetPartBySlot(PartSlot.RightArm);
                if (rightPart != null)
                {
                    rightPart.Data.attackType = rightWeapon.damageType;
                    rightPart.Data.attackPower = 30f + rightWeapon.bonusAttack;
                    rightPart.MountWeaponVisual(rightWeapon, rightPrefab);
                }
                if (bayVisualRoot != null)
                    MechPart.MountWeaponVisualOnArm(bayVisualRoot.Find("RightArmLow02"), rightWeapon, rightPrefab);
            }
        }

        public void SetDeployed(bool deployed, HangarMechManager hangar = null, Vector2 startPos = default)
        {
            isDeployed = deployed;
            battlefieldPos = startPos;
            targetPos = startPos;
            hasTarget = false;

            if (!deployed)
            {
                isReturning = false;
                isShieldActive = false;
            }

            hangar ??= GameManager.Instance?.hangar;
            bool showOnPodium = !deployed && hangar != null && hangar.CurrentMech == this;
            gameObject.SetActive(showOnPodium);
            if (showOnPodium) RestoreRestPose();
            if (bayVisualRoot != null) bayVisualRoot.gameObject.SetActive(showOnPodium);
        }

        public void ApplyDamage(float dmg)
        {
            float armorMitigation = Mathf.Clamp01(TotalDefense / 260f) * 0.35f;
            dmg *= (1f - armorMitigation);

            if (isShieldActive)
            {
                dmg *= 0.35f;
                currentEnergy = Mathf.Max(0f, currentEnergy - dmg * 0.25f);
                if (currentEnergy <= 0f) isShieldActive = false;
            }

            currentHp = Mathf.Max(0f, currentHp - dmg);

            int repairableCount = 0;
            foreach (var p in parts)
                if (p != null && p.IsRepairableSlot) repairableCount++;

            float perPartDmg = (dmg * 0.65f) / Mathf.Max(1, repairableCount);
            foreach (var p in parts)
            {
                if (p != null && p.IsRepairableSlot)
                    p.Damage(perPartDmg);
            }
        }

        public void RepairAll()
        {
            currentHp = maxHp;
            foreach (var p in parts)
            {
                if (p != null) p.RepairFull();
            }
        }

        public float GetTotalHpPercent() => maxHp > 0f ? (currentHp / maxHp) : 1f;
    }
}
