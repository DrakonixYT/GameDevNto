#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using HangarGame.Core;
using HangarGame.CameraControl;
using HangarGame.Interaction;
using HangarGame.Data;
using HangarGame.Mechs;
using HangarGame.Repair;
using HangarGame.Dispatch;
using HangarGame.UI;
using HangarGame.Prototype;

namespace HangarGame.EditorTools
{
    [InitializeOnLoad]
    public static class HangarSceneGenerator
    {
        static HangarSceneGenerator()
        {
            // Автоматически запускается при загрузке или компиляции редактора Unity
            EditorApplication.delayCall += AutoCheckAndBuild;
        }

        private static void AutoCheckAndBuild()
        {
            string targetPath = "Assets/Scenes/HangarMainScene.unity";
            if (!File.Exists(targetPath))
            {
                Debug.Log("<color=cyan>[HangarSceneGenerator] Первичная авто-генерация сцены 'HangarMainScene.unity'...</color>");
                GenerateSceneInternal();
            }
        }

        [MenuItem("Pacific Rim / 🚀 1. Сгенерировать сцену прямо сюда (Bake)")]
        public static void BakeIntoCurrentScene()
        {
            // Чистим старые сгенерированные объекты, если они есть
            string[] objectsToClean = new string[] {
                "--- 01_MANAGERS ---",
                "--- 02_LIGHTING_AND_VFX ---",
                "--- 03_HANGAR_ROOM ---",
                "--- 04_MECH_ATLANT ---",
                "--- 05_JAEGER_FRAME ---",
                "--- 06_WORKBENCH ---",
                "--- 07_DISPATCH_CONSOLE ---",
                "--- 08_MINIMAL_HUD ---",
                "Hangar_Room",
                "--- CORE MANAGERS ---",
                "HUD_Canvas",
                "EventSystem",
                "--- AUTO TESTER BOT ---"
            };
            foreach (var name in objectsToClean)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null) Object.DestroyImmediate(obj);
            }

            GameObject builderObj = new GameObject("--- SCENE GENERATOR ---");
            PrototypeSceneBuilder builder = builderObj.AddComponent<PrototypeSceneBuilder>();
            builder.BuildCompleteHangarPrototype();
            Object.DestroyImmediate(builderObj);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("<color=green><b>[Pacific Rim]</b> Все объекты (ангар, робот, верстак, пульт, UI) успешно добавлены в текущую сцену!</color>");
        }

        [MenuItem("Pacific Rim / 💾 2. Пересоздать файл сцены 'HangarMainScene'")]
        public static void GenerateSceneMenu()
        {
            GenerateSceneInternal();
        }

        public static void GenerateSceneInternal()
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject builderObj = new GameObject("--- SCENE GENERATOR ---");
            PrototypeSceneBuilder builder = builderObj.AddComponent<PrototypeSceneBuilder>();
            builder.BuildCompleteHangarPrototype();
            Object.DestroyImmediate(builderObj);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            string scenePath = "Assets/Scenes/HangarMainScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[HangarSceneGenerator] УСПЕХ!</b> Сцена сохранена в {scenePath}.</color>");
        }

        [MenuItem("Pacific Rim / 📦 3. Создать тестовые ScriptableObjects")]
        public static void CreateDefaultDataAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DataAssets"))
            {
                AssetDatabase.CreateFolder("Assets", "DataAssets");
            }

            CreatePartAsset("Assets/DataAssets/Part_Shield_Bastion.asset", "Тяжелый щит 'Бастион'", PartSlot.LeftArm, DamageType.Physical, DamageType.Physical, 10f, 90f);
            CreatePartAsset("Assets/DataAssets/Part_Plasma_Cannon.asset", "Плазменная пушка 'Сверхновая'", PartSlot.RightArm, DamageType.Plasma, DamageType.Thermal, 70f, 15f);
            CreatePartAsset("Assets/DataAssets/Part_Heavy_Torso.asset", "Бронекорпус МК-IV", PartSlot.Torso, DamageType.Physical, DamageType.Physical, 20f, 100f);

            CreateThreatAsset("Assets/DataAssets/Threat_AssaultDrones.asset", "Штурмовые дроны", DamageType.Physical, DamageType.Plasma, 45f, 200, 80);
            CreateThreatAsset("Assets/DataAssets/Threat_FireBeast.asset", "Огненный монстр 'Инферно'", DamageType.Thermal, DamageType.Plasma, 60f, 300, 120);
            CreateThreatAsset("Assets/DataAssets/Threat_GoliathMech.asset", "Вражеский Титан 'Голиаф'", DamageType.Physical, DamageType.Thermal, 80f, 450, 200);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=cyan>[HangarSceneGenerator] Тестовые ассеты данных созданы в папке Assets/DataAssets/!</color>");
        }

        private static void CreatePartAsset(string path, string name, PartSlot slot, DamageType def, DamageType atk, float atkPow, float defPow)
        {
            MechPartData asset = ScriptableObject.CreateInstance<MechPartData>();
            asset.partName = name;
            asset.slot = slot;
            asset.resistanceType = def;
            asset.attackType = atk;
            asset.attackPower = atkPow;
            asset.defensePower = defPow;
            asset.maxDurability = 100f;
            asset.repairCreditCost = 40;
            asset.repairAlloyCost = 20;

            AssetDatabase.CreateAsset(asset, path);
        }

        private static void CreateThreatAsset(string path, string name, DamageType atk, DamageType weak, float power, int credits, int alloy)
        {
            ThreatData asset = ScriptableObject.CreateInstance<ThreatData>();
            asset.threatName = name;
            asset.incomingAttackType = atk;
            asset.weakAgainst = weak;
            asset.basePower = power;
            asset.missionDurationSeconds = 5f;
            asset.rewardCredits = credits;
            asset.rewardAlloy = alloy;

            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
#endif
