using System.Collections.Generic;
using UnityEngine;
using HangarGame.Core;

namespace HangarGame.Data
{
    public static class ModuleCatalog
    {
        private static List<MechPartData> allCatalogModules;

        public static IReadOnlyList<MechPartData> GetAllModules()
        {
            if (allCatalogModules == null || allCatalogModules.Count == 0)
            {
                InitializeCatalog();
            }
            return allCatalogModules;
        }

        public static List<MechPartData> GetModulesForSlot(PartSlot slot)
        {
            var all = GetAllModules();
            List<MechPartData> result = new List<MechPartData>();
            foreach (var m in all)
            {
                if (m.slot == slot)
                {
                    result.Add(m);
                }
            }
            return result;
        }

        private static void InitializeCatalog()
        {
            allCatalogModules = new List<MechPartData>();

            // --- ПРАВАЯ РУКА (ОРУДИЯ) ---
            allCatalogModules.Add(CreatePart(
                "wpn_plasma", "Плазменная пушка 'Сверхновая'", PartSlot.RightArm,
                DamageType.Plasma, DamageType.Thermal, 75f, 20f, 100f, 50, 25,
                new Color(0.1f, 0.8f, 1f), // Cyan
                "Высокотемпературная плазма. Идеально пробивает титановую броню."
            ));

            allCatalogModules.Add(CreatePart(
                "wpn_minigun", "Тяжелый миниган 'Вулкан'", PartSlot.RightArm,
                DamageType.Physical, DamageType.Physical, 65f, 25f, 100f, 40, 20,
                new Color(0.85f, 0.4f, 0.1f), // Bronze
                "Скорострельный шестиствольный пулемет. Режет био-монстров в фарш."
            ));

            allCatalogModules.Add(CreatePart(
                "wpn_flamethrower", "Термо-огнемет 'Инферно'", PartSlot.RightArm,
                DamageType.Thermal, DamageType.Thermal, 85f, 15f, 90f, 45, 30,
                new Color(1f, 0.2f, 0.1f), // Fiery Red
                "Сжигает наступающие рои дронов и расплавляет переохлажденную броню."
            ));

            // --- ЛЕВАЯ РУКА (ЗАЩИТА И РАКЕТЫ) ---
            allCatalogModules.Add(CreatePart(
                "shd_bastion", "Тяжелый щит 'Бастион'", PartSlot.LeftArm,
                DamageType.Physical, DamageType.Physical, 15f, 90f, 120f, 40, 20,
                new Color(0.2f, 0.5f, 0.85f), // Steel Blue
                "Титано-керамическая броня. Поглощает кинетический обстрел и пули."
            ));

            allCatalogModules.Add(CreatePart(
                "shd_aegis", "Энергетический барьер 'Эгида'", PartSlot.LeftArm,
                DamageType.Plasma, DamageType.Plasma, 20f, 95f, 100f, 60, 35,
                new Color(0.7f, 0.2f, 0.9f), // Neon Purple
                "Силовое поле высокой плотности. Рассеивает плазму и лазерные лучи."
            ));

            allCatalogModules.Add(CreatePart(
                "wpn_missiles", "Ракетный блок 'Тайфун'", PartSlot.LeftArm,
                DamageType.Physical, DamageType.Physical, 80f, 30f, 80f, 55, 30,
                new Color(0.3f, 0.7f, 0.3f), // Military Green
                "Штурмовой ракетный блок. Жертвует защитой ради сокрушительного залпа."
            ));

            // --- ТОРС (БРОНЕКОРПУСА) ---
            allCatalogModules.Add(CreatePart(
                "torso_heavy", "Тяжелый бронекорпус МК-IV", PartSlot.Torso,
                DamageType.Physical, DamageType.Physical, 20f, 80f, 140f, 60, 40,
                new Color(0.2f, 0.45f, 0.7f),
                "Усиленная конструкция шасси с максимальным запасом живучести."
            ));

            allCatalogModules.Add(CreatePart(
                "torso_tesla", "Генераторный корпус 'Тесла'", PartSlot.Torso,
                DamageType.Plasma, DamageType.Plasma, 45f, 50f, 100f, 70, 50,
                new Color(0.15f, 0.7f, 0.8f),
                "Встроенный овердрайв-реактор. Увеличивает мощность всего оружия."
            ));
        }

        private static MechPartData CreatePart(string id, string name, PartSlot slot, DamageType res, DamageType atk, float atkPow, float defPow, float maxDur, int crCost, int alCost, Color color, string desc)
        {
            MechPartData p = ScriptableObject.CreateInstance<MechPartData>();
            p.partId = id;
            p.partName = name;
            p.slot = slot;
            p.resistanceType = res;
            p.attackType = atk;
            p.attackPower = atkPow;
            p.defensePower = defPow;
            p.maxDurability = maxDur;
            p.repairCreditCost = crCost;
            p.repairAlloyCost = alCost;
            p.description = desc;
            return p;
        }
    }
}
