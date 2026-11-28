#if UNITY_EDITOR
using System.Collections.Generic;
using HangarGame.Armory;
using HangarGame.CameraControl;
using HangarGame.Core;
using HangarGame.Dispatch;
using HangarGame.Mechs;
using HangarGame.Prototype;
using HangarGame.Repair;
using HangarGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HangarGame.EditorTools
{
    public static class HangarSceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/HangarMainScene.unity";

        [MenuItem("Tools/Bake Hangar Scene (Full Reset)", false, 1)]
        [MenuItem("Pacific Rim/Bake Hangar Scene", false, 1)]
        public static void BakeIntoCurrentScene()
        {
            var activeScene = OpenMainScene();

            string[] objectsToClean =
            {
                "--- 01_MANAGERS ---", "--- 02_LIGHTING_AND_VFX ---", "--- 03_HANGAR_ROOM ---",
                "--- 04_MECH_ATLANT ---", "--- 05_JAEGER_FRAME ---", "--- 06_WORKBENCH ---",
                "--- 07_DISPATCH_CONSOLE ---", "--- 08_MINIMAL_HUD ---",
                "Hangar_Room", "--- CORE MANAGERS ---", "HUD_Canvas", "EventSystem", "Main Camera"
            };
            foreach (var objName in objectsToClean)
            {
                GameObject obj = GameObject.Find(objName);
                if (obj != null) Object.DestroyImmediate(obj);
            }

            var builderObj = new GameObject("--- SCENE GENERATOR ---");
            builderObj.AddComponent<PrototypeSceneBuilder>().BuildCompleteHangarPrototype();
            Object.DestroyImmediate(builderObj);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[Hangar] Сцена пересобрана и сохранена.");
        }

        [MenuItem("Tools/Upgrade Hangar Scene (Preserve Art)", false, 2)]
        public static void UpgradeCurrentScenePreserveArt()
        {
            var activeScene = OpenMainScene();

            WireManagersAndAlarmLight();
            WireMechsAndBrokenParts();
            WireWorkbenchAnd3DButtons();
            WireJaegerProjectFromScene();
            RebuildAllSceneUIs();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[Hangar] Сцена обновлена с сохранением моделей и материалов.");
        }

        private static Scene OpenMainScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            return activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void WireManagersAndAlarmLight()
        {
            GameObject mgrs = GameObject.Find("--- 01_MANAGERS ---");
            if (mgrs == null) return;

            ModelUtils.GetOrAdd<HangarAudio>(mgrs);
            var alarm = ModelUtils.GetOrAdd<HangarAlarmLight>(mgrs);

            var alarmLightObj = GameObject.Find("Spotlight_Alarm_Red");
            if (alarmLightObj != null) alarm.ConfigureLight(alarmLightObj.GetComponent<Light>());
            EditorUtility.SetDirty(alarm);

            var gm = ModelUtils.GetOrAdd<GameManager>(mgrs);
            var soGm = new SerializedObject(gm);
            soGm.FindProperty("startingCredits").intValue = 220;
            soGm.FindProperty("startingAlloy").intValue = 95;
            soGm.FindProperty("maxBaseHp").floatValue = 100f;
            soGm.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gm);

            var disp = mgrs.GetComponent<DispatchSystem>();
            if (disp != null)
            {
                var soDisp = new SerializedObject(disp);
                soDisp.FindProperty("firstEnemyDelay").floatValue = 9f;
                soDisp.FindProperty("minSpawnInterval").floatValue = 18f;
                soDisp.FindProperty("maxSpawnInterval").floatValue = 27f;
                soDisp.FindProperty("workbenchTimerMultiplier").floatValue = 0.55f;
                soDisp.FindProperty("maxActiveEnemies").intValue = 4;
                soDisp.FindProperty("breachCreditsPenalty").intValue = 50;
                soDisp.FindProperty("breachAlloyPenalty").intValue = 25;
                soDisp.FindProperty("breachBaseDamage").floatValue = 25f;
                soDisp.FindProperty("combatEngageRange").floatValue = 185f;
                soDisp.FindProperty("holdToFightRange").floatValue = 145f;
                soDisp.FindProperty("enemyAggroRange").floatValue = 210f;
                soDisp.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(disp);
            }

            var armory = ModelUtils.GetOrAdd<ArmoryManager>(mgrs);
            armory.ResetCatalogFromDefaults();
            armory.SetWeaponPrefab("weapon_spear", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerSpear.fbx"));
            armory.SetWeaponPrefab("shield_bastion", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerShield.fbx"));
            EditorUtility.SetDirty(armory);
        }

        private static void WireMechsAndBrokenParts()
        {
            var mechMgr = Object.FindAnyObjectByType<HangarMechManager>();
            if (mechMgr == null) return;

            Transform bay0 = mechMgr.transform.Find("Bay_Mech_0_Страж-1")
                ?? mechMgr.transform.Find("Mech_0_Страж-1 (Лёгкий) (1)");

            var allUnits = Object.FindObjectsByType<MechUnit>(FindObjectsInactive.Include);
            foreach (var u in allUnits)
            {
                if (!mechMgr.Mechs.Contains(u))
                {
                    if (bay0 == null) bay0 = u.transform;
                    foreach (var p in u.GetComponentsInChildren<MechPart>(true)) Object.DestroyImmediate(p);
                    foreach (var col in u.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                    Object.DestroyImmediate(u);
                }
            }

            Material holoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Holo.mat");
            GameObject brokenHead = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/HeadBroken.fbx");
            GameObject brokenLeftArm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/LeftArmBroken.fbx");
            GameObject brokenRightArm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mech01/BrokenParts/RightArmBroken.fbx");

            float[] mechHp = { 85f, 120f, 175f };
            float[] mechAtk = { 18f, 26f, 34f };
            float[] mechSpd = { 95f, 62f, 40f };

            for (int i = 0; i < mechMgr.Mechs.Count; i++)
            {
                var mech = mechMgr.Mechs[i];
                if (mech == null) continue;
                if (i < mechHp.Length)
                {
                    mech.maxHp = mechHp[i];
                    mech.currentHp = mechHp[i];
                    mech.attackPower = mechAtk[i];
                    mech.moveSpeed = mechSpd[i];
                }
                if (holoMat != null) ModelUtils.ApplyPrimaryMaterial(mech.gameObject, holoMat);
                mech.EnsurePartsLoaded();
                mech.GetPartBySlot(PartSlot.Head)?.AttachBrokenVisual(brokenHead);
                mech.GetPartBySlot(PartSlot.LeftArm)?.AttachBrokenVisual(brokenLeftArm);
                mech.GetPartBySlot(PartSlot.RightArm)?.AttachBrokenVisual(brokenRightArm);

                var battery = mech.transform.parent?.Find("Battery_Charging_Station/Battery_Energy_Core");
                if (battery != null) ModelUtils.GetOrAdd<BatteryStation>(battery.gameObject);
                EditorUtility.SetDirty(mech);
            }

            if (bay0 == null && mechMgr.Mechs.Count > 0 && mechMgr.Mechs[0] != null)
            {
                GameObject created = Object.Instantiate(mechMgr.Mechs[0].gameObject, mechMgr.transform, false);
                created.name = "Bay_Mech_0_Страж-1";
                created.transform.localPosition = new Vector3(-15.07f, 1.7343613f, -5.07f);
                created.transform.localRotation = new Quaternion(0.7071068f, 0f, 0f, -0.7071068f);
                created.transform.localScale = Vector3.one * 20f;
                StripBayDecor(created);
                bay0 = created.transform;
            }

            if (bay0 != null)
            {
                bay0.name = "Bay_Mech_0_Страж-1";
                StripBayDecor(bay0.gameObject);
                Bounds bay0Bounds = ModelUtils.CalcBounds(bay0.gameObject, true);

                ModelUtils.ApplyPrimaryMaterial(bay0.gameObject, AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Guard_Cyan.mat"));
                Transform bay1 = RebuildBayClone(mechMgr.transform, bay0, "Bay_Mech_1_Атлант-2", 1.06f, AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Atlant_Blue.mat"), bay0Bounds.min.y);
                Transform bay2 = RebuildBayClone(mechMgr.transform, bay0, "Bay_Mech_2_Голиаф-3", 1.14f, AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Mech01/Mech01_Goliath_Orange.mat"), bay0Bounds.min.y);

                var bayList = new List<Transform> { bay0, bay1, bay2 };
                for (int i = 0; i < bayList.Count; i++)
                {
                    if (bayList[i] != null) bayList[i].gameObject.SetActive(i == 0);
                    if (i < mechMgr.Mechs.Count && mechMgr.Mechs[i] != null)
                    {
                        mechMgr.Mechs[i].SetBayVisualRoot(bayList[i]);
                        EditorUtility.SetDirty(mechMgr.Mechs[i]);
                    }
                }
                mechMgr.ConfigureBayMechs(bayList);
            }

            var soMgr = new SerializedObject(mechMgr);
            soMgr.FindProperty("startFirstMechDamaged").boolValue = true;
            soMgr.FindProperty("firstMechStartHpRatio").floatValue = 0.58f;
            soMgr.FindProperty("exitDuration").floatValue = 1.0f;
            soMgr.FindProperty("enterDuration").floatValue = 1.0f;
            soMgr.FindProperty("verticalLiftOffset").floatValue = 0.9f;
            soMgr.FindProperty("bayVerticalLiftOffset").floatValue = 9.5f;
            soMgr.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mechMgr);
        }

        private static Transform RebuildBayClone(Transform parent, Transform source, string cloneName, float scaleMult, Material mat, float floorMinY)
        {
            string prefix = cloneName.Length >= 10 ? cloneName.Substring(0, 10) : cloneName;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch != source && (ch.name == cloneName || ch.name.StartsWith(prefix)))
                    Object.DestroyImmediate(ch.gameObject);
            }

            GameObject clone = Object.Instantiate(source.gameObject, parent, false);
            clone.name = cloneName;
            clone.transform.localPosition = source.localPosition;
            clone.transform.localRotation = source.localRotation;
            clone.transform.localScale = source.localScale * scaleMult;
            StripBayDecor(clone);
            ModelUtils.ApplyPrimaryMaterial(clone, mat);

            Bounds b = ModelUtils.CalcBounds(clone, true);
            if (b.size.sqrMagnitude > 0.0001f)
                clone.transform.position += new Vector3(0f, floorMinY - b.min.y, 0f);

            clone.SetActive(false);
            return clone.transform;
        }

        private static void StripBayDecor(GameObject go)
        {
            foreach (var u in go.GetComponentsInChildren<MechUnit>(true)) Object.DestroyImmediate(u);
            foreach (var p in go.GetComponentsInChildren<MechPart>(true)) Object.DestroyImmediate(p);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var tr in go.GetComponentsInChildren<Transform>(true))
            {
                if (tr != null && (tr.name == "Broken_Visual" || tr.name.StartsWith("Equipped_")))
                    Object.DestroyImmediate(tr.gameObject);
            }
        }

        private static void WireWorkbenchAnd3DButtons()
        {
            GameObject wbObj = GameObject.Find("--- 06_WORKBENCH ---");
            if (wbObj == null) return;

            var wb = ModelUtils.GetOrAdd<RepairWorkbench>(wbObj);
            wb.SetPlatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/Plate.fbx"));
            wb.SetRestOffset(new Vector3(0f, 1.00f, -0.04f));

            var soWb = new SerializedObject(wb);
            var extProp = soWb.FindProperty("normalizedPartExtent");
            if (extProp != null) extProp.floatValue = 0.64f;
            soWb.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wb);

            var rCam = Object.FindAnyObjectByType<RoomCamera>();
            if (rCam != null)
            {
                rCam.ConfigureWorkbenchCamera(new Vector3(0f, 1.54f, -1.24f), 22f);
                EditorUtility.SetDirty(rCam);
            }

            PrototypeSceneBuilder.BuildWorkbenchControlPanel3D(wbObj.transform);
        }

        private static void WireJaegerProjectFromScene()
        {
            var jaeger = Object.FindAnyObjectByType<JaegerProject>();
            if (jaeger == null) return;

            var frame = GameObject.Find("--- 05_JAEGER_FRAME ---");
            GameObject egerRoot = GameObject.Find("Eger")
                ?? (frame != null ? (frame.transform.Find("Eger")?.gameObject ?? frame.transform.Find("Jaeger_Chassis")?.gameObject) : null);
            if (egerRoot == null) return;

            Transform[] sceneParts =
            {
                egerRoot.transform.Find("EgerHead"),
                egerRoot.transform.Find("EgerLeftArm"),
                egerRoot.transform.Find("EgerRightArm"),
                egerRoot.transform.Find("EgerShield"),
                egerRoot.transform.Find("EgerSpear")
            };
            GameObject[] prefabs =
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerHead.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerLeftArm.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerRightArm.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerShield.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/EgerSpear.fbx")
            };
            jaeger.Configure(sceneParts, prefabs, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Eger/Plate.fbx"));
            EditorUtility.SetDirty(jaeger);

            var oldFrame = GameObject.Find("--- 05_JAEGER_FRAME ---");
            if (oldFrame != null && egerRoot.transform.parent != oldFrame.transform)
                oldFrame.SetActive(false);
        }

        private static void RebuildAllSceneUIs()
        {
            HUDOverlay hud = PrototypeSceneBuilder.BuildHUD();
            if (hud != null) EditorUtility.SetDirty(hud);

            GameObject consoleGroup = GameObject.Find("--- 07_DISPATCH_CONSOLE ---");
            if (consoleGroup == null) return;

            var hf = GameObject.Find("HANGARFULL");
            Transform c104 = hf != null ? hf.transform.Find("Cube.104") : GameObject.Find("Cube.104")?.transform;
            Vector3 screenCenter = c104 != null && c104.GetComponent<Renderer>() != null
                ? c104.GetComponent<Renderer>().bounds.center
                : consoleGroup.transform.position;

            PrototypeSceneBuilder.BuildDispatchMonitors(consoleGroup.transform, c104, screenCenter, UnityEngine.Camera.main);

            var rCam = Object.FindAnyObjectByType<RoomCamera>();
            var armoryUi = Object.FindAnyObjectByType<ArmoryUI>();
            if (rCam != null && armoryUi != null)
            {
                Transform aTr = armoryUi.transform;
                rCam.ConfigureDispatchMonitorCamera(1.32f, aTr.position - aTr.forward * 0.58f, aTr.eulerAngles.y);
                EditorUtility.SetDirty(rCam);
            }
        }
    }
}
#endif
