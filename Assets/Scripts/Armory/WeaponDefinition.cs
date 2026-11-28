using System;
using System.Collections.Generic;
using HangarGame.Core;
using UnityEngine;

namespace HangarGame.Armory
{
    [Serializable]
    public class WeaponDefinition
    {
        public string id;
        public string displayName;
        public ArmorySlot slot;
        public DamageType damageType;
        public int costCredits;
        public int costAlloy;
        public float bonusAttack;
        public float bonusDefense;
        public float bonusMaxHp;
        public bool hasCustomVisual;
        public string modelAssetPath;
        public float visualScaleFactor = 0.85f;
        public Color accentColor = Color.cyan;
        public GameObject visualPrefab;
    }

    public static class ArmoryCatalog
    {
        public const string DefaultWeaponId = "weapon_autocannon";
        public const string DefaultShieldId = "shield_standard";

        public static List<WeaponDefinition> CreateDefaultCatalog()
        {
            return new List<WeaponDefinition>
            {
                new WeaponDefinition
                {
                    id = DefaultWeaponId,
                    displayName = "Автопушка Мк-1 (Базовая)",
                    slot = ArmorySlot.RightArm,
                    damageType = DamageType.Physical,
                    costCredits = 0,
                    costAlloy = 0,
                    bonusAttack = 0f,
                    bonusDefense = 0f,
                    hasCustomVisual = false,
                    accentColor = new Color(0.6f, 0.65f, 0.7f)
                },
                new WeaponDefinition
                {
                    id = "weapon_plasma",
                    displayName = "Плазменная пушка 'Сверхновая'",
                    slot = ArmorySlot.RightArm,
                    damageType = DamageType.Plasma,
                    costCredits = 170,
                    costAlloy = 65,
                    bonusAttack = 16f,
                    bonusDefense = 0f,
                    hasCustomVisual = true,
                    visualScaleFactor = 0.85f,
                    accentColor = new Color(0.15f, 0.88f, 1f)
                },
                new WeaponDefinition
                {
                    id = "weapon_spear",
                    displayName = "Энергокопьё 'Лонгин'",
                    slot = ArmorySlot.RightArm,
                    damageType = DamageType.Thermal,
                    costCredits = 260,
                    costAlloy = 100,
                    bonusAttack = 26f,
                    bonusDefense = 5f,
                    hasCustomVisual = true,
                    modelAssetPath = "Assets/Prefabs/Eger/EgerSpear.fbx",
                    visualScaleFactor = 1.25f,
                    accentColor = new Color(1f, 0.55f, 0.15f)
                },
                new WeaponDefinition
                {
                    id = DefaultShieldId,
                    displayName = "Стандартный щит (Базовый)",
                    slot = ArmorySlot.LeftArm,
                    damageType = DamageType.Physical,
                    costCredits = 0,
                    costAlloy = 0,
                    bonusAttack = 0f,
                    bonusDefense = 0f,
                    bonusMaxHp = 0f,
                    hasCustomVisual = false,
                    accentColor = new Color(0.55f, 0.6f, 0.65f)
                },
                new WeaponDefinition
                {
                    id = "shield_aegis",
                    displayName = "Энергощит 'Эгида'",
                    slot = ArmorySlot.LeftArm,
                    damageType = DamageType.Plasma,
                    costCredits = 150,
                    costAlloy = 55,
                    bonusAttack = 0f,
                    bonusDefense = 30f,
                    bonusMaxHp = 20f,
                    hasCustomVisual = true,
                    visualScaleFactor = 0.75f,
                    accentColor = new Color(0.2f, 0.85f, 1f)
                },
                new WeaponDefinition
                {
                    id = "shield_bastion",
                    displayName = "Тяжёлый щит 'Бастион'",
                    slot = ArmorySlot.LeftArm,
                    damageType = DamageType.Physical,
                    costCredits = 240,
                    costAlloy = 95,
                    bonusAttack = 6f,
                    bonusDefense = 55f,
                    bonusMaxHp = 40f,
                    hasCustomVisual = true,
                    modelAssetPath = "Assets/Prefabs/Eger/EgerShield.fbx",
                    visualScaleFactor = 0.88f,
                    accentColor = new Color(0.85f, 0.7f, 0.25f)
                }
            };
        }
    }
}
