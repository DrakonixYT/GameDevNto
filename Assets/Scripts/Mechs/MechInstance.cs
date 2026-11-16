using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;
using HangarGame.Data;

namespace HangarGame.Mechs
{
    public class MechInstance : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private string mechName = "Егерь 'Атлант-1'";
        [SerializeField] private bool isDeployed = false;

        [Header("Parts")]
        [SerializeField] private List<MechPartInstance> parts = new List<MechPartInstance>();

        public string MechName => mechName;
        public bool IsDeployed => isDeployed;
        public IReadOnlyList<MechPartInstance> Parts => parts;

        private Vector3 hangarPosition;

        private void Awake()
        {
            hangarPosition = transform.position;

            if (parts.Count == 0)
            {
                parts.AddRange(GetComponentsInChildren<MechPartInstance>());
            }
        }

        public void RegisterPart(MechPartInstance part)
        {
            if (!parts.Contains(part))
            {
                parts.Add(part);
            }
        }

        public float GetTotalAttackPower()
        {
            float total = 0f;
            foreach (var part in parts)
            {
                if (part != null && part.Data != null)
                {
                    total += part.Data.attackPower * part.DurabilityPercent;
                }
            }
            return Mathf.Max(5f, total);
        }

        public bool HasDefenseAgainst(DamageType attackType)
        {
            foreach (var part in parts)
            {
                if (part != null && part.Data != null && part.Data.resistanceType == attackType && part.DurabilityPercent > 0.2f)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasAttackAgainst(DamageType weakness)
        {
            foreach (var part in parts)
            {
                if (part != null && part.Data != null && part.Data.attackType == weakness && part.DurabilityPercent > 0.2f)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasMissingOrDetachedParts()
        {
            if (parts.Count < 5) return true;
            foreach (var part in parts)
            {
                if (part == null || part.IsOnWorkbench || !part.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanDeploy => !HasMissingOrDetachedParts() && !isDeployed;

        public void SetDeployed(bool deployed)
        {
            isDeployed = deployed;
            // Визуально скрываем меха или опускаем платформу, если он ушел в бой
            gameObject.SetActive(!deployed);
        }

        public void ApplyCombatDamage(float totalDamage)
        {
            if (parts.Count == 0) return;

            float damagePerPart = totalDamage / parts.Count;
            foreach (var part in parts)
            {
                if (part != null)
                {
                    // Небольшой разброс повреждений по деталям
                    float randomMod = Random.Range(0.7f, 1.3f);
                    part.ApplyDamage(damagePerPart * randomMod);
                }
            }
        }

        public float GetAverageHealthPercent()
        {
            if (parts.Count == 0) return 1f;
            float sum = 0f;
            int validCount = 0;
            foreach (var part in parts)
            {
                if (part != null)
                {
                    sum += part.DurabilityPercent;
                    validCount++;
                }
            }
            return validCount > 0 ? (sum / validCount) : 1f;
        }
    }
}
